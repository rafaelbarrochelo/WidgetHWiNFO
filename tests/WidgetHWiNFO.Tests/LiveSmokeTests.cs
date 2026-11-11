using WidgetHWiNFO.Core.HWiNFO;
using WidgetHWiNFO.Core.Metrics;
using Xunit.Abstractions;

namespace WidgetHWiNFO.Tests;

/// <summary>Reads the real HWiNFO shared memory when available (no-op otherwise, e.g. in CI).</summary>
public class LiveSmokeTests(ITestOutputHelper output)
{
    [Fact]
    public void Reads_live_hwinfo_when_running()
    {
        var status = new SharedMemoryReader().TryRead(out var snap);
        output.WriteLine($"Status: {status}");
        if (status != HwInfoStatus.Ok) return;

        Assert.NotEmpty(snap!.Sensors);
        var m = new MetricResolver().Resolve(snap);
        output.WriteLine($"CPU  {m.Cpu.Name}: {m.Cpu.UsagePercent:0.0}% {m.Cpu.TemperatureC:0.0}°C {m.Cpu.PowerW:0.0}W {m.Cpu.ClockMHz:0}MHz");
        output.WriteLine($"GPU  {m.Gpu?.Name}: {m.Gpu?.UsagePercent:0.0}% {m.Gpu?.TemperatureC:0.0}°C {m.Gpu?.PowerW:0.0}W VRAM {m.Gpu?.VramUsedBytes / 1048576:0}MB fan {m.Gpu?.FanRpm:0}");
        output.WriteLine($"RAM  {m.Memory.UsedBytes / 1048576:0}/{m.Memory.TotalBytes / 1048576:0} MB ({m.Memory.LoadPercent:0.0}%)");
        foreach (var d in m.Disks)
            output.WriteLine($"DISK {d.Letters} {d.Model}: {d.TemperatureC}°C life {d.RemainingLifePercent}% R {d.ReadBytesPerSec} W {d.WriteBytesPerSec}");
        output.WriteLine($"NET  {m.Network.AdapterName}: down {m.Network.DownloadBytesPerSec} up {m.Network.UploadBytesPerSec}");
        foreach (var f in m.Fans) output.WriteLine($"FAN  {f.Label}: {f.Rpm} RPM");
        foreach (var t in m.BoardTemperatures) output.WriteLine($"TEMP {t.Label}: {t.TemperatureC}°C");
    }
}
