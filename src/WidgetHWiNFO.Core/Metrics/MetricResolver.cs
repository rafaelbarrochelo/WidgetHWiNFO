using System.Text.RegularExpressions;
using WidgetHWiNFO.Core.HWiNFO;

namespace WidgetHWiNFO.Core.Metrics;

/// <summary>
/// Turns the raw list of HWiNFO readings into a small set of well-known metrics.
/// Matching is done on the original (English) labels, which are stable across
/// HWiNFO languages and user renames. Each metric has an ordered list of candidate
/// labels so it works across AMD/Intel CPUs and AMD/NVIDIA/Intel GPUs.
/// </summary>
public sealed partial class MetricResolver
{
    // ---- Candidate labels (first match wins) ---------------------------------------------

    private static readonly string[] CpuUsageLabels = ["Total CPU Usage", "CPU Total Usage"];
    private static readonly string[] CpuTempLabels =
        ["CPU (Tctl/Tdie)", "CPU (Tdie)", "CPU Package", "CPU (Tctl)", "Core Max", "CPU Die (average)", "CPU"];
    private static readonly string[] CpuPowerLabels =
        ["CPU Package Power", "CPU PPT", "Core+SoC Power (SVI2 TFN)", "CPU Power"];
    private static readonly string[] CpuClockLabels = ["Average Effective Clock", "Core Clocks (avg)"];

    private static readonly string[] GpuUsageLabels = ["GPU Core Load", "GPU Utilization", "GPU D3D Usage"];
    private static readonly string[] GpuTempLabels =
        ["GPU Temperature", "GPU Thermal Diode", "GPU Core Temperature", "GPU Edge Temperature"];
    private static readonly string[] GpuHotSpotLabels =
        ["GPU Hot Spot Temperature", "GPU Hotspot Temperature", "Temperature of the hottest sensor in GPU die."];
    private static readonly string[] GpuPowerLabels =
        ["GPU Power", "Total Board Power (TBP)", "Total Board Power", "GPU ASIC Power", "GPU Chip Power", "GPU Core Power"];
    private static readonly string[] GpuClockLabels = ["GPU Clock", "GPU Core Clock"];
    private static readonly string[] GpuVramLabels =
        ["GPU Memory Allocated", "GPU Memory Usage", "GPU D3D Memory Dedicated"];

    private static readonly string[] BoardTempLabels =
        ["Motherboard", "System", "System1", "Chipset", "PCH", "MOS", "VRM", "VRM MOS", "T_Sensor"];

    // ---- Sensor name patterns -------------------------------------------------------------

    [GeneratedRegex(@"^(?<kind>d|i)?GPU \[#(?<n>\d+)\]", RegexOptions.CultureInvariant)]
    private static partial Regex GpuSensorRegex();

    [GeneratedRegex(@"^CPU \[#\d+\]", RegexOptions.CultureInvariant)]
    private static partial Regex CpuSensorRegex();

    // "Core 3 T1 Usage" (AMD/Intel), "P-core 0 T0 Usage" / "E-core 8 Usage" (Intel hybrid)
    [GeneratedRegex(@"^(?:[PE]-)?[Cc]ore \d+(?: T\d+)? Usage$", RegexOptions.CultureInvariant)]
    private static partial Regex ThreadUsageRegex();

    [GeneratedRegex(@"^(?<kind>Drive|S\.M\.A\.R\.T\.): (?<model>.*?)(?: \((?<serial>[^()]*)\))?(?: \[(?<letters>[^\]]*)\])?$", RegexOptions.CultureInvariant)]
    private static partial Regex DriveSensorRegex();

    /// <summary>Optional user overrides: metric key → original label to use instead of the defaults.</summary>
    public IReadOnlyDictionary<string, string> Overrides { get; init; } = new Dictionary<string, string>();

    public static class Keys
    {
        public const string CpuTemp = "cpu.temp";
        public const string CpuUsage = "cpu.usage";
        public const string CpuPower = "cpu.power";
        public const string GpuTemp = "gpu.temp";
        public const string GpuUsage = "gpu.usage";
        public const string GpuPower = "gpu.power";
    }

