using WidgetHWiNFO.Core.HWiNFO;

namespace WidgetHWiNFO.Tests;

public class HwInfoParserTests
{
    [Fact]
    public void Parses_sensors_and_readings()
    {
        var data = SharedMemoryBuilder.SampleAmdDesktop().Build();

        var status = HwInfoParser.TryParse(data, out var snap);

        Assert.Equal(HwInfoStatus.Ok, status);
        Assert.NotNull(snap);
        Assert.Equal(11, snap.Sensors.Count);
        Assert.Equal("CPU [#0]: AMD Ryzen 7 2700X", snap.Sensors[1].NameOriginal);
        Assert.Equal(2000u, snap.PollingPeriodMs);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_794_424_289), snap.PollTime);

        var tdie = snap.Readings.Single(r => r.LabelOriginal == "CPU (Tdie)");
        Assert.Equal(ReadingType.Temperature, tdie.Type);
        Assert.Equal(2, tdie.SensorIndex);
        Assert.Equal("°C", tdie.Unit);
        Assert.Equal(44.3, tdie.Value, 3);
    }

    [Fact]
    public void Uses_utf8_user_label_when_available()
    {
        var data = SharedMemoryBuilder.SampleAmdDesktop().Build();
        HwInfoParser.TryParse(data, out var snap);

        var mem = snap!.Readings.Single(r => r.LabelOriginal == "Physical Memory Used");
        Assert.Equal("Memória física usada", mem.LabelUser);
    }

    [Fact]
    public void Supports_older_layout_without_utf8_fields()
    {
        var b = SharedMemoryBuilder.SampleAmdDesktop();
        b.SensorSize = 264;
        b.ReadingSize = 316;

        var status = HwInfoParser.TryParse(b.Build(), out var snap);

        Assert.Equal(HwInfoStatus.Ok, status);
        Assert.Equal("°C", snap!.Readings.First(r => r.Type == ReadingType.Temperature).Unit);
    }

    [Fact]
    public void Supports_newer_layout_with_larger_elements()
    {
        var b = SharedMemoryBuilder.SampleAmdDesktop();
        b.SensorSize = 512;
        b.ReadingSize = 600;

        var status = HwInfoParser.TryParse(b.Build(), out var snap);

        Assert.Equal(HwInfoStatus.Ok, status);
        Assert.Equal(44.3, snap!.Readings.Single(r => r.LabelOriginal == "CPU (Tdie)").Value, 3);
    }

    [Fact]
    public void Reports_inactive_when_signature_is_dead()
    {
        var b = SharedMemoryBuilder.SampleAmdDesktop();
        b.Signature = HwInfoParser.SignatureInactive;

        Assert.Equal(HwInfoStatus.Inactive, HwInfoParser.TryParse(b.Build(), out _));
    }

    [Fact]
    public void Rejects_truncated_or_garbage_data()
    {
        var data = SharedMemoryBuilder.SampleAmdDesktop().Build();

        Assert.Equal(HwInfoStatus.Invalid, HwInfoParser.TryParse(data.AsSpan(0, 20), out _));
        Assert.Equal(HwInfoStatus.Invalid, HwInfoParser.TryParse(data.AsSpan(0, data.Length - 10), out _));
        Assert.Equal(HwInfoStatus.Invalid, HwInfoParser.TryParse(new byte[4096], out _));
    }
}
