namespace WidgetHWiNFO.Core.Metrics;

public static class UnitConverter
{
    private const double K = 1024;

    /// <summary>Converts an HWiNFO memory value ("KB", "MB", "GB"...) to bytes.</summary>
    public static double? ToBytes(double value, string unit)
    {
        double? factor = unit.Trim().ToUpperInvariant() switch
        {
            "B" => 1,
            "KB" => K,
            "MB" => K * K,
            "GB" => K * K * K,
            "TB" => K * K * K * K,
            _ => null,
        };
        return factor is null ? null : value * factor;
    }

    /// <summary>Converts an HWiNFO rate ("KB/s", "MB/s"...) to bytes per second.</summary>
    public static double? ToBytesPerSecond(double value, string unit)
    {
        string u = unit.Trim();
        if (u.EndsWith("/s", StringComparison.OrdinalIgnoreCase)) u = u[..^2];
        return ToBytes(value, u);
    }

    /// <summary>Formats bytes as a short human string: "13.3 GB", "512 MB".</summary>
    public static string FormatBytes(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        int i = 0;
        double v = bytes;
        while (Math.Abs(v) >= 1000 && i < units.Length - 1) { v /= K; i++; }
        return v >= 100 || i == 0 ? $"{v:0} {units[i]}" : $"{v:0.0} {units[i]}";
    }

    /// <summary>Formats a rate as "1.2 MB/s".</summary>
    public static string FormatRate(double bytesPerSec) => FormatBytes(bytesPerSec) + "/s";
}
