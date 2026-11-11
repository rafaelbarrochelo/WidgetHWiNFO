using System.Buffers.Binary;
using System.Text;

namespace WidgetHWiNFO.Core.HWiNFO;

/// <summary>
/// Parses the HWiNFO shared memory layout (HWiNFO_SENS_SM2, version 2).
/// Element sizes and offsets are always taken from the header, so newer HWiNFO versions that
/// append fields to the elements keep working.
/// </summary>
public static class HwInfoParser
{
    public const uint SignatureActive = 0x53695748;   // "HWiS"
    public const uint SignatureInactive = 0x44414544; // "DEAD"

    public const int HeaderSize = 44;          // up to and including dwNumReadingElements
    public const int HeaderSizeWithPeriod = 48; // + dwPollingPeriod (revision >= 1)

    // Sensor element layout
    private const int SensorNameLen = 128;
    private const int SensorMinSize = 8 + SensorNameLen * 2;                 // 264
    private const int SensorUtf8NameOffset = SensorMinSize;                   // 264
    private const int SensorWithUtf8Size = SensorUtf8NameOffset + SensorNameLen; // 392

    // Reading element layout
    private const int LabelLen = 128;
    private const int UnitLen = 16;
    private const int ReadingLabelOrigOffset = 12;
    private const int ReadingLabelUserOffset = ReadingLabelOrigOffset + LabelLen; // 140
    private const int ReadingUnitOffset = ReadingLabelUserOffset + LabelLen;      // 268
    private const int ReadingValueOffset = ReadingUnitOffset + UnitLen;           // 284
    private const int ReadingMinSize = ReadingValueOffset + 8 * 4;                // 316
    private const int ReadingUtf8LabelOffset = ReadingMinSize;                    // 316
    private const int ReadingUtf8UnitOffset = ReadingUtf8LabelOffset + LabelLen;  // 444
    private const int ReadingWithUtf8Size = ReadingUtf8UnitOffset + UnitLen;      // 460

    /// <summary>Upper bounds to protect against a corrupt header.</summary>
    private const uint MaxElements = 100_000;

    public readonly record struct Header(
        uint Signature,
        uint Version,
        uint Revision,
        long PollTime,
        uint SensorOffset,
        uint SensorSize,
        uint SensorCount,
        uint ReadingOffset,
        uint ReadingSize,
        uint ReadingCount,
        uint PollingPeriodMs)
    {
        /// <summary>Number of bytes needed to hold header + both sections.</summary>
        public long TotalSize => Math.Max(
            (long)SensorOffset + (long)SensorSize * SensorCount,
            (long)ReadingOffset + (long)ReadingSize * ReadingCount);
    }

    public static bool TryParseHeader(ReadOnlySpan<byte> data, out Header header)
    {
        header = default;
        if (data.Length < HeaderSize) return false;

        header = new Header(
            Signature: U32(data, 0),
            Version: U32(data, 4),
            Revision: U32(data, 8),
            PollTime: BinaryPrimitives.ReadInt64LittleEndian(data[12..]),
            SensorOffset: U32(data, 20),
            SensorSize: U32(data, 24),
            SensorCount: U32(data, 28),
            ReadingOffset: U32(data, 32),
            ReadingSize: U32(data, 36),
            ReadingCount: U32(data, 40),
            PollingPeriodMs: data.Length >= HeaderSizeWithPeriod ? U32(data, 44) : 0);
        return true;
    }

    /// <summary>Validates a header's structure (not its signature).</summary>
    public static bool IsStructurallyValid(in Header h) =>
        h.SensorSize >= SensorMinSize &&
        h.ReadingSize >= ReadingMinSize &&
        h.SensorCount <= MaxElements &&
        h.ReadingCount <= MaxElements &&
        h.SensorOffset >= HeaderSize &&
        h.ReadingOffset >= HeaderSize;

    public static HwInfoStatus TryParse(ReadOnlySpan<byte> data, out HwSnapshot? snapshot)
    {
        snapshot = null;
        if (!TryParseHeader(data, out var h)) return HwInfoStatus.Invalid;
        if (h.Signature == SignatureInactive) return HwInfoStatus.Inactive;
        if (h.Signature != SignatureActive) return HwInfoStatus.Invalid;
        if (!IsStructurallyValid(h) || h.TotalSize > data.Length) return HwInfoStatus.Invalid;

        var sensors = new HwSensor[h.SensorCount];
        for (int i = 0; i < sensors.Length; i++)
        {
            var e = data.Slice((int)(h.SensorOffset + (long)i * h.SensorSize), (int)h.SensorSize);
            string orig = Latin1(e.Slice(8, SensorNameLen));
            string user = e.Length >= SensorWithUtf8Size
                ? Utf8(e.Slice(SensorUtf8NameOffset, SensorNameLen))
                : Latin1(e.Slice(8 + SensorNameLen, SensorNameLen));
            if (user.Length == 0) user = orig;
            sensors[i] = new HwSensor(i, U32(e, 0), U32(e, 4), orig, user);
        }

        var readings = new HwReading[h.ReadingCount];
        for (int i = 0; i < readings.Length; i++)
        {
            var e = data.Slice((int)(h.ReadingOffset + (long)i * h.ReadingSize), (int)h.ReadingSize);
            bool utf8 = e.Length >= ReadingWithUtf8Size;
            string labelOrig = Latin1(e.Slice(ReadingLabelOrigOffset, LabelLen));
            string labelUser = utf8
                ? Utf8(e.Slice(ReadingUtf8LabelOffset, LabelLen))
                : Latin1(e.Slice(ReadingLabelUserOffset, LabelLen));
            string unit = utf8
                ? Utf8(e.Slice(ReadingUtf8UnitOffset, UnitLen))
                : Latin1(e.Slice(ReadingUnitOffset, UnitLen));
            if (labelUser.Length == 0) labelUser = labelOrig;

            readings[i] = new HwReading(
                SensorIndex: (int)U32(e, 4),
                Type: (ReadingType)U32(e, 0),
                Id: U32(e, 8),
                LabelOriginal: labelOrig,
                LabelUser: labelUser,
                Unit: unit,
                Value: F64(e, ReadingValueOffset),
                Min: F64(e, ReadingValueOffset + 8),
                Max: F64(e, ReadingValueOffset + 16),
                Average: F64(e, ReadingValueOffset + 24));
        }

        snapshot = new HwSnapshot
        {
            Version = h.Version,
            Revision = h.Revision,
            PollTime = h.PollTime > 0 && h.PollTime < 253402300799
                ? DateTimeOffset.FromUnixTimeSeconds(h.PollTime)
                : DateTimeOffset.MinValue,
            PollingPeriodMs = h.PollingPeriodMs,
            Sensors = sensors,
            Readings = readings,
        };
        return HwInfoStatus.Ok;
    }

    private static uint U32(ReadOnlySpan<byte> d, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(d[offset..]);

    private static double F64(ReadOnlySpan<byte> d, int offset)
    {
        double v = BinaryPrimitives.ReadDoubleLittleEndian(d[offset..]);
        return double.IsFinite(v) ? v : double.NaN;
    }

    private static string Latin1(ReadOnlySpan<byte> s) => Encoding.Latin1.GetString(TrimAtNull(s)).Trim();

    private static string Utf8(ReadOnlySpan<byte> s) => Encoding.UTF8.GetString(TrimAtNull(s)).Trim();

    private static ReadOnlySpan<byte> TrimAtNull(ReadOnlySpan<byte> s)
    {
        int n = s.IndexOf((byte)0);
        return n >= 0 ? s[..n] : s;
    }
}
