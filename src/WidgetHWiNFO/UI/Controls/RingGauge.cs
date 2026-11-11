using System.Windows;
using System.Windows.Media;

namespace WidgetHWiNFO.UI.Controls;

/// <summary>Circular percentage gauge (0–100) rendered directly with DrawingContext.</summary>
public sealed class RingGauge : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RingGauge),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(RingGauge),
        new FrameworkPropertyMetadata(6d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(RingGauge),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
        nameof(FillBrush), typeof(Brush), typeof(RingGauge),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public Brush FillBrush { get => (Brush)GetValue(FillBrushProperty); set => SetValue(FillBrushProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= Thickness * 2) return;

        double r = (size - Thickness) / 2;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);

        var trackPen = new Pen(TrackBrush, Thickness);
        dc.DrawEllipse(null, trackPen, center, r, r);

        double v = double.IsFinite(Value) ? Math.Clamp(Value, 0, 100) : 0;
        if (v <= 0) return;

        var fillPen = new Pen(FillBrush, Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (v >= 99.9)
        {
            dc.DrawEllipse(null, fillPen, center, r, r);
            return;
        }

        double angle = v / 100 * 360;
        var start = new Point(center.X, center.Y - r);
        double rad = (angle - 90) * Math.PI / 180;
        var end = new Point(center.X + r * Math.Cos(rad), center.Y + r * Math.Sin(rad));

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(start, isFilled: false, isClosed: false);
            ctx.ArcTo(end, new Size(r, r), 0, angle > 180, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
        }
        geo.Freeze();
        dc.DrawGeometry(null, fillPen, geo);
    }
}
