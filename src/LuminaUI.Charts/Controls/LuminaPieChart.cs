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
/// A lookless, custom-drawn pie or donut chart. Each <see cref="LuminaPieSlice"/> is drawn as an
/// annulus segment; setting <see cref="InnerRadiusRatio"/> to 0 renders a classic pie.
/// </summary>
public class LuminaPieChart : Control
{
    private const double TooltipPadding = 8.0;
    private const double TooltipRadius = 6.0;
    private const double LabelFontSize = 12.0;

    private int _hoverSliceIndex = -1;
    private INotifyCollectionChanged? _slicesSubscription;
    private HashSet<LuminaPieSlice> _sliceSubscriptions = new();
    private readonly List<(FormattedText Text, IBrush Color)> _legendItems = new();
    private Rect _legendRect;

    public static readonly StyledProperty<IList<LuminaPieSlice>?> SlicesProperty =
        AvaloniaProperty.Register<LuminaPieChart, IList<LuminaPieSlice>?>(nameof(Slices));

    public static readonly StyledProperty<double> InnerRadiusRatioProperty =
        AvaloniaProperty.Register<LuminaPieChart, double>(nameof(InnerRadiusRatio), 0.55);

    public static readonly StyledProperty<bool> ShowLabelsProperty =
        AvaloniaProperty.Register<LuminaPieChart, bool>(nameof(ShowLabels), false);

    public static readonly StyledProperty<bool> ShowPercentProperty =
        AvaloniaProperty.Register<LuminaPieChart, bool>(nameof(ShowPercent), true);

    public static readonly StyledProperty<bool> ShowLegendProperty =
        AvaloniaProperty.Register<LuminaPieChart, bool>(nameof(ShowLegend), false);

    public static readonly StyledProperty<LuminaChartLegendPosition> LegendPositionProperty =
        AvaloniaProperty.Register<LuminaPieChart, LuminaChartLegendPosition>(nameof(LegendPosition), LuminaChartLegendPosition.TopRight);

    public static readonly StyledProperty<IBrush?> TooltipBackgroundBrushProperty =
        AvaloniaProperty.Register<LuminaPieChart, IBrush?>(nameof(TooltipBackgroundBrush));

    public static readonly StyledProperty<IBrush?> TooltipTextBrushProperty =
        AvaloniaProperty.Register<LuminaPieChart, IBrush?>(nameof(TooltipTextBrush));

    public static readonly StyledProperty<IBrush?> LegendTextBrushProperty =
        AvaloniaProperty.Register<LuminaPieChart, IBrush?>(nameof(LegendTextBrush));

    static LuminaPieChart()
    {
        AffectsRender<LuminaPieChart>(
            SlicesProperty,
            InnerRadiusRatioProperty,
            ShowLabelsProperty,
            ShowPercentProperty,
            ShowLegendProperty,
            LegendPositionProperty,
            TooltipBackgroundBrushProperty,
            TooltipTextBrushProperty,
            LegendTextBrushProperty);
    }

    public LuminaPieChart()
    {
        ClipToBounds = true;
    }

