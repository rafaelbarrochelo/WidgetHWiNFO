using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WidgetHWiNFO.Core.Metrics;
using WidgetHWiNFO.Services;
using WidgetHWiNFO.UI;

namespace WidgetHWiNFO.Tray;

/// <summary>Owns the single tray icon (static app icon) with a live tooltip and context menu.</summary>
internal sealed class TrayController : IDisposable
{
    private readonly MonitorService _monitor;
    private readonly Action<Native.POINT> _onToggle;
    private readonly NotifyIcon _icon;
    private readonly Icon _appIcon;
    private readonly ContextMenuStrip _menu;

    public TrayController(MonitorService monitor, Action<Native.POINT> onToggle, Action onSettings, Action onExit)
    {
        _monitor = monitor;
        _onToggle = onToggle;

        _menu = new ContextMenuStrip { ShowImageMargin = false };
        _menu.Items.Add(Strings.Open, null, (_, _) => Toggle());
        _menu.Items.Add(Strings.Settings, null, (_, _) => onSettings());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(Strings.Exit, null, (_, _) => onExit());

        _appIcon = LoadAppIcon();
        _icon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = Strings.AppName,
            ContextMenuStrip = _menu,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) Toggle();
        };
        _icon.Visible = true;

        _monitor.Updated += Refresh;
    }

    public void ShowNotification(string title, string text) =>
        _icon.ShowBalloonTip(8000, title, text, ToolTipIcon.Warning);

    private void Toggle()
    {
        Native.GetCursorPos(out var pt);
        _onToggle(pt);
    }

    private void Refresh()
    {
        var m = _monitor.Metrics;
        string text = _monitor.State switch
        {
            ConnectionState.Connected when m is not null => Describe(m),
            ConnectionState.Stale => Strings.StaleTitle,
            ConnectionState.NotRunning => Strings.NotRunningTitle,
            ConnectionState.Inactive => Strings.InactiveTitle,
            ConnectionState.Connecting => Strings.Connecting,
            _ => Strings.InvalidTitle,
        };
        // NotifyIcon.Text is limited to 127 characters.
        _icon.Text = text.Length > 127 ? text[..127] : text;
    }

    private static string Describe(SystemMetrics m)
    {
        var sb = new StringBuilder();
        sb.Append($"CPU {Fmt.Percent(m.Cpu.UsagePercent)} · {Fmt.Temp(m.Cpu.TemperatureC)}");
        if (m.Gpu is { } g) sb.Append($"\nGPU {Fmt.Percent(g.UsagePercent)} · {Fmt.Temp(g.TemperatureC)}");
        sb.Append($"\nRAM {Fmt.Percent(m.Memory.LoadPercent)}");
        if (m.Network.DownloadBytesPerSec is not null)
            sb.Append($"\n↓ {Fmt.Rate(m.Network.DownloadBytesPerSec)}  ↑ {Fmt.Rate(m.Network.UploadBytesPerSec)}");
        return sb.ToString();
    }

    /// <summary>Loads the embedded app icon at the tray size for the current DPI.</summary>
    private static Icon LoadAppIcon()
    {
        try
        {
            var info = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
            if (info is not null)
            {
                using var stream = info.Stream;
                return new Icon(stream, SystemInformation.SmallIconSize);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Loading tray icon", ex);
        }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _monitor.Updated -= Refresh;
        _icon.Visible = false;
        _icon.Dispose();
        _appIcon.Dispose();
        _menu.Dispose();
    }
}
