using WidgetHWiNFO.Core.Metrics;

namespace WidgetHWiNFO.UI;

public enum Severity { Normal, Warning, Critical }

/// <summary>Value formatting shared by the popup and the tray tooltip.</summary>
public static class Fmt
{
    public const string Missing = "—";

    public static string Percent(double? v) => v is { } x ? $"{x:0}%" : Missing;
    public static string Temp(double? v) => v is { } x ? $"{x:0}°C" : Missing;
    public static string Watts(double? v) => v is { } x ? (x < 10 ? $"{x:0.0} W" : $"{x:0} W") : Missing;
    public static string Rpm(double? v) => v is { } x ? $"{x:0} RPM" : Missing;
    public static string Bytes(double? v) => v is { } x ? UnitConverter.FormatBytes(x) : Missing;
    public static string Rate(double? v) => v is { } x ? UnitConverter.FormatRate(x) : Missing;

    public static string Clock(double? mhz) => mhz switch
    {
        null => Missing,
        >= 1000 => $"{mhz / 1000:0.00} GHz",
        _ => $"{mhz:0} MHz",
    };

    public static Severity TempSeverity(double? c, double warn = 80, double crit = 90) =>
        c is not { } x ? Severity.Normal : x >= crit ? Severity.Critical : x >= warn ? Severity.Warning : Severity.Normal;

    public static Severity LoadSeverity(double? p) =>
        p is not { } x ? Severity.Normal : x >= 95 ? Severity.Critical : x >= 85 ? Severity.Warning : Severity.Normal;
}
