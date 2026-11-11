using System.Collections.ObjectModel;
using WidgetHWiNFO.Core.Metrics;
using WidgetHWiNFO.Services;

namespace WidgetHWiNFO.UI;

public interface IKeyed
{
    string Key { get; }
}

public sealed class DiskItem(string key) : ObservableObject, IKeyed
{
    public string Key { get; } = key;
    private string _title = "", _model = "", _temp = "", _life = "", _read = "", _write = "", _capacity = "";
    private Severity _tempSeverity;
    private double _activity, _usedPercent;
    private bool _hasCapacity;

    public string Capacity { get => _capacity; set => Set(ref _capacity, value); }
    public double UsedPercent { get => _usedPercent; set => Set(ref _usedPercent, value); }
    public bool HasCapacity { get => _hasCapacity; set => Set(ref _hasCapacity, value); }

    public string Title { get => _title; set => Set(ref _title, value); }
    public string Model { get => _model; set => Set(ref _model, value); }
    public string Temp { get => _temp; set => Set(ref _temp, value); }
    public Severity TempSeverity { get => _tempSeverity; set => Set(ref _tempSeverity, value); }
    public string Life { get => _life; set => Set(ref _life, value); }
    public string Read { get => _read; set => Set(ref _read, value); }
    public string Write { get => _write; set => Set(ref _write, value); }
    public double Activity { get => _activity; set => Set(ref _activity, value); }
}

public sealed class LabelValueItem(string key) : ObservableObject, IKeyed
{
    public string Key { get; } = key;
    private string _label = "", _value = "";
    private Severity _severity;

    public string Label { get => _label; set => Set(ref _label, value); }
    public string Value { get => _value; set => Set(ref _value, value); }
    public Severity Severity { get => _severity; set => Set(ref _severity, value); }
}

public sealed class ProcessItem(string key) : ObservableObject, IKeyed
{
    public string Key { get; } = key;
    private string _name = "", _value = "";
    private double _percent;

    public string Name { get => _name; set => Set(ref _name, value); }
    public string Value { get => _value; set => Set(ref _value, value); }

    /// <summary>Relative size (0â€“100) for the background bar, scaled to the largest item.</summary>
    public double Percent { get => _percent; set => Set(ref _percent, value); }
}

/// <summary>Everything the popup shows. Only refreshed while the popup is visible.</summary>
public sealed class PopupViewModel : ObservableObject
{
    private readonly MonitorService _monitor;
    private AppSettings _settings;

    public PopupViewModel(MonitorService monitor, AppSettings settings)
    {
        _monitor = monitor;
        _settings = settings;
    }

    public MonitorService Monitor => _monitor;