    public SystemMetrics Resolve(HwSnapshot snapshot)
    {
        var bySensor = snapshot.Readings
            .Where(r => r.SensorIndex >= 0 && r.SensorIndex < snapshot.Sensors.Count)
            .ToLookup(r => r.SensorIndex);

        var cpuSensors = snapshot.Sensors.Where(s => CpuSensorRegex().IsMatch(s.NameOriginal)).ToList();
        var gpuSensors = SelectPrimaryGpu(snapshot.Sensors);
        var driveSensors = snapshot.Sensors
            .Select(s => (Sensor: s, Match: DriveSensorRegex().Match(s.NameOriginal)))
            .Where(x => x.Match.Success)
            .ToList();
        var networkSensors = snapshot.Sensors.Where(s => s.NameOriginal.StartsWith("Network: ", StringComparison.Ordinal)).ToList();

        var excluded = new HashSet<int>(cpuSensors.Concat(gpuSensors).Concat(driveSensors.Select(d => d.Sensor)).Concat(networkSensors)
            .Select(s => s.Index));
        var otherSensors = snapshot.Sensors.Where(s => !excluded.Contains(s.Index)).ToList();

        IEnumerable<HwReading> In(IEnumerable<HwSensor> sensors) => sensors.SelectMany(s => bySensor[s.Index]);

        var cpuReadings = In(cpuSensors).ToList();
        var gpuReadings = In(gpuSensors).ToList();
        var allReadings = snapshot.Readings;

        // ---- CPU ----
        var cpu = new CpuMetrics(
            Name: cpuSensors.Count > 0 ? CleanDeviceName(cpuSensors[0].NameOriginal) : null,
            UsagePercent: Find(cpuReadings, Keys.CpuUsage, ReadingType.Usage, CpuUsageLabels)
                          ?? Find(allReadings, Keys.CpuUsage, ReadingType.Usage, CpuUsageLabels),
            TemperatureC: Find(cpuReadings, Keys.CpuTemp, ReadingType.Temperature, CpuTempLabels)
                          ?? Find(In(otherSensors).ToList(), Keys.CpuTemp, ReadingType.Temperature, ["CPU"]),
            PowerW: Find(cpuReadings, Keys.CpuPower, ReadingType.Power, CpuPowerLabels),
            ClockMHz: Find(cpuReadings, null, ReadingType.Clock, CpuClockLabels)
                      ?? AverageOf(cpuReadings, ReadingType.Clock, l => Regex.IsMatch(l, @"^Core \d+ Clock")),
            ThreadUsage: cpuReadings
                .Where(r => r.Type == ReadingType.Usage && double.IsFinite(r.Value) && ThreadUsageRegex().IsMatch(r.LabelOriginal))
                .Select(r => Math.Clamp(r.Value, 0, 100))
                .ToList());

        // ---- GPU ----
        GpuMetrics? gpu = null;
        if (gpuSensors.Count > 0)
        {
            gpu = new GpuMetrics(
                Name: CleanDeviceName(gpuSensors[0].NameOriginal),
                UsagePercent: Find(gpuReadings, Keys.GpuUsage, ReadingType.Usage, GpuUsageLabels),
                TemperatureC: Find(gpuReadings, Keys.GpuTemp, ReadingType.Temperature, GpuTempLabels),
                HotSpotC: Find(gpuReadings, null, ReadingType.Temperature, GpuHotSpotLabels),
                PowerW: Find(gpuReadings, Keys.GpuPower, ReadingType.Power, GpuPowerLabels),
                ClockMHz: Find(gpuReadings, null, ReadingType.Clock, GpuClockLabels),
                VramUsedBytes: FindBytes(gpuReadings, GpuVramLabels),
                FanRpm: gpuReadings.FirstOrDefault(r => r.Type == ReadingType.Fan) is { } f ? Valid(f.Value) : null);
        }

        // ---- Memory ----
        var memory = new MemoryMetrics(
            UsedBytes: FindBytes(allReadings, ["Physical Memory Used"]),
            AvailableBytes: FindBytes(allReadings, ["Physical Memory Available"]),
            LoadPercent: Find(allReadings, null, null, ["Physical Memory Load"]));

        // ---- Disks: merge "Drive:" (rates) and "S.M.A.R.T.:" (temp/life) sensors by model+letters ----
        var disks = driveSensors
            .GroupBy(d => (Model: d.Match.Groups["model"].Value.Trim(), Serial: d.Match.Groups["serial"].Value))
            .Select(g =>
            {
                var readings = g.SelectMany(d => bySensor[d.Sensor.Index]).ToList();
                string? letters = g.Select(d => d.Match.Groups["letters"].Value).FirstOrDefault(l => l.Length > 0);
                string key = letters is null ? g.Key.Model : $"{g.Key.Model} [{letters}]";
                return new DiskMetrics(
                    Key: key,
                    Model: g.Key.Model,
                    Letters: letters,
                    TemperatureC: Find(readings, null, ReadingType.Temperature, ["Drive Temperature", "Drive Airflow Temperature"]),
                    RemainingLifePercent: Find(readings, null, null, ["Drive Remaining Life"]),
                    ReadBytesPerSec: FindRate(readings, ["Read Rate"]),
                    WriteBytesPerSec: FindRate(readings, ["Write Rate"]),
                    ActivityPercent: Find(readings, null, null, ["Total Activity"]));
            })
            .OrderBy(d => d.Letters ?? "￿", StringComparer.OrdinalIgnoreCase)
            .ToList();

        // ---- Network: sum across adapters, name the busiest ----
        var netPerAdapter = networkSensors
            .Select(s =>
            {
                var r = bySensor[s.Index].ToList();
                return (Sensor: s,
                        Down: FindRate(r, ["Current DL rate"]),
                        Up: FindRate(r, ["Current UP rate"]));
            })
            .Where(x => x.Down is not null || x.Up is not null)
            .ToList();
        var network = new NetworkMetrics(
            AdapterName: netPerAdapter.OrderByDescending(x => (x.Down ?? 0) + (x.Up ?? 0)).Select(x => CleanDeviceName(x.Sensor.NameOriginal)).FirstOrDefault(),
            DownloadBytesPerSec: netPerAdapter.Count > 0 ? netPerAdapter.Sum(x => x.Down ?? 0) : null,
            UploadBytesPerSec: netPerAdapter.Count > 0 ? netPerAdapter.Sum(x => x.Up ?? 0) : null);

        // ---- Fans (motherboard/other sensors + GPU) ----
        var fans = new List<FanMetrics>();
        foreach (var r in In(otherSensors).Where(r => r.Type == ReadingType.Fan && double.IsFinite(r.Value)))
        {
            string label = r.LabelUser;
            fans.Add(new FanMetrics($"{r.SensorIndex}:{r.Id}", label, r.Value));
        }
        foreach (var r in gpuReadings.Where(r => r.Type == ReadingType.Fan && double.IsFinite(r.Value)))
        {
            fans.Add(new FanMetrics($"{r.SensorIndex}:{r.Id}", "GPU", r.Value));
        }

        // ---- Board temperatures ----
        var boardTemps = In(otherSensors)
            .Where(r => r.Type == ReadingType.Temperature && double.IsFinite(r.Value) &&
                        BoardTempLabels.Contains(r.LabelOriginal, StringComparer.OrdinalIgnoreCase))
            .Select(r => new TemperatureMetrics($"{r.SensorIndex}:{r.Id}", r.LabelUser, r.Value))
            .DistinctBy(t => t.Label)
            .Take(4)
            .ToList();

        // Motherboard: the "other" sensor that reports fans or board temperatures,
        // e.g. "ASUS PRIME B450M-GAMING/BR (ITE IT8655E)" → "ASUS PRIME B450M-GAMING/BR".
        var boardSensor = otherSensors.FirstOrDefault(s => bySensor[s.Index].Any(r =>
            r.Type == ReadingType.Fan ||
            (r.Type == ReadingType.Temperature && BoardTempLabels.Contains(r.LabelOriginal, StringComparer.OrdinalIgnoreCase))));
        string? boardName = boardSensor is null ? null : StripTrailingParens(boardSensor.NameOriginal);

        return new SystemMetrics(cpu, gpu, memory, disks, network, fans, boardTemps, boardName);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    /// <summary>Picks the readings belonging to the primary GPU: discrete first, then any GPU #0.</summary>
    private static List<HwSensor> SelectPrimaryGpu(IReadOnlyList<HwSensor> sensors)
    {
        var gpus = sensors
            .Select(s => (Sensor: s, Match: GpuSensorRegex().Match(s.NameOriginal)))
            .Where(x => x.Match.Success)
            .ToList();
        if (gpus.Count == 0) return [];

        var primary = gpus.FirstOrDefault(x => x.Match.Groups["kind"].Value == "d");
        if (primary.Sensor is null) primary = gpus[0];

        string prefix = primary.Match.Value; // e.g. "dGPU [#0]"
        return gpus.Where(x => x.Match.Value == prefix).Select(x => x.Sensor).ToList();
    }

    private double? Find(IReadOnlyList<HwReading> readings, string? overrideKey, ReadingType? type, IReadOnlyList<string> labels)
    {
        if (overrideKey is not null && Overrides.TryGetValue(overrideKey, out var custom) && !string.IsNullOrWhiteSpace(custom))
        {
            var o = readings.FirstOrDefault(r => string.Equals(r.LabelOriginal, custom, StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(r.LabelUser, custom, StringComparison.OrdinalIgnoreCase));
            if (o is not null) return Valid(o.Value);
        }

        foreach (var label in labels)
        {
            foreach (var r in readings)
            {
                if ((type is null || r.Type == type) &&
                    string.Equals(r.LabelOriginal, label, StringComparison.OrdinalIgnoreCase))
                {
                    var v = Valid(r.Value);
                    if (v is not null) return v;
                }
            }
        }
        return null;
    }

    private static double? AverageOf(IReadOnlyList<HwReading> readings, ReadingType type, Func<string, bool> labelFilter)
    {
        var values = readings.Where(r => r.Type == type && labelFilter(r.LabelOriginal) && double.IsFinite(r.Value))
                             .Select(r => r.Value).ToList();
        return values.Count > 0 ? values.Average() : null;
    }

    private static double? FindBytes(IReadOnlyList<HwReading> readings, IReadOnlyList<string> labels)
    {
        foreach (var label in labels)
        {
            var r = readings.FirstOrDefault(x => string.Equals(x.LabelOriginal, label, StringComparison.OrdinalIgnoreCase) && double.IsFinite(x.Value));
            if (r is not null) return UnitConverter.ToBytes(r.Value, r.Unit);
        }
        return null;
    }

    private static double? FindRate(IReadOnlyList<HwReading> readings, IReadOnlyList<string> labels)
    {
        foreach (var label in labels)
        {
            var r = readings.FirstOrDefault(x => string.Equals(x.LabelOriginal, label, StringComparison.OrdinalIgnoreCase) && double.IsFinite(x.Value));
            if (r is not null) return UnitConverter.ToBytesPerSecond(r.Value, r.Unit);
        }
        return null;
    }

    private static double? Valid(double v) => double.IsFinite(v) ? v : null;

    private static string StripTrailingParens(string name)
    {
        string n = name.Trim();
        if (n.EndsWith(')'))
        {
            int open = n.LastIndexOf(" (", StringComparison.Ordinal);
            if (open > 0) n = n[..open];
        }
        return n;
    }

    /// <summary>"CPU [#0]: AMD Ryzen 7 2700X: Enhanced" → "AMD Ryzen 7 2700X".</summary>
    internal static string CleanDeviceName(string sensorName)
    {
        int start = sensorName.IndexOf(": ", StringComparison.Ordinal);
        string name = start >= 0 ? sensorName[(start + 2)..] : sensorName;
        int next = name.IndexOf(": ", StringComparison.Ordinal);
        if (next >= 0) name = name[..next];
        return name.Trim();
    }
}
