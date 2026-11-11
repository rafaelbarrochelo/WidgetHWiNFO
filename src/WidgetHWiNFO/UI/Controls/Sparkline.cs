using System.Windows;
using System.Windows.Media;
using WidgetHWiNFO.Core.Metrics;

namespace WidgetHWiNFO.UI.Controls;

/// <summary>
/// Small area chart for a <see cref="History"/>. Newest sample on the right; gaps (NaN) break the line.
/// Bind <see cref="Version"/> to a counter that changes on every new sample to trigger a redraw.
/// </summary>
public sealed class Sparkline : FrameworkElement
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(History), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
        nameof(Version), typeof(int), typeof(Sparkline),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Fixed maximum (e.g. 100 for percentages). NaN = auto-scale to the visible data.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(Sparkline),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Optional second series drawn with <see cref="SecondaryStroke"/> (e.g. upload vs download).</summary>
    public static readonly DependencyProperty SecondarySourceProperty = DependencyProperty.Register(
        nameof(SecondarySource), typeof(History), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SecondaryStrokeProperty = DependencyProperty.Register(
        nameof(SecondaryStroke), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(Brushes.Orange, FrameworkPropertyMetadataOptions.AffectsRender));

    public History? Source { get => (History?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public int Version { get => (int)GetValue(VersionProperty); set => SetValue(VersionProperty, value); }
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public History? SecondarySource { get => (History?)GetValue(SecondarySourceProperty); set => SetValue(SecondarySourceProperty, value); }
    public Brush SecondaryStroke { get => (Brush)GetValue(SecondaryStrokeProperty); set => SetValue(SecondaryStrokeProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        // Transparent hit area so layout is stable even with no data.
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        double max = Maximum;
        if (double.IsNaN(max))
        {
            double a = Source?.Max() ?? double.NaN;
            double b = SecondarySource?.Max() ?? double.NaN;
            max = Math.Max(double.IsNaN(a) ? 0 : a, double.IsNaN(b) ? 0 : b);
            if (max <= 0) max = 1;
            max *= 1.15; // headroom
        }

        if (SecondarySource is { } secondary) Draw(dc, secondary, SecondaryStroke, max, w, h);
        if (Source is { } source) Draw(dc, source, Stroke, max, w, h);
    }

    private static void Draw(DrawingContext dc, History data, Brush stroke, double max, double w, double h)
    {
        int n = data.Count;
        if (n < 2) return;

        double step = w / (data.Capacity - 1);
        double x0 = w - (n - 1) * step;
        const double pad = 1.5;
        double Y(double v) => pad + (h - pad * 2) * (1 - Math.Clamp(v / max, 0, 1));

        var line = new StreamGeometry();
        var area = new StreamGeometry();
        using (var lc = line.Open())
        using (var ac = area.Open())
        {
            int i = 0;
            while (i < n)
            {
                while (i < n && !double.IsFinite(data[i])) i++;
                if (i >= n) break;

                var first = new Point(x0 + i * step, Y(data[i]));
                lc.BeginFigure(first, false, false);
                ac.BeginFigure(new Point(first.X, h), true, true);
                ac.LineTo(first, false, false);
                i++;
                while (i < n && double.IsFinite(data[i]))
                {
                    var p = new Point(x0 + i * step, Y(data[i]));
                    lc.LineTo(p, true, true);
                    ac.LineTo(p, false, false);
                    i++;
                }
                ac.LineTo(new Point(x0 + (i - 1) * step, h), false, false);
            }
        }
        line.Freeze();
        area.Freeze();

        Brush fill = stroke.Clone();
        fill.Opacity = 0.18;
        fill.Freeze();
        dc.DrawGeometry(fill, null, area);
        dc.DrawGeometry(null, new Pen(stroke, 1.4) { LineJoin = PenLineJoin.Round }, line);
    }
}