    // ---- Connection --------------------------------------------------------------------------
    private bool _isConnected, _hasProblem;
    private string _statusText = Strings.Connecting, _problemTitle = "", _problemDetail = "";
    public bool IsConnected { get => _isConnected; private set => Set(ref _isConnected, value); }
    public bool HasProblem { get => _hasProblem; private set => Set(ref _hasProblem, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string ProblemTitle { get => _problemTitle; private set => Set(ref _problemTitle, value); }
    public string ProblemDetail { get => _problemDetail; private set => Set(ref _problemDetail, value); }

    private int _historyVersion;
    public int HistoryVersion { get => _historyVersion; private set => Set(ref _historyVersion, value); }

    // ---- Section visibility ------------------------------------------------------------------
    private bool _showCpu, _showGpu, _showMemory, _showDisks, _showNetwork, _showFans;
    public bool ShowCpu { get => _showCpu; private set => Set(ref _showCpu, value); }
    public bool ShowGpu { get => _showGpu; private set => Set(ref _showGpu, value); }
    public bool ShowMemory { get => _showMemory; private set => Set(ref _showMemory, value); }
    public bool ShowDisks { get => _showDisks; private set => Set(ref _showDisks, value); }
    public bool ShowNetwork { get => _showNetwork; private set => Set(ref _showNetwork, value); }
    public bool ShowFans { get => _showFans; private set => Set(ref _showFans, value); }

    // ---- CPU ---------------------------------------------------------------------------------
    private string _cpuName = "", _cpuUsageText = "", _cpuTemp = "", _cpuPower = "", _cpuClock = "";
    private double _cpuUsage;
    private Severity _cpuTempSeverity;
    public string CpuName { get => _cpuName; private set => Set(ref _cpuName, value); }
    public double CpuUsage { get => _cpuUsage; private set => Set(ref _cpuUsage, value); }
    public string CpuUsageText { get => _cpuUsageText; private set => Set(ref _cpuUsageText, value); }
    public string CpuTemp { get => _cpuTemp; private set => Set(ref _cpuTemp, value); }
    public Severity CpuTempSeverity { get => _cpuTempSeverity; private set => Set(ref _cpuTempSeverity, value); }
    public string CpuPower { get => _cpuPower; private set => Set(ref _cpuPower, value); }
    public string CpuClock { get => _cpuClock; private set => Set(ref _cpuClock, value); }

    // ---- GPU ---------------------------------------------------------------------------------
    private bool _hasGpu;
    private string _gpuName = "", _gpuUsageText = "", _gpuTemp = "", _gpuHotSpot = "", _gpuPower = "", _gpuVram = "", _gpuFan = "", _gpuClock = "";
    private double _gpuUsage;
    private Severity _gpuTempSeverity;
    public bool HasGpu { get => _hasGpu; private set => Set(ref _hasGpu, value); }
    public string GpuName { get => _gpuName; private set => Set(ref _gpuName, value); }
    public double GpuUsage { get => _gpuUsage; private set => Set(ref _gpuUsage, value); }
    public string GpuUsageText { get => _gpuUsageText; private set => Set(ref _gpuUsageText, value); }
    public string GpuTemp { get => _gpuTemp; private set => Set(ref _gpuTemp, value); }
    public Severity GpuTempSeverity { get => _gpuTempSeverity; private set => Set(ref _gpuTempSeverity, value); }
    public string GpuHotSpot { get => _gpuHotSpot; private set => Set(ref _gpuHotSpot, value); }
    public string GpuPower { get => _gpuPower; private set => Set(ref _gpuPower, value); }
    public string GpuVram { get => _gpuVram; private set => Set(ref _gpuVram, value); }
    public string GpuFan { get => _gpuFan; private set => Set(ref _gpuFan, value); }
    public string GpuClock { get => _gpuClock; private set => Set(ref _gpuClock, value); }

    // ---- Memory ------------------------------------------------------------------------------
    private double _memLoad;
    private string _memLoadText = "", _memUsedText = "";
    public double MemLoad { get => _memLoad; private set => Set(ref _memLoad, value); }
    public string MemLoadText { get => _memLoadText; private set => Set(ref _memLoadText, value); }
    public string MemUsedText { get => _memUsedText; private set => Set(ref _memUsedText, value); }

    // ---- Network -----------------------------------------------------------------------------
    private string _netDown = "", _netUp = "", _netAdapter = "";
    public string NetDown { get => _netDown; private set => Set(ref _netDown, value); }
    public string NetUp { get => _netUp; private set => Set(ref _netUp, value); }
    public string NetAdapter { get => _netAdapter; private set => Set(ref _netAdapter, value); }

    // ---- CPU threads / memory breakdown / network address ------------------------------------
    private IReadOnlyList<double> _threadUsage = [];
    public IReadOnlyList<double> ThreadUsage { get => _threadUsage; private set => Set(ref _threadUsage, value); }

    private bool _hasMemDetails;
    private double _memInUse, _memCached, _memFree;
    private string _memInUseText = "", _memCachedText = "", _memFreeText = "", _memCommitText = "";
    public bool HasMemDetails { get => _hasMemDetails; private set => Set(ref _hasMemDetails, value); }
    public double MemInUse { get => _memInUse; private set => Set(ref _memInUse, value); }
    public double MemCached { get => _memCached; private set => Set(ref _memCached, value); }
    public double MemFree { get => _memFree; private set => Set(ref _memFree, value); }
    public string MemInUseText { get => _memInUseText; private set => Set(ref _memInUseText, value); }
    public string MemCachedText { get => _memCachedText; private set => Set(ref _memCachedText, value); }
    public string MemFreeText { get => _memFreeText; private set => Set(ref _memFreeText, value); }
    public string MemCommitText { get => _memCommitText; private set => Set(ref _memCommitText, value); }

    // ---- System ------------------------------------------------------------------------------
    private bool _showSystem;
    private string _osName = "", _boardName = "", _uptime = "", _procCount = "", _threadCount = "", _handleCount = "";
    public bool ShowSystem { get => _showSystem; private set => Set(ref _showSystem, value); }
    public string OsName { get => _osName; private set => Set(ref _osName, value); }
    public string BoardName { get => _boardName; private set => Set(ref _boardName, value); }
    public string Uptime { get => _uptime; private set => Set(ref _uptime, value); }
    public string ProcessCount { get => _procCount; private set => Set(ref _procCount, value); }
    public string ThreadCount { get => _threadCount; private set => Set(ref _threadCount, value); }
    public string HandleCount { get => _handleCount; private set => Set(ref _handleCount, value); }

    private string _netAddress = "";
    public string NetAddress { get => _netAddress; private set => Set(ref _netAddress, value); }

    // ---- Processes ---------------------------------------------------------------------------
    private const int TopProcesses = 5;
    private bool _showProcesses, _hasCpuProcesses;
    public bool ShowProcesses { get => _showProcesses; private set => Set(ref _showProcesses, value); }
    public bool HasCpuProcesses { get => _hasCpuProcesses; private set => Set(ref _hasCpuProcesses, value); }
    public string ProcessesToggleText => ShowProcesses ? Strings.HideProcesses : Strings.ShowProcesses;
    public ObservableCollection<ProcessItem> TopCpu { get; } = [];
    public ObservableCollection<ProcessItem> TopMemory { get; } = [];

    // ---- Lists -------------------------------------------------------------------------------
    public ObservableCollection<DiskItem> Disks { get; } = [];
    public ObservableCollection<LabelValueItem> Fans { get; } = [];

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        Refresh();
    }

    public AppSettings Settings => _settings;

    private bool _processesExpanded = true;

    /// <summary>Called each time the panel opens: lists start in the state chosen in settings.</summary>
    public void ResetForOpen()
    {
        _processesExpanded = !_settings.ProcessesStartCollapsed;
        _monitor.ProcessesEnabled = _processesExpanded;
    }

    /// <summary>Expands/collapses the process lists for this session only.</summary>
    public void ToggleProcesses()
    {
        _processesExpanded = !_processesExpanded;
        _monitor.ProcessesEnabled = _processesExpanded;
        if (_processesExpanded) _monitor.Tick(); // fill the lists right away
        Refresh();
    }

    public void Refresh()
    {
        var state = _monitor.State;
        var m = _monitor.Metrics;

        IsConnected = state == ConnectionState.Connected;
        (HasProblem, ProblemTitle, ProblemDetail) = state switch
        {
            ConnectionState.Connected or ConnectionState.Connecting => (false, "", ""),
            ConnectionState.NotRunning => (true, Strings.NotRunningTitle, Strings.NotRunningDetail),
            ConnectionState.Inactive => (true, Strings.InactiveTitle, Strings.InactiveDetail),
            ConnectionState.Stale => (true, Strings.StaleTitle, Strings.StaleDetail),
            _ => (true, Strings.InvalidTitle, Strings.InvalidDetail),
        };
        StatusText = state switch
        {
            ConnectionState.Connected => Strings.Connected,
            ConnectionState.Connecting => Strings.Connecting,
            _ => ProblemTitle,
        };

        bool hasData = m is not null;
        ShowCpu = hasData && _settings.IsVisible(CardId.Cpu);
        ShowGpu = hasData && _settings.IsVisible(CardId.Gpu) && m!.Gpu is not null;
        ShowMemory = hasData && _settings.IsVisible(CardId.Memory) && m!.Memory.LoadPercent is not null;
        ShowNetwork = hasData && _settings.IsVisible(CardId.Network) && m!.Network.DownloadBytesPerSec is not null;
        if (ShowProcesses != _processesExpanded)
        {
            ShowProcesses = _processesExpanded;
            OnPropertyChanged(nameof(ProcessesToggleText));
        }
        if (m is null) return;

        RefreshDetails();

        // CPU
        CpuName = m.Cpu.Name ?? "CPU";
        CpuUsage = m.Cpu.UsagePercent ?? 0;
        CpuUsageText = Fmt.Percent(m.Cpu.UsagePercent);
        CpuTemp = Fmt.Temp(m.Cpu.TemperatureC);
        CpuTempSeverity = Fmt.TempSeverity(m.Cpu.TemperatureC, 85, 95);
        CpuPower = Fmt.Watts(m.Cpu.PowerW);
        CpuClock = Fmt.Clock(m.Cpu.ClockMHz);
        ThreadUsage = m.Cpu.ThreadUsage;

        // GPU
        HasGpu = m.Gpu is not null;
        if (m.Gpu is { } g)
        {
            GpuName = g.Name ?? "GPU";
            GpuUsage = g.UsagePercent ?? 0;
            GpuUsageText = Fmt.Percent(g.UsagePercent);
            GpuTemp = Fmt.Temp(g.TemperatureC);
            GpuTempSeverity = Fmt.TempSeverity(g.TemperatureC);
            GpuHotSpot = Fmt.Temp(g.HotSpotC);
            GpuPower = Fmt.Watts(g.PowerW);
            GpuVram = Fmt.Bytes(g.VramUsedBytes);
            GpuFan = Fmt.Rpm(g.FanRpm);
            GpuClock = Fmt.Clock(g.ClockMHz);
        }

        // Memory
        MemLoad = m.Memory.LoadPercent ?? 0;
        MemLoadText = Fmt.Percent(m.Memory.LoadPercent);
        MemUsedText = m.Memory.TotalBytes is not null
            ? $"{Fmt.Bytes(m.Memory.UsedBytes)} {Strings.Of} {Fmt.Bytes(m.Memory.TotalBytes)}"
            : Fmt.Bytes(m.Memory.UsedBytes);

        // Network
        NetDown = Fmt.Rate(m.Network.DownloadBytesPerSec);
        NetUp = Fmt.Rate(m.Network.UploadBytesPerSec);
        NetAdapter = m.Network.AdapterName ?? "";

        // Disks
        var volumes = _monitor.Volumes;
        Sync(Disks, m.Disks, d => d.Key, d => new DiskItem(d.Key), (item, d) =>
        {
            // HWiNFO lists the letters of each physical drive ("D:, E:"); sum their volumes.
            var letters = (d.Letters ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var vols = volumes.Where(v => letters.Contains(v.Letter, StringComparer.OrdinalIgnoreCase) && v.TotalBytes >= 1L << 30).ToList();
            long total = vols.Sum(v => v.TotalBytes), free = vols.Sum(v => v.FreeBytes);
            item.HasCapacity = total > 0;
            item.UsedPercent = total > 0 ? (total - free) * 100.0 / total : 0;
            item.Capacity = total > 0 ? $"{Fmt.Bytes(free)} {Strings.FreeOf} {Fmt.Bytes(total)}" : "";

            item.Title = d.Letters ?? d.Model;
            item.Model = d.Letters is null ? "" : d.Model;
            item.Temp = Fmt.Temp(d.TemperatureC);
            item.TempSeverity = Fmt.TempSeverity(d.TemperatureC, 60, 70);
            item.Life = d.RemainingLifePercent is { } life ? $"{life:0}% {Strings.Life}" : "";
            item.Read = Fmt.Rate(d.ReadBytesPerSec);
            item.Write = Fmt.Rate(d.WriteBytesPerSec);
            item.Activity = d.ActivityPercent ?? 0;
        });
        ShowDisks = _settings.IsVisible(CardId.Disks) && Disks.Count > 0;

        // Fans + board temperatures in one compact list
        var fanRows = m.Fans
            .Where(f => !_settings.HideStoppedFans || f.Rpm > 0)
            .Select(f => (Key: "fan:" + f.Key, Label: f.Label, Value: Fmt.Rpm(f.Rpm), Severity: Severity.Normal))
            .Concat(m.BoardTemperatures.Select(t => (Key: "temp:" + t.Key, Label: t.Label, Value: Fmt.Temp(t.TemperatureC), Severity: Fmt.TempSeverity(t.TemperatureC, 70, 85))))
            .ToList();
        Sync(Fans, fanRows, r => r.Key, r => new LabelValueItem(r.Key), (item, r) =>
        {
            item.Label = r.Label;
            item.Value = r.Value;
            item.Severity = r.Severity;
        });
        ShowFans = _settings.IsVisible(CardId.Fans) && Fans.Count > 0;

        HistoryVersion++;
    }

    private void RefreshDetails()
    {
        // Memory breakdown
        if (_monitor.MemoryDetails is { } md)
        {
            HasMemDetails = true;
            MemInUse = md.InUseBytes;
            MemCached = md.CachedBytes;
            MemFree = md.FreeBytes;
            MemInUseText = Fmt.Bytes(md.InUseBytes);
            MemCachedText = Fmt.Bytes(md.CachedBytes);
            MemFreeText = Fmt.Bytes(md.FreeBytes);
            MemCommitText = $"{Strings.Committed}: {Fmt.Bytes(md.CommittedBytes)} / {Fmt.Bytes(md.CommitLimitBytes)}";
        }
        else
        {
            HasMemDetails = false;
        }

        // System
        var sys = _monitor.SystemDetails;
        ShowSystem = _settings.IsVisible(CardId.System) && sys is not null;
        if (sys is not null)
        {
            OsName = sys.OsName;
            BoardName = _monitor.Metrics?.MotherboardName ?? Fmt.Missing;
            Uptime = Strings.Duration(sys.Uptime);
            ProcessCount = sys.Processes.ToString("N0");
            ThreadCount = sys.Threads.ToString("N0");
            HandleCount = sys.Handles.ToString("N0");
        }

        // Network address
        var addr = _monitor.Address;
        NetAddress = addr?.IPv4 is { } ip ? $"{Strings.LocalIp} {ip}" : "";

        // Top processes
        var procs = _monitor.Processes;
        if (!_processesExpanded || procs.Count == 0)
        {
            TopCpu.Clear();
            TopMemory.Clear();
            HasCpuProcesses = false;
            return;
        }

        var byCpu = procs.Where(p => p.CpuPercent is > 0.05)
            .OrderByDescending(p => p.CpuPercent).Take(TopProcesses).ToList();
        double maxCpu = byCpu.Count > 0 ? byCpu.Max(p => p.CpuPercent!.Value) : 1;
        Sync(TopCpu, byCpu, p => p.Name, p => new ProcessItem(p.Name), (item, p) =>
        {
            item.Name = DisplayName(p);
            item.Value = $"{p.CpuPercent:0.0}%";
            item.Percent = p.CpuPercent!.Value / maxCpu * 100;
        });
        HasCpuProcesses = byCpu.Count > 0;

        var byMem = procs.OrderByDescending(p => p.PrivateBytes).Take(TopProcesses).ToList();
        double maxMem = byMem.Count > 0 ? Math.Max(1, byMem[0].PrivateBytes) : 1;
        Sync(TopMemory, byMem, p => p.Name, p => new ProcessItem(p.Name), (item, p) =>
        {
            item.Name = DisplayName(p);
            item.Value = Fmt.Bytes(p.PrivateBytes);
            item.Percent = p.PrivateBytes / maxMem * 100;
        });
    }

    private static string DisplayName(Core.Windows.ProcessUsage p) =>
        p.Instances > 1 ? $"{p.Name} ({p.Instances})" : p.Name;

    /// <summary>Updates a collection in place (by key) to avoid re-creating visuals every tick.</summary>
    private static void Sync<TItem, TSource>(
        ObservableCollection<TItem> target, IReadOnlyList<TSource> source,
        Func<TSource, string> key, Func<TSource, TItem> create, Action<TItem, TSource> update)
        where TItem : IKeyed
    {
        var wanted = source.Select(key).ToHashSet();
        for (int i = target.Count - 1; i >= 0; i--)
            if (!wanted.Contains(target[i].Key)) target.RemoveAt(i);

        var existing = target.ToDictionary(t => t.Key);
        for (int i = 0; i < source.Count; i++)
        {
            var s = source[i];
            if (!existing.TryGetValue(key(s), out var item))
            {
                item = create(s);
                existing[item.Key] = item;
                target.Insert(i, item);
            }
            else
            {
                int idx = target.IndexOf(item);
                if (idx != i) target.Move(idx, i);
            }
            update(item, s);
        }
    }
}
