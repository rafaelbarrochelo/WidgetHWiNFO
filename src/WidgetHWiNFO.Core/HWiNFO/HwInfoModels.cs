namespace WidgetHWiNFO.Core.HWiNFO;

/// <summary>Reading type as reported by HWiNFO (SENSOR_READING_TYPE).</summary>
public enum ReadingType : uint
{
    None = 0,
    Temperature = 1,
    Voltage = 2,
    Fan = 3,
    Current = 4,
    Power = 5,
    Clock = 6,
    Usage = 7,
    Other = 8,
}

/// <param name="Index">Position of the sensor in the shared memory sensor section.</param>
/// <param name="NameOriginal">Original (English) sensor name, e.g. "CPU [#0]: AMD Ryzen 7 2700X".</param>
/// <param name="NameUser">User-facing (possibly renamed/localized) sensor name.</param>
public sealed record HwSensor(int Index, uint Id, uint Instance, string NameOriginal, string NameUser);

/// <param name="LabelOriginal">Original (English) label — stable, used for matching.</param>
/// <param name="LabelUser">User-facing (possibly renamed/localized) label.</param>
public sealed record HwReading(
    int SensorIndex,
    ReadingType Type,
    uint Id,
    string LabelOriginal,
    string LabelUser,
    string Unit,
    double Value,
    double Min,
    double Max,
    double Average);

/// <summary>A complete, immutable copy of the HWiNFO shared memory at one point in time.</summary>
public sealed class HwSnapshot
{
    public required uint Version { get; init; }
    public required uint Revision { get; init; }

    /// <summary>Last time HWiNFO refreshed the shared memory (UTC).</summary>
    public required DateTimeOffset PollTime { get; init; }

    /// <summary>HWiNFO polling period in milliseconds (0 when unknown).</summary>
    public required uint PollingPeriodMs { get; init; }

    public required IReadOnlyList<HwSensor> Sensors { get; init; }
    public required IReadOnlyList<HwReading> Readings { get; init; }
}

public enum HwInfoStatus
{
    /// <summary>Data read successfully.</summary>
    Ok,

    /// <summary>The shared memory object does not exist (HWiNFO closed or "Shared Memory Support" disabled).</summary>
    NotRunning,

    /// <summary>The shared memory exists but HWiNFO flagged it inactive (e.g. free-version 12h limit reached).</summary>
    Inactive,

    /// <summary>The shared memory content is not in a format we understand.</summary>
    Invalid,
}
