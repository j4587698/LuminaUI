using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>
/// A named collection of points drawn as a single line on a <see cref="LuminaLineChart"/>.
/// </summary>
public class LuminaChartSeries
{
    public LuminaChartSeries()
    {
        Points = new List<LuminaChartPoint>();
    }

    public string? Title { get; set; }

    public IList<LuminaChartPoint>? Points { get; set; }

    public IBrush? Stroke { get; set; }

    public double StrokeThickness { get; set; } = 2.0;

    public bool ShowArea { get; set; }

    public IBrush? Fill { get; set; }

    public bool ShowMarkers { get; set; }

    /// <summary>When <c>true</c>, connects points with horizontal-then-vertical steps instead of straight segments.</summary>
    public bool IsStep { get; set; }
}