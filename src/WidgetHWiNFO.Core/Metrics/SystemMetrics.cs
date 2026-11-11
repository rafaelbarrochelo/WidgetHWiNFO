namespace WidgetHWiNFO.Core.Metrics;

// All values are nullable: null means "not available on this machine".
// Units are normalized: temperatures °C, power W, clocks MHz, memory bytes, rates bytes/s.

/// <param name="ThreadUsage">Per logical processor (thread) usage %, in HWiNFO order. Empty if unavailable.</param>
public sealed record CpuMetrics(
    string? Name,
    double? UsagePercent,
    double? TemperatureC,
    double? PowerW,
    double? ClockMHz,
    IReadOnlyList<double> ThreadUsage);

public sealed record GpuMetrics(
    string? Name,
    double? UsagePercent,
    double? TemperatureC,
    double? HotSpotC,
    double? PowerW,
    double? ClockMHz,
    double? VramUsedBytes,
    double? FanRpm);

public sealed record MemoryMetrics(
    double? UsedBytes,
    double? AvailableBytes,
    double? LoadPercent)
{
    public double? TotalBytes => UsedBytes + AvailableBytes;
}

/// <param name="Key">Stable identifier (model + drive letters, without serial number).</param>
public sealed record DiskMetrics(
    string Key,
    string Model,
    string? Letters,
    double? TemperatureC,
    double? RemainingLifePercent,
    double? ReadBytesPerSec,
    double? WriteBytesPerSec,
    double? ActivityPercent);

public sealed record NetworkMetrics(
    string? AdapterName,
    double? DownloadBytesPerSec,
    double? UploadBytesPerSec);

public sealed record FanMetrics(string Key, string Label, double Rpm);

public sealed record TemperatureMetrics(string Key, string Label, double TemperatureC);

public sealed record SystemMetrics(
    CpuMetrics Cpu,
    GpuMetrics? Gpu,
    MemoryMetrics Memory,
    IReadOnlyList<DiskMetrics> Disks,
    NetworkMetrics Network,
    IReadOnlyList<FanMetrics> Fans,
    IReadOnlyList<TemperatureMetrics> BoardTemperatures,
    string? MotherboardName = null);
