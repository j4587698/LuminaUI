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
/// A lookless, custom-drawn pie or donut chart. Each <see cref="LuminaPieSlice"/> is drawn as an
/// annulus segment; setting <see cref="InnerRadiusRatio"/> to 0 renders a classic pie.
/// </summary>
public class LuminaPieChart : Control
{
    private const double TooltipPadding = 8.0;
    private const double TooltipRadius = 6.0;
    private const double LegendBandHeight = 20.0;
    private const double LegendBandGap = 6.0;

    private int _hoverSliceIndex = -1;

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

        double total = 0.0;
        foreach (LuminaPieSlice slice in slices)
        {
            if (double.IsFinite(slice.Value) && slice.Value > 0.0)
            {
                total += slice.Value;
            }
        }

        if (total <= 0.0)
        {
            return;
        }

        var (center, outer) = ComputeLayout();
        double inner = outer * Math.Clamp(InnerRadiusRatio, 0.0, 0.98);
        double startAngle = -90.0;

        for (int index = 0; index < slices.Count; index++)
        {
            LuminaPieSlice slice = slices[index];
            if (!(slice.Value > 0.0))
            {
                continue;
            }

            double sweep = slice.Value / total * 360.0;
            StreamGeometry geometry = BuildSlice(center, outer, inner, startAngle, sweep);
            IBrush fill = slice.Brush ?? Brushes.SlateGray;
            context.DrawGeometry(fill, null, geometry);
            if (index == _hoverSliceIndex)
            {
                var hoverPen = new Pen(Brushes.White, 2.0);
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
                    var text = new FormattedText(
                        label,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Normal),
                        12.0,
                        Brushes.White);
                    context.DrawText(text, new Point(labelPoint.X - text.Width / 2.0, labelPoint.Y - text.Height / 2.0));
                }
            }

            startAngle += sweep;
        }

        if (_hoverSliceIndex >= 0 && _hoverSliceIndex < slices.Count)
        {
            DrawTooltip(context, center, slices[_hoverSliceIndex], total);
        }

        if (ShowLegend)
        {
            DrawLegend(context, slices);
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

        double total = 0.0;
        foreach (LuminaPieSlice slice in slices)
        {
            if (double.IsFinite(slice.Value) && slice.Value > 0.0)
            {
                total += slice.Value;
            }
        }

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
        if (angle < 0.0)
        {
            angle += 360.0;
        }

        double start = 0.0;
        for (int index = 0; index < slices.Count; index++)
        {
            double sweep = slices[index].Value > 0.0 ? slices[index].Value / total * 360.0 : 0.0;
            if (angle >= start && angle < start + sweep)
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
        double legendSpace = ShowLegend ? LegendBandHeight + LegendBandGap : 0.0;
        double topPad = legendTop ? legendSpace : 8.0;
        double bottomPad = (!legendTop && ShowLegend) ? legendSpace : 8.0;

        var center = new Point(Bounds.Width / 2.0, topPad + (Bounds.Height - topPad - bottomPad) / 2.0);
        double outer = Math.Max(2.0, Math.Min(Bounds.Width, Bounds.Height - topPad - bottomPad) / 2.0 - 4.0);
        return (center, outer);
    }

    private void DrawLegend(DrawingContext context, IList<LuminaPieSlice> slices)
    {
        IBrush textBrush = LegendTextBrush ?? Brushes.Gray;
        const double swatchWidth = 12.0;
        const double swatchHeight = 3.0;
        const double swatchGap = 6.0;
        const double itemGap = 14.0;
        const double fontSize = 12.0;
        var typeface = new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Normal);

        var items = new List<(FormattedText Text, IBrush Color)>(slices.Count);
        double contentWidth = 0.0;
        foreach (LuminaPieSlice slice in slices)
        {
            if (!(slice.Value > 0.0))
            {
                continue;
            }

            var text = new FormattedText(
                slice.Label ?? "Slice",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                textBrush);
            items.Add((text, slice.Brush ?? Brushes.SlateGray));
            if (items.Count > 1)
            {
                contentWidth += itemGap;
            }

            contentWidth += swatchWidth + swatchGap + text.Width;
        }

        if (items.Count == 0)
        {
            return;
        }

        if (contentWidth > Bounds.Width - 8.0)
        {
            contentWidth = Bounds.Width - 8.0;
        }

        double x;
        switch (LegendPosition)
        {
            case LuminaChartLegendPosition.TopLeft:
            case LuminaChartLegendPosition.BottomLeft:
                x = 4.0;
                break;
            case LuminaChartLegendPosition.TopCenter:
            case LuminaChartLegendPosition.BottomCenter:
                x = Math.Max(0.0, (Bounds.Width - contentWidth) / 2.0);
                break;
            default:
                x = Math.Max(0.0, Bounds.Width - contentWidth - 4.0);
                break;
        }

        bool isTop = LegendPosition is LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.TopRight;
        double cursorY = isTop
            ? 2.0 + (LegendBandHeight - fontSize) / 2.0
            : Bounds.Height - LegendBandHeight - 2.0 + (LegendBandHeight - fontSize) / 2.0;

        double cursorX = x;
        foreach ((FormattedText Text, IBrush Color) item in items)
        {
            var swatchRect = new Rect(cursorX, cursorY + (fontSize - swatchHeight) / 2.0, swatchWidth, swatchHeight);
            context.DrawRectangle(item.Color, null, swatchRect, 1.5, 1.5);
            cursorX += swatchWidth + swatchGap;
            context.DrawText(item.Text, new Point(cursorX, cursorY));
            cursorX += item.Text.Width + itemGap;
        }
    }

    private static StreamGeometry BuildSlice(Point center, double outer, double inner, double startAngle, double sweep)
    {
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
        var typeface = new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Normal);

        string percent = ShowPercent ? $" ({Math.Round(slice.Value / total * 100.0)}%)" : string.Empty;
        string line = $"{slice.Label ?? "Slice"}: {slice.Value.ToString("0.##", CultureInfo.CurrentCulture)}{percent}";
        var text = new FormattedText(line, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12.0, textBrush);

        double boxWidth = text.Width + TooltipPadding * 2.0;
        double boxHeight = text.Height + TooltipPadding * 2.0;
        double x = center.X + 12.0;
        double y = Math.Max(0.0, Bounds.Height / 2.0 - boxHeight / 2.0);
        if (x + boxWidth > Bounds.Width)
        {
            x = Math.Max(0.0, center.X - boxWidth - 12.0);
        }

        context.DrawRectangle(background, null, new Rect(x, y, boxWidth, boxHeight), TooltipRadius, TooltipRadius);
        context.DrawText(text, new Point(x + TooltipPadding, y + TooltipPadding));
    }
}