using System.Diagnostics;
using WidgetHWiNFO.Core.HWiNFO;
using WidgetHWiNFO.Core.Metrics;
using WidgetHWiNFO.Core.Windows;
using Xunit.Abstractions;

namespace WidgetHWiNFO.Tests;

/// <summary>Reports per-call cost of each sampler (informational; asserts only generous bounds).</summary>
public class PerfProbeTests(ITestOutputHelper output)
{
    private double Time(string name, int n, Action a)
    {
        a(); // warm up
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < n; i++) a();
        double ms = sw.Elapsed.TotalMilliseconds / n;
        output.WriteLine($"{name}: {ms:0.00} ms");
        return ms;
    }

    [Fact]
    public void Samplers_are_cheap()
    {
        using var sampler = new ProcessSampler();
        var reader = new SharedMemoryReader();
        var resolver = new MetricResolver();

        Assert.True(Time("processes", 20, () => sampler.Sample()) < 50);
        Assert.True(Time("memory", 20, () => SystemInfo.GetMemory()) < 20);
        Time("volumes", 5, () => SystemInfo.GetVolumes());
        Time("address", 5, () => SystemInfo.GetPrimaryAddress());
        Time("hwinfo read+resolve", 20, () =>
        {
            if (reader.TryRead(out var s) == HwInfoStatus.Ok) resolver.Resolve(s!);
        });
    }
}
