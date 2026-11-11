using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WidgetHWiNFO.Services;

namespace WidgetHWiNFO.UI;

/// <summary>One editable row of the "Arrange panel" list.</summary>
public sealed class CardRow(CardId id) : ObservableObject
{
    public static string[] ColumnOptions { get; } = [Strings.ColumnAuto, Strings.ColumnLeft, Strings.ColumnRight];

    private bool _visible = true, _columnEnabled;
    private int _columnIndex;

    public CardId Id { get; } = id;
    public string Name { get; } = Strings.CardName(id);
    public bool Visible { get => _visible; set => Set(ref _visible, value); }

    /// <summary>Index into <see cref="ColumnOptions"/>, matching <see cref="CardColumn"/>.</summary>
    public int ColumnIndex { get => _columnIndex; set => Set(ref _columnIndex, value); }

    /// <summary>Columns only matter in the wide layout.</summary>
    public bool ColumnEnabled { get => _columnEnabled; set => Set(ref _columnEnabled, value); }

    public static CardRow From(CardLayout c) => new(c.Id) { Visible = c.Visible, ColumnIndex = (int)c.Column };
    public CardLayout ToLayout() => new() { Id = Id, Visible = Visible, Column = (CardColumn)Math.Clamp(ColumnIndex, 0, 2) };
}

internal partial class SettingsWindow : Window
{
    private sealed record IntervalOption(int Ms, string Label);

    private static readonly IntervalOption[] Intervals =
        [.. new[] { 500, 1000, 2000, 3000, 5000 }.Select(ms => new IntervalOption(ms, Strings.Seconds(ms / 1000.0)))];

    private readonly AppSettings _settings;
    private readonly ObservableCollection<CardRow> _rows = [];

    /// <summary>Raised after the settings were saved to disk.</summary>
    public event Action<AppSettings>? Saved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        LoadRows(settings.Cards);
        CardList.ItemsSource = _rows;

        HideStoppedFans.IsChecked = settings.HideStoppedFans;
        PinTrayIcons.IsChecked = settings.PinTrayIcons;
        ProcessesStartCollapsed.IsChecked = settings.ProcessesStartCollapsed;
        Layout.ItemsSource = new[] { Strings.LayoutCompact, Strings.LayoutWide };
        Layout.SelectedIndex = settings.WideLayout ? 1 : 0;
        StartWithWindows.IsChecked = SafeIsStartupEnabled();

        Interval.ItemsSource = Intervals;
        Interval.SelectedItem = Intervals.MinBy(i => Math.Abs(i.Ms - settings.UpdateIntervalMs));
        UpdateColumnEnabled();
    }

    private void LoadRows(IEnumerable<CardLayout> cards)
    {
        _rows.Clear();
        foreach (var c in cards) _rows.Add(CardRow.From(c));
        UpdateColumnEnabled();
    }

    private void UpdateColumnEnabled()
    {
        bool wide = Layout.SelectedIndex == 1;
        foreach (var r in _rows) r.ColumnEnabled = wide;
    }

    private void OnLayoutChanged(object sender, SelectionChangedEventArgs e) => UpdateColumnEnabled();

    private void OnMoveUp(object sender, RoutedEventArgs e) => Move(sender, -1);
    private void OnMoveDown(object sender, RoutedEventArgs e) => Move(sender, +1);

    private void Move(object sender, int delta)
    {
        if ((sender as FrameworkElement)?.Tag is not CardRow row) return;
        int i = _rows.IndexOf(row), j = i + delta;
        if (i < 0 || j < 0 || j >= _rows.Count) return;
        _rows.Move(i, j);
    }

    private void OnResetCards(object sender, RoutedEventArgs e) => LoadRows(AppSettings.DefaultCards());

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Cards = _rows.Select(r => r.ToLayout()).ToList();
        _settings.HideStoppedFans = HideStoppedFans.IsChecked == true;
        _settings.PinTrayIcons = PinTrayIcons.IsChecked == true;
        _settings.ProcessesStartCollapsed = ProcessesStartCollapsed.IsChecked == true;
        _settings.WideLayout = Layout.SelectedIndex == 1;
        if (Interval.SelectedItem is IntervalOption opt) _settings.UpdateIntervalMs = opt.Ms;

        try
        {
            StartupService.SetEnabled(StartWithWindows.IsChecked == true);
        }
        catch (Exception ex)
        {
            Log.Error("Updating startup registration", ex);
        }

        _settings.Save();
        Saved?.Invoke(_settings);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnOpenSettingsFile(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(AppSettings.FilePath)) _settings.Save();
            Process.Start(new ProcessStartInfo(AppSettings.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error("Opening settings file", ex);
        }
    }

    private static bool SafeIsStartupEnabled()
    {
        try { return StartupService.IsEnabled; }
        catch { return false; }
    }
}
