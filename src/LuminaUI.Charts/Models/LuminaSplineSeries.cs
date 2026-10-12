namespace LuminaUI.Charts;

/// <summary>
/// A line series rendered through the points with a Catmull-Rom spline for a smooth curve.
/// </summary>
public sealed class LuminaSplineSeries : LuminaChartSeries
{
    /// <summary>Samples emitted per source segment. Higher values produce a smoother curve. Default 12.</summary>
    public int SamplesPerSegment { get; set; } = 12;
}