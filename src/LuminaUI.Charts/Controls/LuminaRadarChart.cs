using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>
/// A lookless, custom-drawn radar chart: series are drawn as polygons over a circular grid of
/// category axes.
/// </summary>
public class LuminaRadarChart : Control
{
    private const double TooltipPadding = 8.0;
    private const double TooltipRadius = 6.0;

    private (int Series, int Point)? _hover;

    public static readonly StyledProperty<IList<LuminaRadarSeries>?> SeriesProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IList<LuminaRadarSeries>?>(nameof(Series));

    public static readonly StyledProperty<IReadOnlyList<string>?> CategoriesProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IReadOnlyList<string>?>(nameof(Categories));

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<LuminaRadarChart, bool>(nameof(ShowGrid), true);

    public static readonly StyledProperty<bool> ShowLabelsProperty =
        AvaloniaProperty.Register<LuminaRadarChart, bool>(nameof(ShowLabels), true);

    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(GridLineBrush));

    public static readonly StyledProperty<IBrush?> AxisLabelBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(AxisLabelBrush));

    public static readonly StyledProperty<IBrush?> TooltipBackgroundBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(TooltipBackgroundBrush));

    public static readonly StyledProperty<IBrush?> TooltipTextBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(TooltipTextBrush));

    static LuminaRadarChart()
    {
        AffectsRender<LuminaRadarChart>(
            SeriesProperty,
            CategoriesProperty,
            ShowGridProperty,
            ShowLabelsProperty,
            GridLineBrushProperty,
            AxisLabelBrushProperty,
            TooltipBackgroundBrushProperty,
            TooltipTextBrushProperty);
    }

    public LuminaRadarChart()
    {
        ClipToBounds = true;
    }

    public IList<LuminaRadarSeries>? Series
    {
        get => GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public IReadOnlyList<string>? Categories
    {
        get => GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public bool ShowLabels
    {
        get => GetValue(ShowLabelsProperty);
        set => SetValue(ShowLabelsProperty, value);
    }

    public IBrush? GridLineBrush
    {
        get => GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public IBrush? AxisLabelBrush
    {
        get => GetValue(AxisLabelBrushProperty);
        set => SetValue(AxisLabelBrushProperty, value);
    }

    public IBrush? TooltipBackgroundBrush
    {
        get => GetValue(TooltipBackgroundBrushProperty);
        set => SetValue(TooltipBackgroundBrushProperty, value);
    }

    public IBrush? TooltipTextBrush
    {
        get => GetValue(TooltipTextBrushProperty);
        set => SetValue(TooltipTextBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        IList<LuminaRadarSeries>? series = Series;
        if (series == null || series.Count == 0)
        {
            return;
        }

        int axes = Math.Max(3, GetAxisCount(series));
        if (Categories is { Count: > 0 })
        {
            axes = Math.Max(axes, Categories.Count);
        }

        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);
        double radius = Math.Max(10.0, Math.Min(Bounds.Width, Bounds.Height) / 2.0 - 26.0);
        double maxValue = GetMaxValue(series);

        if (ShowGrid)
        {
            DrawGrid(context, center, radius, axes, maxValue);
        }

        if (ShowLabels && Categories != null)
        {
            DrawLabels(context, center, radius, axes, Categories);
        }

        foreach (LuminaRadarSeries item in series)
        {
            DrawSeries(context, item, center, radius, axes, maxValue);
        }

        if (_hover is { } hover)
        {
            DrawHover(context, series, hover, center, radius, axes, maxValue);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Point position = e.GetPosition(this);
        (int, int)? hit = HitTest(position);
        if (hit != _hover)
        {
            _hover = hit;
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hover != null)
        {
            _hover = null;
            InvalidateVisual();
        }
    }

    private void DrawGrid(DrawingContext context, Point center, double radius, int axes, double maxValue)
    {
        IBrush gridBrush = GridLineBrush ?? Brushes.LightGray;
        var pen = new Pen(gridBrush, 1.0);

        for (int ring = 1; ring <= 4; ring++)
        {
            double r = radius * ring / 4.0;
            var polygon = new List<Point>(axes);
            for (int axis = 0; axis < axes; axis++)
            {
                polygon.Add(PointOnRing(center, r, axis, axes));
            }

            DrawClosedPolygon(context, null, pen, polygon);
        }

        for (int axis = 0; axis < axes; axis++)
        {
            context.DrawLine(pen, center, PointOnRing(center, radius, axis, axes));
        }

        _ = maxValue;
    }

    private void DrawLabels(DrawingContext context, Point center, double radius, int axes, IReadOnlyList<string> categories)
    {
        IBrush labelBrush = AxisLabelBrush ?? Brushes.Gray;
        var typeface = new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Normal);

        for (int axis = 0; axis < axes; axis++)
        {
            if (axis >= categories.Count)
            {
                break;
            }

            Point p = PointOnRing(center, radius + 8.0, axis, axes);
            var text = new FormattedText(categories[axis], CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12.0, labelBrush);
            context.DrawText(text, new Point(p.X - text.Width / 2.0, p.Y - text.Height / 2.0));
        }
    }

    private void DrawSeries(
        DrawingContext context,
        LuminaRadarSeries series,
        Point center,
        double radius,
        int axes,
        double maxValue)
    {
        IList<double>? values = series.Values;
        if (values == null || values.Count == 0 || maxValue <= 0.0)
        {
            return;
        }

        var points = new List<Point>(axes);
        for (int axis = 0; axis < axes; axis++)
        {
            double value = axis < values.Count ? values[axis] : 0.0;
            points.Add(PointOnRing(center, radius * Math.Clamp(value / maxValue, 0.0, 1.0), axis, axes));
        }

        IBrush stroke = series.Stroke ?? Brushes.SlateGray;
        if (series.ShowArea)
        {
            IBrush fill = series.Fill ?? CreateTranslucent(stroke);
            DrawClosedPolygon(context, fill, null, points);
        }

        DrawClosedPolygon(context, null, new Pen(stroke, Math.Max(0.5, series.StrokeThickness)), points);
    }

    private void DrawHover(
        DrawingContext context,
        IList<LuminaRadarSeries> series,
        (int Series, int Point) hover,
        Point center,
        double radius,
        int axes,
        double maxValue)
    {
        if (hover.Series >= series.Count)
        {
            return;
        }

        LuminaRadarSeries item = series[hover.Series];
        IList<double>? values = item.Values;
        if (values == null || hover.Point >= axes)
        {
            return;
        }

        double value = hover.Point < values.Count ? values[hover.Point] : 0.0;
        Point p = PointOnRing(center, radius * Math.Clamp(value / maxValue, 0.0, 1.0), hover.Point, axes);
        IBrush stroke = item.Stroke ?? Brushes.SlateGray;

        var ringPen = new Pen(stroke, 2.0);
        context.DrawEllipse(Brushes.White, ringPen, p, 4.0, 4.0);

        string label = Categories != null && hover.Point < Categories.Count
            ? Categories[hover.Point]
            : $"Axis {hover.Point + 1}";
        IBrush background = TooltipBackgroundBrush ?? Brushes.SlateGray;
        IBrush textBrush = TooltipTextBrush ?? Brushes.White;
        var typeface = new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Normal);

        string text = $"{label}: {value.ToString("0.##", CultureInfo.CurrentCulture)}";
        var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12.0, textBrush);
        double boxWidth = ft.Width + TooltipPadding * 2.0;
        double boxHeight = ft.Height + TooltipPadding * 2.0;
        double x = Math.Clamp(p.X + 10.0, 0.0, Math.Max(0.0, Bounds.Width - boxWidth));
        double y = Math.Clamp(p.Y - boxHeight - 8.0, 0.0, Math.Max(0.0, Bounds.Height - boxHeight));

        context.DrawRectangle(background, null, new Rect(x, y, boxWidth, boxHeight), TooltipRadius, TooltipRadius);
        context.DrawText(ft, new Point(x + TooltipPadding, y + TooltipPadding));
    }

    private (int, int)? HitTest(Point position)
    {
        IList<LuminaRadarSeries>? series = Series;
        if (series == null || series.Count == 0)
        {
            return null;
        }

        int axes = Math.Max(3, GetAxisCount(series));
        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);
        double radius = Math.Max(10.0, Math.Min(Bounds.Width, Bounds.Height) / 2.0 - 26.0);
        double maxValue = GetMaxValue(series);

        double best = double.PositiveInfinity;
        (int, int)? bestHit = null;
        for (int s = 0; s < series.Count; s++)
        {
            IList<double>? values = series[s].Values;
            if (values == null)
            {
                continue;
            }

            for (int axis = 0; axis < axes; axis++)
            {
                double value = axis < values.Count ? values[axis] : 0.0;
                Point p = PointOnRing(center, radius * Math.Clamp(value / maxValue, 0.0, 1.0), axis, axes);
                double dx = position.X - p.X;
                double dy = position.Y - p.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance <= 20.0 && distance < best)
                {
                    best = distance;
                    bestHit = (s, axis);
                }
            }
        }

        return bestHit;
    }

    private static void DrawClosedPolygon(DrawingContext context, IBrush? fill, IPen? pen, List<Point> points)
    {
        if (points.Count == 0)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (StreamGeometryContext c = geometry.Open())
        {
            c.BeginFigure(points[0], isFilled: fill != null);
            for (int i = 1; i < points.Count; i++)
            {
                c.LineTo(points[i]);
            }

            c.EndFigure(isClosed: true);
        }

        if (fill != null)
        {
            context.DrawGeometry(fill, null, geometry);
        }

        if (pen != null)
        {
            context.DrawGeometry(null, pen, geometry);
        }
    }

    private static Point PointOnRing(Point center, double radius, int axis, int axes)
    {
        double angle = (-90.0 + axis * 360.0 / axes) * Math.PI / 180.0;
        return new Point(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
    }

    private static int GetAxisCount(IList<LuminaRadarSeries> series)
    {
        int count = 0;
        foreach (LuminaRadarSeries item in series)
        {
            if (item.Values != null && item.Values.Count > count)
            {
                count = item.Values.Count;
            }
        }

        return count;
    }

    private static double GetMaxValue(IList<LuminaRadarSeries> series)
    {
        double max = 0.0;
        foreach (LuminaRadarSeries item in series)
        {
            if (item.Values == null)
            {
                continue;
            }

            foreach (double value in item.Values)
            {
                if (double.IsFinite(value) && value > max)
                {
                    max = value;
                }
            }
        }

        return max;
    }

    private static IBrush CreateTranslucent(IBrush source)
    {
        if (source is ISolidColorBrush solid && solid.Color.A > 0)
        {
            return new SolidColorBrush(Color.FromArgb((byte)(solid.Color.A * 0.3), solid.Color.R, solid.Color.G, solid.Color.B));
        }

        return Brushes.LightGray;
    }
}