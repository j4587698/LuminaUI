using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>
/// Drawing helpers shared by the Lumina chart controls: palette resolution, text/brush helpers,
/// and the legend and tooltip chrome.
/// </summary>
internal static class ChartDrawing
{
    internal const double LegendBandHeight = 20.0;
    internal const double LegendBandGap = 6.0;
    internal const double LegendFontSize = 12.0;

    private const double LegendSwatchWidth = 12.0;
    private const double LegendSwatchHeight = 3.0;
    private const double LegendSwatchGap = 6.0;
    private const double LegendItemGap = 14.0;

    /// <summary>
    /// Fallback palette used when the Lumina theme is not loaded. Mirrors
    /// <c>LuminaChartColor1..8</c> (Primary, Teal, Orange, Pink, Indigo, Success, Warning, Danger).
    /// </summary>
    internal static readonly IReadOnlyList<Color> DefaultPalette =
    [
        Color.FromRgb(0x25, 0x63, 0xEB),
        Color.FromRgb(0x0D, 0x94, 0x88),
        Color.FromRgb(0xEA, 0x58, 0x0C),
        Color.FromRgb(0xDB, 0x27, 0x77),
        Color.FromRgb(0x4F, 0x46, 0xE5),
        Color.FromRgb(0x16, 0xA3, 0x4A),
        Color.FromRgb(0xF5, 0x9E, 0x0B),
        Color.FromRgb(0xEF, 0x44, 0x44)
    ];

    /// <summary>Lumina semantic brushes backing <c>LuminaChartColor1..8</c>, in palette order.</summary>
    private static readonly string[] PaletteFallbackKeys =
    [
        "LuminaPrimaryBrush",
        "LuminaTealBrush",
        "LuminaOrangeBrush",
        "LuminaPinkBrush",
        "LuminaIndigoBrush",
        "LuminaSuccessBrush",
        "LuminaWarningBrush",
        "LuminaDangerBrush"
    ];

    /// <summary>
    /// Resolves series color <paramref name="index"/>: an app-defined <c>LuminaChartColor1..8</c>
    /// override first, then the matching Lumina semantic brush (theme-variant and accent aware,
    /// looked up from <paramref name="owner"/>), then the built-in palette when no Lumina theme is
    /// loaded.
    /// </summary>
    internal static IBrush ResolvePaletteBrush(Control owner, int index)
    {
        int slot = index % DefaultPalette.Count;
        if (slot < 0)
        {
            slot += DefaultPalette.Count;
        }

        if (owner.TryFindResource("LuminaChartColor" + (slot + 1), owner.ActualThemeVariant, out object? value) && value is IBrush brush)
        {
            return brush;
        }

        if (owner.TryFindResource(PaletteFallbackKeys[slot], owner.ActualThemeVariant, out object? fallback) && fallback is IBrush fallbackBrush)
        {
            return fallbackBrush;
        }

        return new SolidColorBrush(DefaultPalette[slot]);
    }

    internal static Typeface CreateTypeface(Control owner)
    {
        return new Typeface(TextElement.GetFontFamily(owner), FontStyle.Normal, FontWeight.Normal);
    }

    /// <summary>
    /// Returns <paramref name="value"/> when it is a finite number, otherwise
    /// <paramref name="fallback"/>. Model properties are plain doubles, and a NaN thickness or size
    /// would otherwise flow through Math.Max/Clamp (which propagate NaN) into pens and rects.
    /// </summary>
    internal static double Finite(double value, double fallback) => double.IsFinite(value) ? value : fallback;

    /// <summary>
    /// Desired size of a custom-drawn chart. Charts have no content to size to, so in a finite
    /// slot they ask for nothing and simply take what they are arranged into (unchanged
    /// behaviour); along an unbounded axis (StackPanel, ScrollViewer, Auto row) they fall back to
    /// a default instead of collapsing to zero.
    /// </summary>
    internal static Size MeasureChart(Size availableSize, double defaultWidth, double defaultHeight) =>
        new(
            double.IsInfinity(availableSize.Width) ? defaultWidth : 0.0,
            double.IsInfinity(availableSize.Height) ? defaultHeight : 0.0);

    /// <summary>
    /// The surface colour behind the plot, used to punch out marker centres and hover rings so
    /// they read as outlines instead of solid dots in both light and dark themes.
    /// </summary>
    internal static IBrush ResolveSurfaceBrush(Control owner)
    {
        if (owner.TryFindResource("LuminaChartSurfaceBrush", owner.ActualThemeVariant, out object? value) && value is IBrush brush)
        {
            return brush;
        }

        if (owner.TryFindResource("LuminaSurfaceBrush", owner.ActualThemeVariant, out object? fallback) && fallback is IBrush fallbackBrush)
        {
            return fallbackBrush;
        }

        return Brushes.White;
    }

