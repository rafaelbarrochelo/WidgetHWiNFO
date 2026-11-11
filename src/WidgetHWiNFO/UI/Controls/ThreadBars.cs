using System.Windows;
using System.Windows.Media;

namespace WidgetHWiNFO.UI.Controls;

/// <summary>One small vertical bar per CPU thread (0–100%), like the per-core view in macOS Stats.</summary>
public sealed class ThreadBars : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<double>), typeof(ThreadBars),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
        nameof(BarBrush), typeof(Brush), typeof(ThreadBars),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(ThreadBars),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<double>? Values { get => (IReadOnlyList<double>?)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public Brush BarBrush { get => (Brush)GetValue(BarBrushProperty); set => SetValue(BarBrushProperty, value); }
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    private static readonly Brush HighBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)));

    protected override void OnRender(DrawingContext dc)
    {
        var values = Values;
        if (values is null || values.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0) return;

        int n = values.Count;
        double gap = n > 24 ? 1 : 2;
        double w = (ActualWidth - gap * (n - 1)) / n;
        if (w < 1) return;
        double h = ActualHeight;
        double radius = Math.Min(1.5, w / 2);

        for (int i = 0; i < n; i++)
        {
            double x = i * (w + gap);
            dc.DrawRoundedRectangle(TrackBrush, null, new Rect(x, 0, w, h), radius, radius);

            double v = Math.Clamp(values[i], 0, 100);
            if (v <= 0) continue;
            double bh = Math.Max(1, h * v / 100);
            dc.DrawRoundedRectangle(v >= 90 ? HighBrush : BarBrush, null, new Rect(x, h - bh, w, bh), radius, radius);
        }
    }

    private static Brush Freeze(Brush b)
    {
        b.Freeze();
        return b;
    }
}
