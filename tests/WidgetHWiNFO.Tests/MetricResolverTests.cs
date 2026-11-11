using WidgetHWiNFO.Core.HWiNFO;
using WidgetHWiNFO.Core.Metrics;

namespace WidgetHWiNFO.Tests;

public class MetricResolverTests
{
    private static SystemMetrics Resolve(SharedMemoryBuilder b, MetricResolver? resolver = null)
    {
        Assert.Equal(HwInfoStatus.Ok, HwInfoParser.TryParse(b.Build(), out var snap));
        return (resolver ?? new MetricResolver()).Resolve(snap!);
    }

    [Fact]
    public void Resolves_amd_desktop()
    {
        var m = Resolve(SharedMemoryBuilder.SampleAmdDesktop());

        Assert.Equal("AMD Ryzen 7 2700X", m.Cpu.Name);
        Assert.Equal(30.3, m.Cpu.UsagePercent);
        Assert.Equal(44.3, m.Cpu.TemperatureC); // Tdie preferred over Tctl (offset on Zen+)
        Assert.Equal(63.4, m.Cpu.PowerW);
        Assert.Equal(823.5, m.Cpu.ClockMHz);
        Assert.Equal([19.8, 5.6], m.Cpu.ThreadUsage); // excludes "Max CPU/Thread" and "Utility"

        Assert.NotNull(m.Gpu);
        Assert.Equal("AMD Radeon RX 580", m.Gpu.Name);
        Assert.Equal(72, m.Gpu.UsagePercent);
        Assert.Equal(55, m.Gpu.TemperatureC);
        Assert.Equal(56, m.Gpu.HotSpotC);
        Assert.Equal(29.1, m.Gpu.PowerW);
        Assert.Equal(2412d * 1024 * 1024, m.Gpu.VramUsedBytes);
        Assert.Equal(798, m.Gpu.FanRpm);

        Assert.Equal(13582d * 1024 * 1024, m.Memory.UsedBytes);
        Assert.Equal((13582d + 19097d) * 1024 * 1024, m.Memory.TotalBytes);
        Assert.Equal(41.5, m.Memory.LoadPercent);
    }

    [Fact]
    public void Merges_drive_and_smart_sensors_and_hides_serials()
    {
        var m = Resolve(SharedMemoryBuilder.SampleAmdDesktop());

        Assert.Equal(2, m.Disks.Count);
        var c = m.Disks[0];
        Assert.Equal("C:", c.Letters);
        Assert.Equal("KINGSTON SNV3S1000G", c.Model);
        Assert.DoesNotContain("SERIAL", c.Key);
        Assert.Equal(41, c.TemperatureC);
        Assert.Equal(100, c.RemainingLifePercent);
        Assert.Equal(2d * 1024 * 1024, c.WriteBytesPerSec);
        Assert.Equal(0.4, c.ActivityPercent);

        Assert.Equal("D:, E:", m.Disks[1].Letters);
        Assert.Equal(93, m.Disks[1].RemainingLifePercent);
    }

    [Fact]
    public void Resolves_network_fans_and_board()
    {
        var m = Resolve(SharedMemoryBuilder.SampleAmdDesktop());

        Assert.Equal(41.5 * 1024, m.Network.DownloadBytesPerSec);
        Assert.Equal(51.2 * 1024, m.Network.UploadBytesPerSec);

        Assert.Equal(["CPU", "Chassis1", "Chassis2", "GPU"], m.Fans.Select(f => f.Label));
        Assert.Contains(m.BoardTemperatures, t => t.Label == "Motherboard" && t.TemperatureC == 38);
        Assert.Equal("ASUS PRIME B450M-GAMING/BR", m.MotherboardName);
    }

    [Fact]
    public void Resolves_intel_nvidia_laptop()
    {
        var b = new SharedMemoryBuilder();
        int sys = b.Sensor("System");
        int cpu = b.Sensor("CPU [#0]: Intel Core i7-12700H");
        int dts = b.Sensor("CPU [#0]: Intel Core i7-12700H: DTS");
        int igpu = b.Sensor("GPU [#0]: Intel Iris Xe Graphics");
        int dgpu = b.Sensor("GPU [#1]: NVIDIA GeForce RTX 3060 Laptop GPU");
        b.Reading(sys, ReadingType.Other, "Physical Memory Load", "%", 60)
         .Reading(cpu, ReadingType.Usage, "Total CPU Usage", "%", 12)
         .Reading(cpu, ReadingType.Power, "CPU Package Power", "W", 25)
         .Reading(dts, ReadingType.Temperature, "Core Max", "°C", 70)
         .Reading(dts, ReadingType.Temperature, "CPU Package", "°C", 68)
         .Reading(igpu, ReadingType.Usage, "GPU D3D Usage", "%", 5)
         .Reading(dgpu, ReadingType.Temperature, "GPU Temperature", "°C", 61)
         .Reading(dgpu, ReadingType.Usage, "GPU Core Load", "%", 88)
         .Reading(dgpu, ReadingType.Power, "GPU Power", "W", 90)
         .Reading(dgpu, ReadingType.Other, "GPU Memory Allocated", "MB", 4096);

        var m = Resolve(b);

        Assert.Equal("Intel Core i7-12700H", m.Cpu.Name);
        Assert.Equal(68, m.Cpu.TemperatureC);
        // No "dGPU" prefix here, so the first GPU wins.
        Assert.Equal("Intel Iris Xe Graphics", m.Gpu!.Name);
        Assert.Equal(5, m.Gpu.UsagePercent);
    }

    [Fact]
    public void Missing_values_are_null_not_zero()
    {
        var b = new SharedMemoryBuilder();
        b.Sensor("System");

        var m = Resolve(b);

        Assert.Null(m.Cpu.TemperatureC);
        Assert.Null(m.Gpu);
        Assert.Null(m.Memory.TotalBytes);
        Assert.Null(m.Network.DownloadBytesPerSec);
        Assert.Empty(m.Disks);
    }

    [Fact]
    public void Override_picks_custom_label()
    {
        var resolver = new MetricResolver
        {
            Overrides = new Dictionary<string, string> { [MetricResolver.Keys.CpuTemp] = "CPU (Tctl)" },
        };

        var m = Resolve(SharedMemoryBuilder.SampleAmdDesktop(), resolver);

        Assert.Equal(54.3, m.Cpu.TemperatureC);
    }
}
