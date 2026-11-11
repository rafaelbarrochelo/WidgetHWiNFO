using Microsoft.Win32;

namespace WidgetHWiNFO.Services;

/// <summary>
/// Windows 11 hides new tray icons in the overflow menu. Each icon has an entry under
/// HKCU\Control Panel\NotifyIconSettings with an "IsPromoted" flag (the same switch as
/// Settings → Personalization → Taskbar → Other system tray icons). This only touches
/// entries that belong to this executable.
/// </summary>
internal static class TrayPinService
{
    private const string Root = @"Control Panel\NotifyIconSettings";

    public static void Apply(bool pinned)
    {
        try
        {
            string? exe = Environment.ProcessPath;
            if (exe is null) return;

            using var root = Registry.CurrentUser.OpenSubKey(Root);
            if (root is null) return; // Windows 10: no such setting

            foreach (var name in root.GetSubKeyNames())
            {
                using var key = root.OpenSubKey(name, writable: true);
                if (key?.GetValue("ExecutablePath") is not string path ||
                    !string.Equals(path, exe, StringComparison.OrdinalIgnoreCase))
                    continue;

                int desired = pinned ? 1 : 0;
                if (key.GetValue("IsPromoted") is int current && current == desired) continue;
                key.SetValue("IsPromoted", desired, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Updating tray icon visibility", ex);
        }
    }
}
