using System.Windows;
using System.Windows.Media;

namespace WidgetHWiNFO.UI.Controls;

/// <summary>Horizontal bar split into three proportional segments (e.g. memory in use / cached / free).</summary>
public sealed class SegmentBar : FrameworkElement
{
    private static FrameworkPropertyMetadata Render(object def) => new(def, FrameworkPropertyMetadataOptions.AffectsRender);

    public static readonly DependencyProperty FirstProperty = DependencyProperty.Register(nameof(First), typeof(double), typeof(SegmentBar), Render(0d));
    public static readonly DependencyProperty SecondProperty = DependencyProperty.Register(nameof(Second), typeof(double), typeof(SegmentBar), Render(0d));
    public static readonly DependencyProperty ThirdProperty = DependencyProperty.Register(nameof(Third), typeof(double), typeof(SegmentBar), Render(0d));
    public static readonly DependencyProperty FirstBrushProperty = DependencyProperty.Register(nameof(FirstBrush), typeof(Brush), typeof(SegmentBar), Render(Brushes.Green));
    public static readonly DependencyProperty SecondBrushProperty = DependencyProperty.Register(nameof(SecondBrush), typeof(Brush), typeof(SegmentBar), Render(Brushes.Blue));
    public static readonly DependencyProperty ThirdBrushProperty = DependencyProperty.Register(nameof(ThirdBrush), typeof(Brush), typeof(SegmentBar), Render(Brushes.Gray));

    public double First { get => (double)GetValue(FirstProperty); set => SetValue(FirstProperty, value); }
    public double Second { get => (double)GetValue(SecondProperty); set => SetValue(SecondProperty, value); }
    public double Third { get => (double)GetValue(ThirdProperty); set => SetValue(ThirdProperty, value); }
    public Brush FirstBrush { get => (Brush)GetValue(FirstBrushProperty); set => SetValue(FirstBrushProperty, value); }
    public Brush SecondBrush { get => (Brush)GetValue(SecondBrushProperty); set => SetValue(SecondBrushProperty, value); }
    public Brush ThirdBrush { get => (Brush)GetValue(ThirdBrushProperty); set => SetValue(ThirdBrushProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        double a = Math.Max(0, First), b = Math.Max(0, Second), c = Math.Max(0, Third);
        double total = a + b + c;
        if (w <= 0 || h <= 0 || total <= 0) return;

        double r = h / 2;
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h), r, r));
        double x = 0;
        foreach (var (value, brush) in new[] { (a, FirstBrush), (b, SecondBrush), (c, ThirdBrush) })
        {
            double sw = w * value / total;
            if (sw > 0) dc.DrawRectangle(brush, null, new Rect(x, 0, sw, h));
            x += sw;
        }
        dc.Pop();
    }
}
