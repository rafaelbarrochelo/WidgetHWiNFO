using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WidgetHWiNFO.Services;
using Forms = System.Windows.Forms;

namespace WidgetHWiNFO.UI;

internal partial class PopupWindow : Window
{
    private const int MarginPx = 12;

    private readonly PopupViewModel _vm;
    private Native.POINT _anchor;
    private DateTime _hiddenAtUtc = DateTime.MinValue;
    private bool _positioning;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public PopupWindow(PopupViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        Deactivated += (_, _) => HidePopup();
        SizeChanged += (_, _) => Reposition();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) HidePopup();
        };
        SourceInitialized += (_, _) => ApplyBackdrop();
    }

    /// <summary>True if the popup was hidden very recently (the tray click that caused the
    /// deactivation should not immediately re-open it).</summary>
    public bool JustHidden => (DateTime.UtcNow - _hiddenAtUtc).TotalMilliseconds < 400;

    public void Toggle(Native.POINT cursor)
    {
        if (IsVisible) { HidePopup(); return; }
        if (JustHidden) return;
        ShowAt(cursor);
    }

    public void ShowAt(Native.POINT cursor)
    {
        _anchor = cursor;
        ApplyLayout();
        _vm.ResetForOpen();
        _vm.Monitor.DetailsEnabled = true;
        _vm.Monitor.Tick(); // sample processes/volumes right away instead of waiting a tick
        _vm.Refresh();

        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(cursor.X, cursor.Y));
        double scale = Native.GetScaleForPoint(cursor);
        MaxHeight = Math.Max(240, screen.WorkingArea.Height / scale - MarginPx * 2 / scale);

        Opacity = 0;
        Show();
        Reposition();
        Opacity = 1;
        Activate();
        Native.SetForegroundWindow(new WindowInteropHelper(this).Handle);
        Scroller.ScrollToTop();
        BalanceColumnsSoon();
    }

    public void HidePopup()
    {
        if (!IsVisible) return;
        _hiddenAtUtc = DateTime.UtcNow;
        _vm.Monitor.DetailsEnabled = false;
        Hide();
    }

    private void OnToggleProcesses(object sender, RoutedEventArgs e)
    {
        _vm.ToggleProcesses();
        BalanceColumnsSoon();
        // The CPU list fills in on the next sample (~1 s later); re-check once it has.
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (_, _) => { timer.Stop(); BalanceColumns(); };
        timer.Start();
    }

    private bool _wide;

    private Border CardFor(CardId id) => id switch
    {
        CardId.Cpu => CpuCard,
        CardId.Gpu => GpuCard,
        CardId.Memory => MemoryCard,
        CardId.Disks => DisksCard,
        CardId.Network => NetworkCard,
        CardId.Fans => FansCard,
        _ => SystemCard,
    };

    /// <summary>
    /// Arranges the cards in the user's order. Compact: one 360px column. Wide: two columns,
    /// cards pinned Left/Right go there; "Auto" cards start on the left (so they get measured)
    /// and are moved to the shorter column by <see cref="BalanceColumns"/> after layout.
    /// </summary>
    private void ApplyLayout()
    {
        _wide = _vm.Settings.WideLayout;
        Width = _wide ? 700 : 360;
        SecondColumn.Width = _wide ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        RightPanel.Margin = _wide ? new Thickness(8, 0, 0, 0) : new Thickness(0);
        System.Windows.Controls.Grid.SetRow(RightPanel, 0);
        System.Windows.Controls.Grid.SetColumn(RightPanel, 1);

        PlaceCards(c => _wide && c.Column == CardColumn.Right ? RightPanel : LeftPanel);
    }

    private void PlaceCards(Func<CardLayout, StackPanel> panelOf)
    {
        LeftPanel.Children.Clear();
        RightPanel.Children.Clear();
        foreach (var c in _vm.Settings.Cards) panelOf(c).Children.Add(CardFor(c.Id));
    }

    /// <summary>Runs after layout, since balancing depends on the rendered card heights.</summary>
    private void BalanceColumnsSoon() =>
        Dispatcher.BeginInvoke(BalanceColumns, System.Windows.Threading.DispatcherPriority.Background);

    private void BalanceColumns()
    {
        if (!_wide || !IsVisible) return;
        var cards = _vm.Settings.Cards;
        double Height(CardLayout c)
        {
            var b = CardFor(c.Id);
            return b.IsVisible ? b.ActualHeight + b.Margin.Bottom : 0;
        }

        double left = cards.Where(c => c.Column == CardColumn.Left).Sum(Height);
        double right = cards.Where(c => c.Column == CardColumn.Right).Sum(Height);
        var assigned = new Dictionary<CardId, StackPanel>();
        foreach (var c in cards.Where(c => c.Column == CardColumn.Auto))
        {
            if (left <= right) { assigned[c.Id] = LeftPanel; left += Height(c); }
            else { assigned[c.Id] = RightPanel; right += Height(c); }
        }

        if (assigned.All(a => CardFor(a.Key).Parent == a.Value)) return; // nothing moves
        PlaceCards(c => c.Column switch
        {
            CardColumn.Left => LeftPanel,
            CardColumn.Right => RightPanel,
            _ => assigned[c.Id],
        });
    }

    /// <summary>Places the window next to the taskbar, near the clicked tray icon (physical pixels).</summary>
    private void Reposition()
    {
        if (!IsVisible || _positioning) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        _positioning = true;
        try
        {
            var screen = Forms.Screen.FromPoint(new System.Drawing.Point(_anchor.X, _anchor.Y));
            var wa = screen.WorkingArea;
            var bounds = screen.Bounds;
            double scale = Native.GetScaleForPoint(_anchor);
            int w = (int)Math.Ceiling(ActualWidth * scale);
            int h = (int)Math.Ceiling(ActualHeight * scale);

            int x, y;
            if (wa.Left > bounds.Left) // taskbar on the left
            {
                x = wa.Left + MarginPx;
                y = _anchor.Y - h / 2;
            }
            else if (wa.Right < bounds.Right) // right
            {
                x = wa.Right - w - MarginPx;
                y = _anchor.Y - h / 2;
            }
            else
            {
                // Horizontal taskbar: dock to the screen corner on the tray's side,
                // like the Windows 11 flyouts (Wi-Fi, volume, notifications).
                bool trayOnRight = _anchor.X >= wa.Left + wa.Width / 2;
                x = trayOnRight ? wa.Right - w - MarginPx : wa.Left + MarginPx;
                y = wa.Top > bounds.Top
                    ? wa.Top + MarginPx            // taskbar on top
                    : wa.Bottom - h - MarginPx;    // taskbar at the bottom (default)
            }

            x = Math.Clamp(x, wa.Left + MarginPx, Math.Max(wa.Left + MarginPx, wa.Right - w - MarginPx));
            y = Math.Clamp(y, wa.Top + MarginPx, Math.Max(wa.Top + MarginPx, wa.Bottom - h - MarginPx));

            Native.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }
        finally
        {
            _positioning = false;
        }
    }

    /// <summary>Windows 11: acrylic backdrop + rounded corners. Older systems: solid background.</summary>
    public void ApplyBackdrop()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        bool dark = !ThemeHelper.AppsUseLightTheme();
        Native.SetDwmInt(hwnd, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, dark ? 1 : 0);
        Native.SetDwmInt(hwnd, Native.DWMWA_WINDOW_CORNER_PREFERENCE, Native.DWMWCP_ROUND);

        if (Native.SupportsSystemBackdrop)
        {
            if (HwndSource.FromHwnd(hwnd) is { } src) src.CompositionTarget.BackgroundColor = Colors.Transparent;
            Background = Brushes.Transparent;
            Native.SetDwmInt(hwnd, Native.DWMWA_SYSTEMBACKDROP_TYPE, Native.DWMSBT_TRANSIENTWINDOW);
        }
        else
        {
            SetResourceReference(BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        HidePopup();
        SettingsRequested?.Invoke();
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => ExitRequested?.Invoke();
}
