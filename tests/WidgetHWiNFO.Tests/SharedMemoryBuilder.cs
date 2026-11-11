using System.Buffers.Binary;
using System.Text;
using WidgetHWiNFO.Core.HWiNFO;

namespace WidgetHWiNFO.Tests;

/// <summary>Builds a synthetic HWiNFO_SENS_SM2 buffer matching HWiNFO 8.x layout (392/460-byte elements).</summary>
internal sealed class SharedMemoryBuilder
{
    private readonly List<string> _sensors = [];
    private readonly List<(int Sensor, ReadingType Type, string Label, string Unit, double Value, string? UserLabel)> _readings = [];

    public uint Signature { get; set; } = HwInfoParser.SignatureActive;
    public int SensorSize { get; set; } = 392;
    public int ReadingSize { get; set; } = 460;
    public long PollTime { get; set; } = 1_794_424_289;

    public int Sensor(string name)
    {
        _sensors.Add(name);
        return _sensors.Count - 1;
    }

    public SharedMemoryBuilder Reading(int sensor, ReadingType type, string label, string unit, double value, string? userLabel = null)
    {
        _readings.Add((sensor, type, label, unit, value, userLabel));
        return this;
    }

    public byte[] Build()
    {
        const int sensorOffset = 48;
        int readingOffset = sensorOffset + SensorSize * _sensors.Count;
        var buf = new byte[readingOffset + ReadingSize * _readings.Count];
        var s = buf.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(s[0..], Signature);
        BinaryPrimitives.WriteUInt32LittleEndian(s[4..], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(s[8..], 1);
        BinaryPrimitives.WriteInt64LittleEndian(s[12..], PollTime);
        BinaryPrimitives.WriteUInt32LittleEndian(s[20..], sensorOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(s[24..], (uint)SensorSize);
        BinaryPrimitives.WriteUInt32LittleEndian(s[28..], (uint)_sensors.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(s[32..], (uint)readingOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(s[36..], (uint)ReadingSize);
        BinaryPrimitives.WriteUInt32LittleEndian(s[40..], (uint)_readings.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(s[44..], 2000);

        for (int i = 0; i < _sensors.Count; i++)
        {
            var e = s.Slice(sensorOffset + i * SensorSize, SensorSize);
            BinaryPrimitives.WriteUInt32LittleEndian(e, (uint)(0xF000 + i));
            Write(e[8..], _sensors[i], Encoding.Latin1, 128);
            Write(e[136..], _sensors[i], Encoding.Latin1, 128);
            if (SensorSize >= 392) Write(e[264..], _sensors[i], Encoding.UTF8, 128);
        }

        for (int i = 0; i < _readings.Count; i++)
        {
            var r = _readings[i];
            var e = s.Slice(readingOffset + i * ReadingSize, ReadingSize);
            BinaryPrimitives.WriteUInt32LittleEndian(e[0..], (uint)r.Type);
            BinaryPrimitives.WriteUInt32LittleEndian(e[4..], (uint)r.Sensor);
            BinaryPrimitives.WriteUInt32LittleEndian(e[8..], (uint)(0x1000 + i));
            Write(e[12..], r.Label, Encoding.Latin1, 128);
            Write(e[140..], r.UserLabel ?? r.Label, Encoding.Latin1, 128);
            Write(e[268..], r.Unit, Encoding.Latin1, 16);
            BinaryPrimitives.WriteDoubleLittleEndian(e[284..], r.Value);
            BinaryPrimitives.WriteDoubleLittleEndian(e[292..], r.Value);
            BinaryPrimitives.WriteDoubleLittleEndian(e[300..], r.Value);
            BinaryPrimitives.WriteDoubleLittleEndian(e[308..], r.Value);
            if (ReadingSize >= 460)
            {
                Write(e[316..], r.UserLabel ?? r.Label, Encoding.UTF8, 128);
                Write(e[444..], r.Unit, Encoding.UTF8, 16);
            }
        }
        return buf;
    }

    private static void Write(Span<byte> dest, string text, Encoding enc, int max)
    {
        var bytes = enc.GetBytes(text);
        bytes.AsSpan(0, Math.Min(bytes.Length, max - 1)).CopyTo(dest);
    }

    /// <summary>A machine resembling a Ryzen + Radeon desktop (the author's), with fake serials.</summary>
    public static SharedMemoryBuilder SampleAmdDesktop()
    {
        var b = new SharedMemoryBuilder();
        int sys = b.Sensor("System");
        int cpu = b.Sensor("CPU [#0]: AMD Ryzen 7 2700X");
        int cpuEnh = b.Sensor("CPU [#0]: AMD Ryzen 7 2700X: Enhanced");
        int mobo = b.Sensor("ASUS PRIME B450M-GAMING/BR (ITE IT8655E)");
        int smartC = b.Sensor("S.M.A.R.T.: KINGSTON SNV3S1000G (SERIAL123) [C:]");
        int driveC = b.Sensor("Drive: KINGSTON SNV3S1000G (SERIAL123) [C:]");
        int smartD = b.Sensor("S.M.A.R.T.: SanDisk SSD PLUS 480GB (SERIAL456) [D:, E:]");
        int driveD = b.Sensor("Drive: SanDisk SSD PLUS 480GB (SERIAL456) [D:, E:]");
        int gpu = b.Sensor("dGPU [#0]: AMD Radeon RX 580: XFX RX 580 GTS");
        int gpuVrm = b.Sensor("dGPU [#0]: AMD Radeon RX 580: XFX RX 580 GTS - On Semi NCP81022");
        int net = b.Sensor("Network: RealTek Semiconductor RTL8168/8111 PCI-E Gigabit Ethernet NIC");

        b.Reading(sys, ReadingType.Other, "Physical Memory Used", "MB", 13582, "Memória física usada")
         .Reading(sys, ReadingType.Other, "Physical Memory Available", "MB", 19097)
         .Reading(sys, ReadingType.Other, "Physical Memory Load", "%", 41.5)
         .Reading(cpu, ReadingType.Clock, "Core 0 Clock (perf #2)", "MHz", 4000)
         .Reading(cpu, ReadingType.Clock, "Average Effective Clock", "MHz", 823.5)
         .Reading(cpu, ReadingType.Usage, "Core 0 T0 Usage", "%", 19.8)
         .Reading(cpu, ReadingType.Usage, "Core 0 T1 Usage", "%", 5.6)
         .Reading(cpu, ReadingType.Usage, "Max CPU/Thread Usage", "%", 19.8)
         .Reading(cpu, ReadingType.Usage, "Total CPU Usage", "%", 30.3)
         .Reading(cpu, ReadingType.Usage, "Core 0 T0 Utility", "%", 19.4)
         .Reading(cpuEnh, ReadingType.Temperature, "CPU (Tctl)", "°C", 54.3)
         .Reading(cpuEnh, ReadingType.Temperature, "CPU (Tdie)", "°C", 44.3)
         .Reading(cpuEnh, ReadingType.Power, "CPU Package Power", "W", 63.4)
         .Reading(mobo, ReadingType.Temperature, "CPU", "°C", 44)
         .Reading(mobo, ReadingType.Temperature, "Motherboard", "°C", 38)
         .Reading(mobo, ReadingType.Fan, "CPU", "RPM", 2227)
         .Reading(mobo, ReadingType.Fan, "Chassis1", "RPM", 0)
         .Reading(mobo, ReadingType.Fan, "Chassis2", "RPM", 1558)
         .Reading(smartC, ReadingType.Temperature, "Drive Temperature", "°C", 41)
         .Reading(smartC, ReadingType.Temperature, "Drive Temperature 2", "°C", 48)
         .Reading(smartC, ReadingType.Usage, "Drive Remaining Life", "%", 100)
         .Reading(driveC, ReadingType.Usage, "Total Activity", "%", 0.4)
         .Reading(driveC, ReadingType.Other, "Read Rate", "MB/s", 0.5)
         .Reading(driveC, ReadingType.Other, "Write Rate", "MB/s", 2)
         .Reading(smartD, ReadingType.Temperature, "Drive Temperature", "°C", 30)
         .Reading(smartD, ReadingType.Usage, "Drive Remaining Life", "%", 93)
         .Reading(driveD, ReadingType.Other, "Read Rate", "MB/s", 0)
         .Reading(gpu, ReadingType.Temperature, "GPU Thermal Diode", "°C", 55)
         .Reading(gpu, ReadingType.Temperature, "Temperature of the hottest sensor in GPU die.", "°C", 56)
         .Reading(gpu, ReadingType.Fan, "GPU Fan (ODN)", "RPM", 798)
         .Reading(gpu, ReadingType.Power, "GPU Core Power", "W", 8)
         .Reading(gpu, ReadingType.Power, "GPU Chip Power", "W", 29.1)
         .Reading(gpu, ReadingType.Clock, "GPU Clock", "MHz", 300)
         .Reading(gpu, ReadingType.Usage, "GPU Utilization", "%", 72)
         .Reading(gpu, ReadingType.Other, "GPU Memory Usage", "MB", 2412)
         .Reading(gpuVrm, ReadingType.Power, "GPU VRM Power Out (POUT)", "W", 4.5)
         .Reading(net, ReadingType.Other, "Current DL rate", "KB/s", 41.5)
         .Reading(net, ReadingType.Other, "Current UP rate", "KB/s", 51.2);
        return b;
    }
}
