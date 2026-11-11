using Microsoft.Win32;

namespace WidgetHWiNFO.Services;

/// <summary>"Start with Windows" via HKCU\...\Run (no admin rights needed).</summary>
internal static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WidgetHWiNFO";

    private static string ExePath => Environment.ProcessPath ?? throw new InvalidOperationException("Unknown process path");

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string v &&
                   v.Contains(ExePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(ValueName, $"\"{ExePath}\"");
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
