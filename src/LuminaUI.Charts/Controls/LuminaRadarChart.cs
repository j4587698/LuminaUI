using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
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
    private INotifyCollectionChanged? _seriesSubscription;
    private HashSet<LuminaRadarSeries> _seriesSubscriptions = new();
    private HashSet<INotifyCollectionChanged> _valuesSubscriptions = new();
    private readonly List<(FormattedText Text, IBrush Color)> _legendItems = new();
    private Rect _legendRect;

    public static readonly StyledProperty<IList<LuminaRadarSeries>?> SeriesProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IList<LuminaRadarSeries>?>(nameof(Series));

    public static readonly StyledProperty<IReadOnlyList<string>?> CategoriesProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IReadOnlyList<string>?>(nameof(Categories));

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<LuminaRadarChart, bool>(nameof(ShowGrid), true);

    public static readonly StyledProperty<bool> ShowLabelsProperty =
        AvaloniaProperty.Register<LuminaRadarChart, bool>(nameof(ShowLabels), true);

    public static readonly StyledProperty<bool> ShowLegendProperty =
        AvaloniaProperty.Register<LuminaRadarChart, bool>(nameof(ShowLegend), false);

    public static readonly StyledProperty<LuminaChartLegendPosition> LegendPositionProperty =
        AvaloniaProperty.Register<LuminaRadarChart, LuminaChartLegendPosition>(nameof(LegendPosition), LuminaChartLegendPosition.TopRight);

    public static readonly StyledProperty<double> HoverThresholdProperty =
        AvaloniaProperty.Register<LuminaRadarChart, double>(nameof(HoverThreshold), 20.0);

    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(GridLineBrush));

    public static readonly StyledProperty<IBrush?> AxisLabelBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(AxisLabelBrush));

    public static readonly StyledProperty<IBrush?> TooltipBackgroundBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(TooltipBackgroundBrush));

    public static readonly StyledProperty<IBrush?> TooltipTextBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(TooltipTextBrush));

    public static readonly StyledProperty<IBrush?> LegendTextBrushProperty =
        AvaloniaProperty.Register<LuminaRadarChart, IBrush?>(nameof(LegendTextBrush));

    static LuminaRadarChart()
    {
        AffectsRender<LuminaRadarChart>(
            SeriesProperty,
            CategoriesProperty,
            ShowGridProperty,
            ShowLabelsProperty,
            ShowLegendProperty,
            LegendPositionProperty,
            GridLineBrushProperty,
            AxisLabelBrushProperty,
            TooltipBackgroundBrushProperty,
            TooltipTextBrushProperty,
            LegendTextBrushProperty);
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

    /// <summary>Gets or sets whether to render a legend identifying each series by title and color.</summary>
    public bool ShowLegend
    {
        get => GetValue(ShowLegendProperty);
        set => SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets where the legend is placed.</summary>
    public LuminaChartLegendPosition LegendPosition
    {
        get => GetValue(LegendPositionProperty);
        set => SetValue(LegendPositionProperty, value);
    }

    /// <summary>Gets or sets the pointer distance, in pixels, that activates a point tooltip.</summary>
    public double HoverThreshold
    {
        get => GetValue(HoverThresholdProperty);
        set => SetValue(HoverThresholdProperty, value);
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

    /// <summary>Gets or sets the legend text brush.</summary>
    public IBrush? LegendTextBrush
    {
        get => GetValue(LegendTextBrushProperty);
        set => SetValue(LegendTextBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        IList<LuminaRadarSeries>? series = Series;
        if (series == null || series.Count == 0)
        {
            return;
        }

        int axes = GetAxisCount(series);
        if (Categories is { Count: > 0 })
        {
            axes = Math.Max(axes, Categories.Count);
        }

        if (axes < 3)
        {
            return;
        }

        var (center, radius) = ComputeLayout();
        double maxValue = GetMaxValue(series);

        if (ShowGrid)
        {
            DrawGrid(context, center, radius, axes);
        }

        if (ShowLabels && Categories != null)
        {
            DrawLabels(context, center, radius, axes, Categories);
        }

        for (int index = 0; index < series.Count; index++)
        {
            LuminaRadarSeries item = series[index];
            if (item == null)
            {
                continue;
            }

            DrawSeries(context, item, index, center, radius, axes, maxValue);
        }

        if (ShowLegend)
        {
            // Measured before the tooltip so the tooltip can be kept clear of the legend band.
            BuildLegendItems(series, _legendItems);
            _legendRect = ChartDrawing.ComputeLegendRect(
                _legendItems,
                ChartDrawing.MeasureLegend(_legendItems),
                LegendPosition,
                Bounds.Size);
        }
        else
        {
            _legendItems.Clear();
            _legendRect = default;
        }

        if (_hover is { } hover)
        {
            DrawHover(context, series, hover, center, radius, axes, maxValue);
        }

        if (ShowLegend)
        {
            DrawLegend(context);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateSubscriptions();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_seriesSubscription != null)
        {
            _seriesSubscription.CollectionChanged -= OnSeriesCollectionChanged;
            _seriesSubscription = null;
        }

        foreach (LuminaRadarSeries series in _seriesSubscriptions)
        {
            series.PropertyChanged -= OnSeriesPropertyChanged;
        }

        foreach (INotifyCollectionChanged values in _valuesSubscriptions)
        {
            values.CollectionChanged -= OnValuesChanged;
        }

        _seriesSubscriptions.Clear();
        _valuesSubscriptions.Clear();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeriesProperty)
        {
            _hover = null;
            UpdateSubscriptions();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        (int, int)? hit = HitTest(e.GetPosition(this));
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

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _hover = null;
        UpdateSubscriptions();
        InvalidateVisual();
    }

    private void OnSeriesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LuminaRadarSeries.Values))
        {
            UpdateSubscriptions();
        }

        _hover = null;
        InvalidateVisual();
    }

    private void OnValuesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _hover = null;
        InvalidateVisual();
    }

    /// <summary>
    /// Keeps change subscriptions in sync with the current series list: the collection itself,
    /// each series (color, thickness, visibility) and each observable <c>Values</c> collection.
    /// </summary>
    private void UpdateSubscriptions()
    {
        if (_seriesSubscription != null)
        {
            _seriesSubscription.CollectionChanged -= OnSeriesCollectionChanged;
            _seriesSubscription = null;
        }

        var desiredSeries = new HashSet<LuminaRadarSeries>();
        var desiredValues = new HashSet<INotifyCollectionChanged>();
        IList<LuminaRadarSeries>? series = Series;
        if (series != null)
        {
            foreach (LuminaRadarSeries item in series)
            {
                if (item == null)
                {
                    continue;
                }

                desiredSeries.Add(item);
                if (item.Values is INotifyCollectionChanged values)
                {
                    desiredValues.Add(values);
                }
            }
        }

        foreach (LuminaRadarSeries stale in _seriesSubscriptions)
        {
            if (!desiredSeries.Contains(stale))
            {
                stale.PropertyChanged -= OnSeriesPropertyChanged;
            }
        }

        foreach (LuminaRadarSeries add in desiredSeries)
        {
            if (!_seriesSubscriptions.Contains(add))
            {
                add.PropertyChanged += OnSeriesPropertyChanged;
            }
        }

        foreach (INotifyCollectionChanged stale in _valuesSubscriptions)
        {
            if (!desiredValues.Contains(stale))
            {
                stale.CollectionChanged -= OnValuesChanged;
            }
        }

        foreach (INotifyCollectionChanged add in desiredValues)
        {
            if (!_valuesSubscriptions.Contains(add))
            {
                add.CollectionChanged += OnValuesChanged;
            }
        }

        _seriesSubscriptions = desiredSeries;
        _valuesSubscriptions = desiredValues;

        if (series is INotifyCollectionChanged observable)
        {
            _seriesSubscription = observable;
            _seriesSubscription.CollectionChanged += OnSeriesCollectionChanged;
        }
    }

    private (Point Center, double Radius) ComputeLayout()
    {
        bool legendTop = ShowLegend &&
            (LegendPosition is LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.TopRight);
        double legendSpace = ShowLegend ? ChartDrawing.LegendBandHeight + ChartDrawing.LegendBandGap : 0.0;
        double topPad = legendTop ? legendSpace : 4.0;
        double bottomPad = (!legendTop && ShowLegend) ? legendSpace : 4.0;

        var center = new Point(Bounds.Width / 2.0, topPad + ((Bounds.Height - topPad - bottomPad) / 2.0));
        double radius = Math.Max(10.0, (Math.Min(Bounds.Width, Bounds.Height - topPad - bottomPad) / 2.0) - 26.0);
        return (center, radius);
    }

    private void DrawGrid(DrawingContext context, Point center, double radius, int axes)
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
    }

    private void DrawLabels(DrawingContext context, Point center, double radius, int axes, IReadOnlyList<string> categories)
    {
        IBrush labelBrush = AxisLabelBrush ?? Brushes.Gray;
        Typeface typeface = ChartDrawing.CreateTypeface(this);

        for (int axis = 0; axis < axes; axis++)
        {
            if (axis >= categories.Count)
            {
                break;
            }

            Point p = PointOnRing(center, radius + 8.0, axis, axes);
            FormattedText text = ChartDrawing.CreateText(categories[axis] ?? string.Empty, typeface, labelBrush, 12.0);
            context.DrawText(text, new Point(p.X - (text.Width / 2.0), p.Y - (text.Height / 2.0)));
        }
    }

    private void DrawSeries(
        DrawingContext context,
        LuminaRadarSeries series,
        int index,
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
            double ratio = Normalize(axis < values.Count ? values[axis] : 0.0, maxValue);
            points.Add(PointOnRing(center, radius * ratio, axis, axes));
        }

        IBrush stroke = series.Stroke ?? ChartDrawing.ResolvePaletteBrush(this, index);
        if (series.ShowArea)
        {
            IBrush fill = series.Fill ?? ChartDrawing.CreateTranslucent(stroke);
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
        if (maxValue <= 0.0 || hover.Series >= series.Count || hover.Point >= axes)
        {
            return;
        }

        LuminaRadarSeries item = series[hover.Series];
        IList<double>? values = item.Values;
        if (item == null || values == null)
        {
            return;
        }

        double value = hover.Point < values.Count ? values[hover.Point] : 0.0;
        Point p = PointOnRing(center, radius * Normalize(value, maxValue), hover.Point, axes);
        IBrush stroke = item.Stroke ?? ChartDrawing.ResolvePaletteBrush(this, hover.Series);

        var ringPen = new Pen(stroke, 2.0);
        context.DrawEllipse(ChartDrawing.ResolveSurfaceBrush(this), ringPen, p, 4.0, 4.0);

        string label = Categories != null && hover.Point < Categories.Count
            ? Categories[hover.Point] ?? $"Axis {hover.Point + 1}"
            : $"Axis {hover.Point + 1}";
        IBrush background = TooltipBackgroundBrush ?? Brushes.SlateGray;
        IBrush textBrush = TooltipTextBrush ?? Brushes.White;
        Typeface typeface = ChartDrawing.CreateTypeface(this);

        string text = $"{label}: {value.ToString("0.##", CultureInfo.CurrentCulture)}";
        var lines = new List<FormattedText>(1)
        {
            ChartDrawing.CreateText(text, typeface, textBrush, ChartDrawing.LegendFontSize)
        };

        Size size = ChartDrawing.MeasureTooltip(lines, TooltipPadding);
        Point origin = ChartDrawing.PlaceTooltip(
            size,
            new Point(p.X + 10.0, p.Y - size.Height - 8.0),
            Bounds.Size,
            ShowLegend && _legendItems.Count > 0 ? _legendRect : null);

        ChartDrawing.DrawTooltip(context, lines, background, ChartDrawing.ResolveTooltipBorderPen(this), TooltipPadding, TooltipRadius, origin, size);
    }

    private void DrawLegend(DrawingContext context)
    {
        ChartDrawing.DrawLegend(
            context,
            _legendItems,
            ChartDrawing.MeasureLegend(_legendItems),
            LegendPosition,
            Bounds.Size);
    }

    /// <summary>Rebuilds <paramref name="target"/> with one entry per non-null series.</summary>
    private void BuildLegendItems(IList<LuminaRadarSeries> series, List<(FormattedText Text, IBrush Color)> target)
    {
        IBrush textBrush = LegendTextBrush ?? Brushes.Gray;
        Typeface typeface = ChartDrawing.CreateTypeface(this);
        target.Clear();

        for (int index = 0; index < series.Count; index++)
        {
            LuminaRadarSeries item = series[index];
            if (item == null)
            {
                continue;
            }

            string title = string.IsNullOrEmpty(item.Title) ? $"Series {index + 1}" : item.Title!;
            FormattedText text = ChartDrawing.CreateText(title, typeface, textBrush, ChartDrawing.LegendFontSize);
            target.Add((text, item.Stroke ?? ChartDrawing.ResolvePaletteBrush(this, index)));
        }
    }

    private (int, int)? HitTest(Point position)
    {
        IList<LuminaRadarSeries>? series = Series;
        if (series == null || series.Count == 0)
        {
            return null;
        }

        int axes = GetAxisCount(series);
        if (Categories is { Count: > 0 })
        {
            // Must match Render(): categories can add axes that no series has a value for.
            axes = Math.Max(axes, Categories.Count);
        }

        var (center, radius) = ComputeLayout();
        double maxValue = GetMaxValue(series);
        if (axes < 3 || maxValue <= 0.0)
        {
            return null;
        }

        double threshold = Math.Max(1.0, HoverThreshold);
        double best = double.PositiveInfinity;
        (int, int)? bestHit = null;
        for (int s = 0; s < series.Count; s++)
        {
            IList<double>? values = series[s]?.Values;
            if (values == null)
            {
                continue;
            }

            for (int axis = 0; axis < axes; axis++)
            {
                double value = axis < values.Count ? values[axis] : 0.0;
                Point p = PointOnRing(center, radius * Normalize(value, maxValue), axis, axes);
                double dx = position.X - p.X;
                double dy = position.Y - p.Y;
                double distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance <= threshold && distance < best)
                {
                    best = distance;
                    bestHit = (s, axis);
                }
            }
        }

        return bestHit;
    }

    /// <summary>Maps a raw value to 0..1, treating non-finite input as zero.</summary>
    private static double Normalize(double value, double maxValue)
    {
        if (!double.IsFinite(value) || maxValue <= 0.0)
        {
            return 0.0;
        }

        return Math.Clamp(value / maxValue, 0.0, 1.0);
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
        double angle = (-90.0 + (axis * 360.0 / axes)) * Math.PI / 180.0;
        return new Point(center.X + (Math.Cos(angle) * radius), center.Y + (Math.Sin(angle) * radius));
    }

    private static int GetAxisCount(IList<LuminaRadarSeries> series)
    {
        int count = 0;
        foreach (LuminaRadarSeries item in series)
        {
            if (item?.Values != null && item.Values.Count > count)
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
            if (item?.Values == null)
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
}
