using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WidgetHWiNFO.Core.Windows;

/// <param name="Name">Executable name without ".exe" (processes with the same name are grouped).</param>
/// <param name="Instances">Number of running processes in the group.</param>
/// <param name="CpuPercent">Share of total CPU time since the previous sample (0–100), null on the first sample.</param>
/// <param name="PrivateBytes">Private working set (what Task Manager shows as "Memory").</param>
public sealed record ProcessUsage(string Name, int Instances, double? CpuPercent, long PrivateBytes);

/// <summary>
/// Samples every process with a single NtQuerySystemInformation(SystemProcessInformation) call:
/// no per-process handles, so it is fast and also covers protected/elevated processes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ProcessSampler : IDisposable
{
    private const int SystemProcessInformation = 5;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);

    // SYSTEM_PROCESS_INFORMATION (x64) field offsets
    private const int OffNext = 0;
    private const int OffWorkingSetPrivate = 8;
    private const int OffCreateTime = 32;
    private const int OffUserTime = 40;
    private const int OffKernelTime = 48;
    private const int OffImageNameLength = 56;
    private const int OffImageNameBuffer = 64;
    private const int OffPid = 80;

    /// <summary>Samples further apart than this are treated as a fresh start (no CPU deltas).</summary>
    private static readonly TimeSpan MaxDeltaWindow = TimeSpan.FromSeconds(10);

    private IntPtr _buffer;
    private int _bufferSize = 1024 * 1024;
    private Dictionary<(long Pid, long Created), long> _previousCpu = [];
    private long _previousTimestamp;

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(int infoClass, IntPtr info, int length, out int returnLength);

    public IReadOnlyList<ProcessUsage> Sample()
    {
        if (!Environment.Is64BitProcess) return [];
        if (!Query()) return [];

        long now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_previousTimestamp, now);
        bool hasDelta = _previousTimestamp != 0 && elapsed > TimeSpan.Zero && elapsed < MaxDeltaWindow;
        double totalCpu100ns = elapsed.Ticks * (double)Environment.ProcessorCount; // TimeSpan ticks are 100 ns

        var currentCpu = new Dictionary<(long, long), long>(_previousCpu.Count + 64);
        var groups = new Dictionary<string, (string Name, int Count, double Cpu, long Mem)>(StringComparer.OrdinalIgnoreCase);

        int offset = 0;
        while (true)
        {
            IntPtr entry = _buffer + offset;
            long pid = Marshal.ReadIntPtr(entry, OffPid);
            if (pid != 0) // skip "System Idle Process"
            {
                long created = Marshal.ReadInt64(entry, OffCreateTime);
                long cpu = Marshal.ReadInt64(entry, OffUserTime) + Marshal.ReadInt64(entry, OffKernelTime);
                long priv = Marshal.ReadInt64(entry, OffWorkingSetPrivate);
                currentCpu[(pid, created)] = cpu;

                double cpuShare = 0;
                if (hasDelta && _previousCpu.TryGetValue((pid, created), out long prev) && cpu >= prev)
                    cpuShare = (cpu - prev) / totalCpu100ns * 100;

                string name = ReadName(entry, pid);
                groups[name] = groups.TryGetValue(name, out var g)
                    ? (g.Name, g.Count + 1, g.Cpu + cpuShare, g.Mem + priv)
                    : (name, 1, cpuShare, priv);
            }

            int next = Marshal.ReadInt32(entry, OffNext);
            if (next == 0) break;
            offset += next;
            if (offset >= _bufferSize) break;
        }

        _previousCpu = currentCpu;
        _previousTimestamp = now;

        return groups.Values
            .Select(g => new ProcessUsage(g.Name, g.Count, hasDelta ? Math.Min(100, g.Cpu) : null, g.Mem))
            .ToList();
    }

    private bool Query()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (_buffer == IntPtr.Zero) _buffer = Marshal.AllocHGlobal(_bufferSize);

            int status = NtQuerySystemInformation(SystemProcessInformation, _buffer, _bufferSize, out int needed);
            if (status == 0) return true;
            if (status != StatusInfoLengthMismatch) return false;

            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
            _bufferSize = Math.Max(needed + 256 * 1024, _bufferSize * 2);
        }
        return false;
    }

    private static string ReadName(IntPtr entry, long pid)
    {
        int lengthBytes = (ushort)Marshal.ReadInt16(entry, OffImageNameLength);
        IntPtr text = Marshal.ReadIntPtr(entry, OffImageNameBuffer);
        if (lengthBytes == 0 || text == IntPtr.Zero) return pid == 4 ? "System" : $"PID {pid}";

        string name = Marshal.PtrToStringUni(text, lengthBytes / 2);
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    public void Dispose()
    {
        if (_buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
        }
    }
}
