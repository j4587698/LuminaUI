namespace LuminaUI.Charts;

/// <summary>
/// A line series drawn as a smooth curve through its points.
/// </summary>
/// <remarks>
/// The curve is a monotone cubic (Fritsch–Carlson) interpolation: it passes through every point,
/// never overshoots above or below neighbouring values, and never bends backwards in x, so the
/// tooltip always reports values that lie between the real data. Series whose points are not
/// ordered by x, or that are larger than <see cref="LuminaLineChart.DecimationThreshold"/>, are
/// drawn as a regular (decimated) line instead.
/// </remarks>
public sealed class LuminaSplineSeries : LuminaChartSeries
{
    private int _samplesPerSegment = 12;

    /// <summary>
    /// Samples emitted per source segment. Higher values produce a smoother curve. Default 12;
    /// clamped to 2..32 when drawing.
    /// </summary>
    public int SamplesPerSegment
    {
        get => _samplesPerSegment;
        set => Set(ref _samplesPerSegment, value);
    }
}
