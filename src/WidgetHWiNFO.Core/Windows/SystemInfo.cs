using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WidgetHWiNFO.Core.Windows;

/// <summary>Physical memory split the way Task Manager presents it, plus commit charge.</summary>
public sealed record MemoryBreakdown(
    long TotalBytes,
    long InUseBytes,
    long CachedBytes,
    long FreeBytes,
    long CommittedBytes,
    long CommitLimitBytes);

/// <param name="Letter">"C:"</param>
public sealed record VolumeSpace(string Letter, string? Label, long TotalBytes, long FreeBytes)
{
    public long UsedBytes => TotalBytes - FreeBytes;
}

public sealed record NetworkAddress(string AdapterName, string? IPv4, string? Gateway);

public sealed record SystemDetails(string OsName, TimeSpan Uptime, int Processes, int Threads, int Handles);

/// <summary>Cheap queries against Windows APIs that do not need HWiNFO or elevation.</summary>
[SupportedOSPlatform("windows")]
public static class SystemInfo
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PerformanceInformation
    {
        public uint cb;
        public nuint CommitTotal, CommitLimit, CommitPeak, PhysicalTotal, PhysicalAvailable,
                     SystemCache, KernelTotal, KernelPaged, KernelNonpaged, PageSize;
        public uint HandleCount, ProcessCount, ThreadCount;
    }

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPerformanceInfo(out PerformanceInformation info, uint size);

    public static MemoryBreakdown? GetMemory()
    {
        try
        {
            if (!GetPerformanceInfo(out var p, (uint)Marshal.SizeOf<PerformanceInformation>())) return null;
            long page = (long)p.PageSize;
            long total = (long)p.PhysicalTotal * page;
            long available = (long)p.PhysicalAvailable * page;
            // SystemCache = standby list + system working set; only the part counted as available is "cache".
            long cached = Math.Min((long)p.SystemCache * page, available);
            return new MemoryBreakdown(
                TotalBytes: total,
                InUseBytes: total - available,
                CachedBytes: cached,
                FreeBytes: available - cached,
                CommittedBytes: (long)p.CommitTotal * page,
                CommitLimitBytes: (long)p.CommitLimit * page);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    public static SystemDetails GetSystemDetails()
    {
        int processes = 0, threads = 0, handles = 0;
        try
        {
            if (GetPerformanceInfo(out var p, (uint)Marshal.SizeOf<PerformanceInformation>()))
            {
                processes = (int)p.ProcessCount;
                threads = (int)p.ThreadCount;
                handles = (int)p.HandleCount;
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
        }
        return new SystemDetails(OsName.Value, TimeSpan.FromMilliseconds(Environment.TickCount64), processes, threads, handles);
    }

    private static readonly Lazy<string> OsName = new(ReadOsName);

    /// <summary>"Windows 11 Pro 25H2". The registry ProductName still says "Windows 10" on Windows 11.</summary>
    private static string ReadOsName()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string product = key?.GetValue("ProductName") as string ?? "Windows";
            string? display = key?.GetValue("DisplayVersion") as string;
            if (Environment.OSVersion.Version.Build >= 22000)
                product = product.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase);
            return display is null ? product : $"{product} {display}";
        }
        catch (Exception)
        {
            return Environment.OSVersion.VersionString;
        }
    }

    /// <summary>Fixed, ready volumes with a drive letter.</summary>
    public static IReadOnlyList<VolumeSpace> GetVolumes()
    {
        var list = new List<VolumeSpace>();
        foreach (var d in DriveInfo.GetDrives())
        {
            try
            {
                if (d.DriveType != DriveType.Fixed || !d.IsReady) continue;
                string letter = d.Name.TrimEnd('\\');
                string? label = string.IsNullOrWhiteSpace(d.VolumeLabel) ? null : d.VolumeLabel;
                list.Add(new VolumeSpace(letter, label, d.TotalSize, d.TotalFreeSpace));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Drive disappeared or is locked (e.g. BitLocker): skip it.
            }
        }
        return list;
    }

    /// <summary>The adapter carrying the default route (has a gateway), or the first active one.</summary>
    public static NetworkAddress? GetPrimaryAddress()
    {
        try
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
                .Select(n => (Nic: n, Props: n.GetIPProperties()))
                .ToList();

            var best = candidates.FirstOrDefault(c => c.Props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork));
            if (best.Nic is null) best = candidates.FirstOrDefault();
            if (best.Nic is null) return null;

            string? ip = best.Props.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
            string? gw = best.Props.GatewayAddresses
                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
            return new NetworkAddress(best.Nic.Name, ip, gw);
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }
}
