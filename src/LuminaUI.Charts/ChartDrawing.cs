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

    /// <summary>
    /// Resolves <c>LuminaChartColor1..8</c> from the current theme, falling back to the built-in
    /// palette when the Lumina theme is not loaded.
    /// </summary>
    internal static IBrush ResolvePaletteBrush(Control owner, int index)
    {
        int slot = index % DefaultPalette.Count;
        if (slot < 0)
        {
            slot += DefaultPalette.Count;
        }

        string key = "LuminaChartColor" + (slot + 1);
        if (owner.TryFindResource(key, out object? value) && value is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(DefaultPalette[slot]);
    }

    internal static Typeface CreateTypeface(Control owner)
    {
        return new Typeface(TextElement.GetFontFamily(owner), FontStyle.Normal, FontWeight.Normal);
    }

    /// <summary>
    /// The surface colour behind the plot, used to punch out marker centres and hover rings so
    /// they read as outlines instead of solid dots in both light and dark themes.
    /// </summary>
    internal static IBrush ResolveSurfaceBrush(Control owner)
    {
        if (owner.TryFindResource("LuminaChartSurfaceBrush", out object? value) && value is IBrush brush)
        {
            return brush;
        }

        if (owner.TryFindResource("LuminaSurfaceBrush", out object? fallback) && fallback is IBrush fallbackBrush)
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

    /// <summary>Total width of the legend items, including swatches and gaps.</summary>
    internal static double MeasureLegend(IReadOnlyList<(FormattedText Text, IBrush Color)> items)
    {
        double width = 0.0;
        for (int index = 0; index < items.Count; index++)
        {
            if (index > 0)
            {
                width += LegendItemGap;
            }

            width += LegendSwatchWidth + LegendSwatchGap + items[index].Text.Width;
        }

        return width;
    }

    /// <summary>
    /// Bounds of the legend band for the given items. Callers use it to keep the tooltip out of the
    /// legend's way; <see cref="DrawLegend"/> draws at exactly this rectangle.
    /// </summary>
    internal static Rect ComputeLegendRect(
        IReadOnlyList<(FormattedText Text, IBrush Color)> items,
        double contentWidth,
        LuminaChartLegendPosition position,
        Size bounds)
    {
        if (items.Count == 0)
        {
            return default;
        }

        if (contentWidth > bounds.Width - 8.0)
        {
            contentWidth = bounds.Width - 8.0;
        }

        double x = position switch
        {
            LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.BottomLeft => 4.0,
            LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.BottomCenter
                => Math.Max(0.0, (bounds.Width - contentWidth) / 2.0),
            _ => Math.Max(0.0, bounds.Width - contentWidth - 4.0)
        };

        bool isTop = position is LuminaChartLegendPosition.TopLeft
            or LuminaChartLegendPosition.TopCenter
            or LuminaChartLegendPosition.TopRight;

        double cursorY = isTop
            ? 2.0 + ((LegendBandHeight - LegendFontSize) / 2.0)
            : bounds.Height - LegendBandHeight - 2.0 + ((LegendBandHeight - LegendFontSize) / 2.0);

        return new Rect(x, cursorY, contentWidth, LegendFontSize);
    }

    internal static void DrawLegend(
        DrawingContext context,
        IReadOnlyList<(FormattedText Text, IBrush Color)> items,
        double contentWidth,
        LuminaChartLegendPosition position,
        Size bounds)
    {
        if (items.Count == 0)
        {
            return;
        }

        Rect band = ComputeLegendRect(items, contentWidth, position, bounds);
        double cursorX = band.X;
        double cursorY = band.Y;
        foreach ((FormattedText text, IBrush color) in items)
        {
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
        if (owner.TryFindResource("LuminaChartTooltipBorderBrush", out object? value) && value is IBrush brush)
        {
            return new Pen(brush, thickness);
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