    public IList<LuminaPieSlice>? Slices
    {
        get => GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    public double InnerRadiusRatio
    {
        get => GetValue(InnerRadiusRatioProperty);
        set => SetValue(InnerRadiusRatioProperty, value);
    }

    public bool ShowLabels
    {
        get => GetValue(ShowLabelsProperty);
        set => SetValue(ShowLabelsProperty, value);
    }

    public bool ShowPercent
    {
        get => GetValue(ShowPercentProperty);
        set => SetValue(ShowPercentProperty, value);
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

    /// <summary>Gets or sets whether to render a legend identifying each slice by name and color.</summary>
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

    /// <summary>Gets or sets the legend text brush.</summary>
    public IBrush? LegendTextBrush
    {
        get => GetValue(LegendTextBrushProperty);
        set => SetValue(LegendTextBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        IList<LuminaPieSlice>? slices = Slices;
        if (slices == null || slices.Count == 0)
        {
            return;
        }

        double total = SumValues(slices);
        if (total <= 0.0)
        {
            return;
        }

        var (center, outer) = ComputeLayout();
        double inner = outer * Math.Clamp(InnerRadiusRatio, 0.0, 0.98);
        double startAngle = -90.0;
        Typeface typeface = ChartDrawing.CreateTypeface(this);

        for (int index = 0; index < slices.Count; index++)
        {
            LuminaPieSlice slice = slices[index];
            if (!IsDrawable(slice))
            {
                continue;
            }

            double sweep = slice.Value / total * 360.0;
            StreamGeometry geometry = BuildSlice(center, outer, inner, startAngle, sweep);
            IBrush fill = slice.Brush ?? ChartDrawing.ResolvePaletteBrush(this, index);
            context.DrawGeometry(fill, null, geometry);
            if (index == _hoverSliceIndex)
            {
                var hoverPen = new Pen(ChartDrawing.ContrastFor(fill), 2.0);
                context.DrawGeometry(null, hoverPen, geometry);
            }

            if (ShowLabels)
            {
                double mid = startAngle + sweep / 2.0;
                double radius = inner > 0.0 ? (inner + outer) / 2.0 : outer * 0.62;
                var labelPoint = new Point(
                    center.X + Math.Cos(mid * Math.PI / 180.0) * radius,
                    center.Y + Math.Sin(mid * Math.PI / 180.0) * radius);
                string label = ShowPercent
                    ? Math.Round(slice.Value / total * 100.0) + "%"
                    : slice.Label ?? string.Empty;
                if (!string.IsNullOrEmpty(label))
                {
                    FormattedText text = ChartDrawing.CreateText(label, typeface, ChartDrawing.ContrastFor(fill), LabelFontSize);
                    context.DrawText(text, new Point(labelPoint.X - text.Width / 2.0, labelPoint.Y - text.Height / 2.0));
                }
            }

            startAngle += sweep;
        }

        if (ShowLegend)
        {
            // Measured before the tooltip so the tooltip can be kept clear of the legend band.
            BuildLegendItems(slices, _legendItems);
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

        if (_hoverSliceIndex >= 0 && _hoverSliceIndex < slices.Count)
        {
            DrawTooltip(context, center, slices[_hoverSliceIndex], total);
        }

        if (ShowLegend)
        {
            DrawLegend(context);
        }
    }

    /// <summary>A slice contributes to the chart only when its value is finite and positive.</summary>
    private static bool IsDrawable(LuminaPieSlice slice)
    {
        return slice != null && double.IsFinite(slice.Value) && slice.Value > 0.0;
    }

    private static double SumValues(IList<LuminaPieSlice> slices)
    {
        double total = 0.0;
        foreach (LuminaPieSlice slice in slices)
        {
            if (IsDrawable(slice))
            {
                total += slice.Value;
            }
        }

        return total;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateSubscriptions();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_slicesSubscription != null)
        {
            _slicesSubscription.CollectionChanged -= OnSlicesChanged;
            _slicesSubscription = null;
        }

        foreach (LuminaPieSlice slice in _sliceSubscriptions)
        {
            slice.PropertyChanged -= OnSlicePropertyChanged;
        }

        _sliceSubscriptions.Clear();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SlicesProperty)
        {
            UpdateSubscriptions();
        }
    }

    private void OnSlicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _hoverSliceIndex = -1;
        UpdateSubscriptions();
        InvalidateVisual();
    }

    private void OnSlicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void UpdateSubscriptions()
    {
        if (_slicesSubscription != null)
        {
            _slicesSubscription.CollectionChanged -= OnSlicesChanged;
            _slicesSubscription = null;
        }

        var desired = new HashSet<LuminaPieSlice>();
        IList<LuminaPieSlice>? slices = Slices;
        if (slices != null)
        {
            foreach (LuminaPieSlice slice in slices)
            {
                if (slice != null)
                {
                    desired.Add(slice);
                }
            }
        }

        foreach (LuminaPieSlice stale in _sliceSubscriptions)
        {
            if (!desired.Contains(stale))
            {
                stale.PropertyChanged -= OnSlicePropertyChanged;
            }
        }

        foreach (LuminaPieSlice add in desired)
        {
            if (!_sliceSubscriptions.Contains(add))
            {
                add.PropertyChanged += OnSlicePropertyChanged;
            }
        }

        _sliceSubscriptions = desired;

        if (slices is INotifyCollectionChanged observable)
        {
            _slicesSubscription = observable;
            _slicesSubscription.CollectionChanged += OnSlicesChanged;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Point position = e.GetPosition(this);
        int index = HitTest(position);
        if (index != _hoverSliceIndex)
        {
            _hoverSliceIndex = index;
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoverSliceIndex >= 0)
        {
            _hoverSliceIndex = -1;
            InvalidateVisual();
        }
    }

    private int HitTest(Point position)
    {
        IList<LuminaPieSlice>? slices = Slices;
        if (slices == null || slices.Count == 0)
        {
            return -1;
        }

        double total = SumValues(slices);
        if (total <= 0.0)
        {
            return -1;
        }

        var (center, outer) = ComputeLayout();
        double inner = outer * Math.Clamp(InnerRadiusRatio, 0.0, 0.98);

        double dx = position.X - center.X;
        double dy = position.Y - center.Y;
        double radius = Math.Sqrt(dx * dx + dy * dy);
        if (radius > outer || radius < inner)
        {
            return -1;
        }

        double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI + 90.0;
        angle %= 360.0;
        if (angle < 0.0)
        {
            angle += 360.0;
        }

        if (angle >= 360.0)
        {
            angle = 0.0;
        }

        double start = 0.0;
        for (int index = 0; index < slices.Count; index++)
        {
            double sweep = IsDrawable(slices[index]) ? slices[index].Value / total * 360.0 : 0.0;
            // The epsilon absorbs the accumulated floating point error of the sweep sum, which
            // otherwise leaves the slice that owns the 0/360 seam unclickable.
            if (angle >= start && angle < start + sweep + 1e-9)
            {
                return index;
            }

            start += sweep;
        }

        return -1;
    }

    private (Point Center, double Outer) ComputeLayout()
    {
        bool legendTop = ShowLegend &&
            (LegendPosition is LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.TopRight);
        double legendSpace = ShowLegend ? ChartDrawing.LegendBandHeight + ChartDrawing.LegendBandGap : 0.0;
        double topPad = legendTop ? legendSpace : 8.0;
        double bottomPad = (!legendTop && ShowLegend) ? legendSpace : 8.0;

        var center = new Point(Bounds.Width / 2.0, topPad + (Bounds.Height - topPad - bottomPad) / 2.0);
        double outer = Math.Max(2.0, Math.Min(Bounds.Width, Bounds.Height - topPad - bottomPad) / 2.0 - 4.0);
        return (center, outer);
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

    /// <summary>Rebuilds <paramref name="target"/> with one entry per drawable slice.</summary>
    private void BuildLegendItems(IList<LuminaPieSlice> slices, List<(FormattedText Text, IBrush Color)> target)
    {
        IBrush textBrush = LegendTextBrush ?? Brushes.Gray;
        Typeface typeface = ChartDrawing.CreateTypeface(this);
        target.Clear();

        for (int index = 0; index < slices.Count; index++)
        {
            LuminaPieSlice slice = slices[index];
            if (!IsDrawable(slice))
            {
                continue;
            }

            FormattedText text = ChartDrawing.CreateText(
                slice.Label ?? "Slice",
                typeface,
                textBrush,
                ChartDrawing.LegendFontSize);
            target.Add((text, slice.Brush ?? ChartDrawing.ResolvePaletteBrush(this, index)));
        }
    }

    private static StreamGeometry BuildSlice(Point center, double outer, double inner, double startAngle, double sweep)
    {
        if (sweep >= 359.999)
        {
            var fullGeometry = new StreamGeometry();
            using (StreamGeometryContext context = fullGeometry.Open())
            {
                var outerTop = new Point(center.X, center.Y - outer);
                var outerBottom = new Point(center.X, center.Y + outer);
                context.BeginFigure(outerTop, isFilled: true);
                context.ArcTo(outerBottom, new Size(outer, outer), 0.0, false, SweepDirection.Clockwise);
                context.ArcTo(outerTop, new Size(outer, outer), 0.0, false, SweepDirection.Clockwise);
                if (inner > 0.5)
                {
                    var innerTop = new Point(center.X, center.Y - inner);
                    var innerBottom = new Point(center.X, center.Y + inner);
                    context.LineTo(innerTop);
                    context.ArcTo(innerBottom, new Size(inner, inner), 0.0, false, SweepDirection.CounterClockwise);
                    context.ArcTo(innerTop, new Size(inner, inner), 0.0, false, SweepDirection.CounterClockwise);
                }

                context.EndFigure(isClosed: true);
            }

            return fullGeometry;
        }

        bool largeArc = sweep > 180.0;
        double startRad = startAngle * Math.PI / 180.0;
        double endRad = (startAngle + sweep) * Math.PI / 180.0;

        var outerStart = new Point(center.X + Math.Cos(startRad) * outer, center.Y + Math.Sin(startRad) * outer);
        var outerEnd = new Point(center.X + Math.Cos(endRad) * outer, center.Y + Math.Sin(endRad) * outer);

        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(outerStart, isFilled: true);
            context.ArcTo(outerEnd, new Size(outer, outer), 0.0, largeArc, SweepDirection.Clockwise);
            if (inner > 0.5)
            {
                var innerEnd = new Point(center.X + Math.Cos(endRad) * inner, center.Y + Math.Sin(endRad) * inner);
                var innerStart = new Point(center.X + Math.Cos(startRad) * inner, center.Y + Math.Sin(startRad) * inner);
                context.LineTo(innerEnd);
                context.ArcTo(innerStart, new Size(inner, inner), 0.0, largeArc, SweepDirection.CounterClockwise);
            }
            else
            {
                context.LineTo(center);
            }

            context.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private void DrawTooltip(DrawingContext context, Point center, LuminaPieSlice slice, double total)
    {
        IBrush background = TooltipBackgroundBrush ?? Brushes.SlateGray;
        IBrush textBrush = TooltipTextBrush ?? Brushes.White;
        Typeface typeface = ChartDrawing.CreateTypeface(this);

        string percent = ShowPercent ? $" ({Math.Round(slice.Value / total * 100.0)}%)" : string.Empty;
        string line = $"{slice.Label ?? "Slice"}: {slice.Value.ToString("0.##", CultureInfo.CurrentCulture)}{percent}";
        var lines = new List<FormattedText>(1)
        {
            ChartDrawing.CreateText(line, typeface, textBrush, ChartDrawing.LegendFontSize)
        };

        Size size = ChartDrawing.MeasureTooltip(lines, TooltipPadding);
        double x = center.X + 12.0;
        if (x + size.Width > Bounds.Width)
        {
            x = Math.Max(0.0, center.X - size.Width - 12.0);
        }

        Point origin = ChartDrawing.PlaceTooltip(
            size,
            new Point(x, (Bounds.Height / 2.0) - (size.Height / 2.0)),
            Bounds.Size,
            ShowLegend && _legendItems.Count > 0 ? _legendRect : null);

        ChartDrawing.DrawTooltip(context, lines, background, ChartDrawing.ResolveTooltipBorderPen(this), TooltipPadding, TooltipRadius, origin, size);
    }
}