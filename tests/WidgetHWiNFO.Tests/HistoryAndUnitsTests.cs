using WidgetHWiNFO.Core.Metrics;

namespace WidgetHWiNFO.Tests;

public class HistoryAndUnitsTests
{
    [Fact]
    public void History_keeps_last_n_in_order()
    {
        var h = new History(3);
        h.Add(1); h.Add(2); h.Add(3); h.Add(4);

        Assert.Equal(3, h.Count);
        Assert.Equal([2d, 3d, 4d], Enumerable.Range(0, h.Count).Select(i => h[i]));
        Assert.Equal(4, h.Max());
    }

    [Fact]
    public void History_stores_missing_as_nan()
    {
        var h = new History(2);
        h.Add(null);
        Assert.True(double.IsNaN(h[0]));
        Assert.True(double.IsNaN(h.Max()));
    }

    [Theory]
    [InlineData(1, "KB/s", 1024)]
    [InlineData(2, "MB/s", 2 * 1024 * 1024)]
    [InlineData(1, "GB", 1024d * 1024 * 1024)]
    public void Converts_units(double value, string unit, double expected)
    {
        double? actual = unit.EndsWith("/s") ? UnitConverter.ToBytesPerSecond(value, unit) : UnitConverter.ToBytes(value, unit);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Unknown_unit_is_null()
    {
        Assert.Null(UnitConverter.ToBytes(1, "RPM"));
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(13582d * 1024 * 1024, "13.3 GB")]
    public void Formats_bytes(double bytes, string expected)
    {
        Assert.Equal(expected, UnitConverter.FormatBytes(bytes).Replace(',', '.'));
    }
}
