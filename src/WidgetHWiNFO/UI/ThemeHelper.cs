using Microsoft.Win32;

namespace WidgetHWiNFO.UI;

internal static class ThemeHelper
{
    public static bool AppsUseLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not int v || v == 1;
        }
        catch
        {
            return true;
        }
    }
}
