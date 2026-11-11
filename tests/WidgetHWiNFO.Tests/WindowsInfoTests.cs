using System.Diagnostics;
using WidgetHWiNFO.Core.Windows;
using Xunit.Abstractions;

namespace WidgetHWiNFO.Tests;

public class WindowsInfoTests(ITestOutputHelper output)
{
    [Fact]
    public void Process_sampler_matches_process_api()
    {
        using var sampler = new ProcessSampler();
        var first = sampler.Sample();
        Assert.NotEmpty(first);
        Assert.All(first, p => Assert.Null(p.CpuPercent)); // no delta on the first sample

        // Burn some CPU so this process shows up in the delta.
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 300) { }

        var second = sampler.Sample();
        string me = Process.GetCurrentProcess().ProcessName;
        var mine = second.FirstOrDefault(p => string.Equals(p.Name, me, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(mine);
        Assert.True(mine.PrivateBytes > 1024 * 1024, $"private bytes {mine.PrivateBytes}");
        Assert.True(mine.CpuPercent > 0, $"cpu {mine.CpuPercent}");

        double totalCpu = second.Sum(p => p.CpuPercent ?? 0);
        Assert.InRange(totalCpu, 0, 101);

        foreach (var p in second.OrderByDescending(p => p.PrivateBytes).Take(5))
            output.WriteLine($"{p.Name} x{p.Instances}: {p.PrivateBytes / 1048576} MB, {p.CpuPercent:0.0}%");
    }

    [Fact]
    public void Memory_breakdown_adds_up()
    {
        var m = SystemInfo.GetMemory();
        Assert.NotNull(m);
        Assert.Equal(m.TotalBytes, m.InUseBytes + m.CachedBytes + m.FreeBytes);
        Assert.True(m.CommitLimitBytes >= m.TotalBytes / 2);
        output.WriteLine($"in use {m.InUseBytes >> 20} MB, cache {m.CachedBytes >> 20} MB, free {m.FreeBytes >> 20} MB, commit {m.CommittedBytes >> 20}/{m.CommitLimitBytes >> 20} MB");
    }

    [Fact]
    public void Lists_volumes_and_address()
    {
        var vols = SystemInfo.GetVolumes();
        Assert.Contains(vols, v => v.Letter == "C:");
        Assert.All(vols, v => Assert.InRange(v.FreeBytes, 0, v.TotalBytes));
        foreach (var v in vols) output.WriteLine($"{v.Letter} {v.Label}: {v.FreeBytes >> 30} GB free of {v.TotalBytes >> 30} GB");

        var net = SystemInfo.GetPrimaryAddress();
        output.WriteLine($"net: {net}");

        var sys = SystemInfo.GetSystemDetails();
        Assert.StartsWith("Windows", sys.OsName);
        Assert.True(sys.Processes > 10 && sys.Threads > sys.Processes && sys.Handles > 0);
        Assert.True(sys.Uptime > TimeSpan.Zero);
        output.WriteLine($"sys: {sys}");
    }
}