    internal static FormattedText CreateText(string text, Typeface typeface, IBrush brush, double fontSize)
    {
        return new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            brush);
    }

    /// <summary>Returns a translucent version of <paramref name="source"/> for area fills.</summary>
    internal static IBrush CreateTranslucent(IBrush source, double alpha = 0.3)
    {
        if (source is ISolidColorBrush solid && solid.Color.A > 0)
        {
            return new SolidColorBrush(Color.FromArgb((byte)(solid.Color.A * alpha), solid.Color.R, solid.Color.G, solid.Color.B));
        }

        return Brushes.LightGray;
    }

    /// <summary>
    /// Picks black or white for content drawn on top of <paramref name="source"/> (marker fills,
    /// hover outlines, slice labels), so the result stays readable in both light and dark themes.
    /// </summary>
    internal static IBrush ContrastFor(IBrush? source)
    {
        if (source is ISolidColorBrush solid && solid.Color.A > 0)
        {
            Color color = solid.Color;
            double luminance = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255.0;
            return luminance > 0.6 ? Brushes.Black : Brushes.White;
        }

        return Brushes.White;
    }

    /// <summary>Width of a single legend entry: swatch, gap and label.</summary>
    private static double MeasureLegendItem((FormattedText Text, IBrush Color) item) =>
        LegendSwatchWidth + LegendSwatchGap + item.Text.Width;

    /// <summary>
    /// Splits the legend into rows that fit <paramref name="availableWidth"/>. Every row holds at
    /// least one entry, so an entry wider than the chart still gets its own (clipped) row.
    /// </summary>
    private static List<(int Start, int Count, double Width)> LayoutLegendRows(
        IReadOnlyList<(FormattedText Text, IBrush Color)> items,
        double availableWidth)
    {
        var rows = new List<(int Start, int Count, double Width)>();
        int start = 0;
        double width = 0.0;
        for (int index = 0; index < items.Count; index++)
        {
            double itemWidth = MeasureLegendItem(items[index]);
            double next = index == start ? itemWidth : width + LegendItemGap + itemWidth;
            if (index > start && next > availableWidth)
            {
                rows.Add((start, index - start, width));
                start = index;
                width = itemWidth;
            }
            else
            {
                width = next;
            }
        }

        if (items.Count > start)
        {
            rows.Add((start, items.Count - start, width));
        }

        return rows;
    }

    private static double LegendAvailableWidth(Size bounds) => Math.Max(0.0, bounds.Width - 8.0);

    /// <summary>
    /// Height the legend band needs at the given chart size: one <see cref="LegendBandHeight"/>
    /// per row, since long legends wrap instead of running off the edge.
    /// </summary>
    internal static double MeasureLegendHeight(IReadOnlyList<(FormattedText Text, IBrush Color)> items, Size bounds)
    {
        if (items.Count == 0)
        {
            return 0.0;
        }

        return LayoutLegendRows(items, LegendAvailableWidth(bounds)).Count * LegendBandHeight;
    }

    /// <summary>
    /// Bounds of the legend band for the given items. Callers use it to keep the tooltip out of the
    /// legend's way; <see cref="DrawLegend"/> draws inside exactly this rectangle.
    /// </summary>
    internal static Rect ComputeLegendRect(
        IReadOnlyList<(FormattedText Text, IBrush Color)> items,
        LuminaChartLegendPosition position,
        Size bounds)
    {
        if (items.Count == 0)
        {
            return default;
        }

        List<(int Start, int Count, double Width)> rows = LayoutLegendRows(items, LegendAvailableWidth(bounds));
        double widest = 0.0;
        foreach (var row in rows)
        {
            widest = Math.Max(widest, Math.Min(row.Width, LegendAvailableWidth(bounds)));
        }

        double x = AlignLegendRow(widest, position, bounds);
        double y = LegendTop(rows.Count, position, bounds);
        double height = ((rows.Count - 1) * LegendBandHeight) + LegendFontSize;
        return new Rect(x, y, widest, height);
    }

    private static double AlignLegendRow(double rowWidth, LuminaChartLegendPosition position, Size bounds)
    {
        rowWidth = Math.Min(rowWidth, LegendAvailableWidth(bounds));
        return position switch
        {
            LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.BottomLeft => 4.0,
            LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.BottomCenter
                => Math.Max(0.0, (bounds.Width - rowWidth) / 2.0),
            _ => Math.Max(0.0, bounds.Width - rowWidth - 4.0)
        };
    }

    /// <summary>Text top of the first legend row.</summary>
    private static double LegendTop(int rowCount, LuminaChartLegendPosition position, Size bounds)
    {
        bool isTop = position is LuminaChartLegendPosition.TopLeft
            or LuminaChartLegendPosition.TopCenter
            or LuminaChartLegendPosition.TopRight;
        double inset = (LegendBandHeight - LegendFontSize) / 2.0;
        return isTop
            ? 2.0 + inset
            : bounds.Height - (rowCount * LegendBandHeight) - 2.0 + inset;
    }

    internal static void DrawLegend(
        DrawingContext context,
        IReadOnlyList<(FormattedText Text, IBrush Color)> items,
        LuminaChartLegendPosition position,
        Size bounds)
    {
        if (items.Count == 0)
        {
            return;
        }

        List<(int Start, int Count, double Width)> rows = LayoutLegendRows(items, LegendAvailableWidth(bounds));
        double cursorY = LegendTop(rows.Count, position, bounds);
        foreach (var row in rows)
        {
            double cursorX = AlignLegendRow(row.Width, position, bounds);
            for (int index = row.Start; index < row.Start + row.Count; index++)
            {
                (FormattedText text, IBrush color) = items[index];
                var swatchRect = new Rect(
                    cursorX,
                    cursorY + ((LegendFontSize - LegendSwatchHeight) / 2.0),
                    LegendSwatchWidth,
                    LegendSwatchHeight);
                context.DrawRectangle(color, null, swatchRect, 1.5, 1.5);
                cursorX += LegendSwatchWidth + LegendSwatchGap;
                context.DrawText(text, new Point(cursorX, cursorY));
                cursorX += text.Width + LegendItemGap;
            }

            cursorY += LegendBandHeight;
        }
    }

    /// <summary>
    /// Clamps a tooltip origin into <paramref name="bounds"/> and, when it would land on the legend,
    /// pushes it to the other side of the legend band so the two never overlap.
    /// </summary>
    internal static Point PlaceTooltip(Size size, Point preferred, Size bounds, Rect? legend)
    {
        const double Gap = 4.0;
        double maxX = Math.Max(0.0, bounds.Width - size.Width);
        double maxY = Math.Max(0.0, bounds.Height - size.Height);

        double x = Math.Clamp(preferred.X, 0.0, maxX);
        double y = Math.Clamp(preferred.Y, 0.0, maxY);

        if (legend is { } band && band.Width > 0.0)
        {
            var rect = new Rect(x, y, size.Width, size.Height);
            if (rect.Intersects(band))
            {
                bool legendOnTop = band.Center.Y < bounds.Height / 2.0;
                double shifted = legendOnTop ? band.Bottom + Gap : band.Top - Gap - size.Height;
                double clamped = Math.Clamp(shifted, 0.0, maxY);

                // Only accept the move if it actually clears the band; otherwise slide sideways so
                // the tooltip ends up next to the legend instead of underneath it.
                if (!new Rect(x, clamped, size.Width, size.Height).Intersects(band))
                {
                    y = clamped;
                }
                else
                {
                    double leftOfLegend = band.Left - Gap - size.Width;
                    x = leftOfLegend >= 0.0 ? leftOfLegend : Math.Clamp(band.Right + Gap, 0.0, maxX);
                }
            }
        }

        return new Point(x, y);
    }

    /// <summary>Measures the tooltip box for the given lines.</summary>
    internal static Size MeasureTooltip(IReadOnlyList<FormattedText> lines, double padding)
    {
        double width = 0.0;
        double height = 0.0;
        foreach (FormattedText line in lines)
        {
            width = Math.Max(width, line.Width);
            height += line.Height;
        }

        return new Size(width + (padding * 2.0), height + (padding * 2.0));
    }

    /// <summary>
    /// Border pen for tooltip boxes. The tooltip background equals the card surface, which is the
    /// same colour as the page in the light theme, so an outline is what actually separates it
    /// from the chart underneath.
    /// </summary>
    internal static IPen ResolveTooltipBorderPen(Control owner, double thickness = 1.0)
    {
        if (owner.TryFindResource("LuminaChartTooltipBorderBrush", owner.ActualThemeVariant, out object? value) && value is IBrush brush)
        {
            return new Pen(brush, thickness);
        }

        if (owner.TryFindResource("LuminaBorderStrongBrush", owner.ActualThemeVariant, out object? fallback) && fallback is IBrush fallbackBrush)
        {
            return new Pen(fallbackBrush, thickness);
        }

        return new Pen(new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00)), thickness);
    }

    /// <summary>Draws the tooltip box at <paramref name="origin"/>; the caller clamps the position.</summary>
    internal static void DrawTooltip(
        DrawingContext context,
        IReadOnlyList<FormattedText> lines,
        IBrush background,
        IPen? border,
        double padding,
        double cornerRadius,
        Point origin,
        Size size)
    {
        context.DrawRectangle(background, border, new Rect(origin.X, origin.Y, size.Width, size.Height), cornerRadius, cornerRadius);

        double lineY = origin.Y + padding;
        foreach (FormattedText line in lines)
        {
            context.DrawText(line, new Point(origin.X + padding, lineY));
            lineY += line.Height;
        }
    }
}
