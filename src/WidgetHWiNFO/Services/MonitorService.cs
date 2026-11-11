using System.Windows.Threading;
using WidgetHWiNFO.Core.HWiNFO;
using WidgetHWiNFO.Core.Metrics;
using WidgetHWiNFO.Core.Windows;

namespace WidgetHWiNFO.Services;

public enum ConnectionState
{
    Connecting,
    Connected,

    /// <summary>HWiNFO not running or "Shared Memory Support" disabled.</summary>
    NotRunning,

    /// <summary>HWiNFO marked the shared memory inactive (free version 12h limit).</summary>
    Inactive,

    /// <summary>Shared memory exists but stopped updating (sensors window closed/paused, or 12h limit).</summary>
    Stale,

    Invalid,
}

/// <summary>Polls HWiNFO on the UI thread (parsing ~300 readings takes well under 1 ms).</summary>
public sealed class MonitorService : IDisposable
{
    public const int HistoryLength = 60;

    private readonly SharedMemoryReader _reader = new();
    private readonly DispatcherTimer _timer;
    private MetricResolver _resolver;

    private DateTimeOffset _lastPollTime = DateTimeOffset.MinValue;
    private DateTime _lastPollChangeUtc = DateTime.UtcNow;

    public MonitorService(AppSettings settings)
    {
        _resolver = CreateResolver(settings);
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(settings.UpdateIntervalMs),
        };
        _timer.Tick += (_, _) => Tick();
    }

    public event Action? Updated;

    public ConnectionState State { get; private set; } = ConnectionState.Connecting;
    public SystemMetrics? Metrics { get; private set; }

    public History CpuUsage { get; } = new(HistoryLength);
    public History CpuTemp { get; } = new(HistoryLength);
    public History GpuUsage { get; } = new(HistoryLength);
    public History GpuTemp { get; } = new(HistoryLength);
    public History MemoryLoad { get; } = new(HistoryLength);
    public History NetDown { get; } = new(HistoryLength);
    public History NetUp { get; } = new(HistoryLength);

    // ---- Details from Windows APIs (only sampled while the panel is visible) ----------------

    private static readonly TimeSpan SlowRefresh = TimeSpan.FromSeconds(15);
    private readonly ProcessSampler _processes = new();
    private DateTime _lastSlowRefreshUtc = DateTime.MinValue;
    private bool _detailsEnabled;

    /// <summary>Turn on while the panel is open; process/volume/IP sampling costs nothing when off.</summary>
    public bool DetailsEnabled
    {
        get => _detailsEnabled;
        set
        {
            if (_detailsEnabled == value) return;
            _detailsEnabled = value;
            if (value) _lastSlowRefreshUtc = DateTime.MinValue; // refresh slow data on open
        }
    }

    /// <summary>Sample processes along with the other details (off while the lists are collapsed).</summary>
    public bool ProcessesEnabled { get; set; } = true;

    public IReadOnlyList<ProcessUsage> Processes { get; private set; } = [];
    public MemoryBreakdown? MemoryDetails { get; private set; }
    public IReadOnlyList<VolumeSpace> Volumes { get; private set; } = [];
    public NetworkAddress? Address { get; private set; }
    public SystemDetails? SystemDetails { get; private set; }

    public void Start()
    {
        Tick();
        _timer.Start();
    }

    public void ApplySettings(AppSettings settings)
    {
        _resolver = CreateResolver(settings);
        _timer.Interval = TimeSpan.FromMilliseconds(settings.UpdateIntervalMs);
        Tick();
    }

    public void Tick()
    {
        try
        {
            var status = _reader.TryRead(out var snapshot);
            switch (status)
            {
                case HwInfoStatus.Ok:
                    var metrics = _resolver.Resolve(snapshot!);
                    bool stale = IsStale(snapshot!);
                    Metrics = metrics;
                    State = stale ? ConnectionState.Stale : ConnectionState.Connected;
                    if (!stale) AppendHistory(metrics);
                    break;
                case HwInfoStatus.NotRunning:
                    State = ConnectionState.NotRunning;
                    Metrics = null;
                    break;
                case HwInfoStatus.Inactive:
                    State = ConnectionState.Inactive;
                    Metrics = null;
                    break;
                default:
                    State = ConnectionState.Invalid;
                    Metrics = null;
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Reading HWiNFO", ex);
            State = ConnectionState.Invalid;
            Metrics = null;
        }

        if (_detailsEnabled) SampleDetails();

        try
        {
            Updated?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error("Updating UI", ex);
        }
    }

    private void SampleDetails()
    {
        try
        {
            Processes = ProcessesEnabled ? _processes.Sample() : [];
            MemoryDetails = SystemInfo.GetMemory();
            SystemDetails = SystemInfo.GetSystemDetails();

            var now = DateTime.UtcNow;
            if (now - _lastSlowRefreshUtc >= SlowRefresh)
            {
                _lastSlowRefreshUtc = now;
                Volumes = SystemInfo.GetVolumes();
                Address = SystemInfo.GetPrimaryAddress();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Sampling system details", ex);
        }
    }

    private bool IsStale(HwSnapshot snapshot)
    {
        var now = DateTime.UtcNow;
        if (snapshot.PollTime != _lastPollTime)
        {
            _lastPollTime = snapshot.PollTime;
            _lastPollChangeUtc = now;
            return false;
        }

        // HWiNFO's poll_time has 1 s resolution; allow several missed polling periods.
        var limit = TimeSpan.FromMilliseconds(Math.Max(10_000, snapshot.PollingPeriodMs * 5.0));
        return now - _lastPollChangeUtc > limit;
    }

    private void AppendHistory(SystemMetrics m)
    {
        CpuUsage.Add(m.Cpu.UsagePercent);
        CpuTemp.Add(m.Cpu.TemperatureC);
        GpuUsage.Add(m.Gpu?.UsagePercent);
        GpuTemp.Add(m.Gpu?.TemperatureC);
        MemoryLoad.Add(m.Memory.LoadPercent);
        NetDown.Add(m.Network.DownloadBytesPerSec);
        NetUp.Add(m.Network.UploadBytesPerSec);
    }

    private static MetricResolver CreateResolver(AppSettings settings) =>
        new() { Overrides = new Dictionary<string, string>(settings.SensorOverrides) };

    public void Dispose()
    {
        _timer.Stop();
        _processes.Dispose();
    }
}
