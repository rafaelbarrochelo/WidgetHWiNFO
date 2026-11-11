using System.Windows;
using Microsoft.Win32;
using WidgetHWiNFO.Services;
using WidgetHWiNFO.Tray;
using WidgetHWiNFO.UI;

namespace WidgetHWiNFO;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private AppSettings _settings = new();
    private MonitorService? _monitor;
    private TrayController? _tray;
    private PopupViewModel? _vm;
    private PopupWindow? _popup;
    private SettingsWindow? _settingsWindow;
    private ConnectionState _lastState = ConnectionState.Connecting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, @"Local\WidgetHWiNFO.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        // An always-on tray app must never crash because of a transient error.
        DispatcherUnhandledException += (_, a) =>
        {
            Log.Error("Unhandled UI exception", a.Exception);
            a.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
            Log.Error("Unhandled exception", a.ExceptionObject as Exception ?? new Exception(a.ExceptionObject?.ToString()));
        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Log.Error("Unobserved task exception", a.Exception);
            a.SetObserved();
        };

        _settings = AppSettings.Load();
        _monitor = new MonitorService(_settings);
        _vm = new PopupViewModel(_monitor, _settings);

        _popup = new PopupWindow(_vm);
        _popup.SettingsRequested += OpenSettings;
        _popup.ExitRequested += ExitApp;

        _tray = new TrayController(_monitor, pt => _popup.Toggle(pt), OpenSettings, ExitApp);
        if (_settings.PinTrayIcons) ApplyTrayPinSoon(true);

        _monitor.Updated += OnMonitorUpdated;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _monitor.Start();

        if (e.Args.Contains("--show", StringComparer.OrdinalIgnoreCase))
        {
            var wa = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
            _popup.ShowAt(new Native.POINT { X = wa.Right - 1, Y = wa.Bottom + 1 });
        }
        if (e.Args.Contains("--settings", StringComparer.OrdinalIgnoreCase)) OpenSettings();
    }

    private void OnMonitorUpdated()
    {
        if (_popup is { IsVisible: true }) _vm!.Refresh();

        var state = _monitor!.State;
        if (state == _lastState) return;

        // Notify on meaningful transitions only (not on every tick).
        switch (state)
        {
            case ConnectionState.NotRunning when _lastState is ConnectionState.Connecting or ConnectionState.Connected:
                _tray?.ShowNotification(Strings.NotRunningTitle, Strings.NotRunningDetail);
                break;
            case ConnectionState.Inactive:
                _tray?.ShowNotification(Strings.InactiveTitle, Strings.InactiveDetail);
                break;
            case ConnectionState.Stale when _lastState == ConnectionState.Connected:
                _tray?.ShowNotification(Strings.StaleTitle, Strings.StaleDetail);
                break;
        }
        Log.Info($"State {_lastState} -> {state}");
        _lastState = state;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
            Dispatcher.BeginInvoke(() => _popup?.ApplyBackdrop());
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings);
        bool wasPinned = _settings.PinTrayIcons;
        _settingsWindow.Saved += s =>
        {
            _monitor!.ApplySettings(s);
            _vm!.ApplySettings(s);
            // Only touch the Windows setting when the user turned it on, or just turned it off.
            if (s.PinTrayIcons || wasPinned) ApplyTrayPinSoon(s.PinTrayIcons);
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /// <summary>Explorer registers new tray icons asynchronously, so wait a moment before updating them.</summary>
    private static void ApplyTrayPinSoon(bool pinned)
    {
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            TrayPinService.Apply(pinned);
        };
        timer.Start();
    }

    private void ExitApp() => Shutdown();

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _tray?.Dispose();
        _monitor?.Dispose();
        if (_singleInstance is not null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        base.OnExit(e);
    }
}
