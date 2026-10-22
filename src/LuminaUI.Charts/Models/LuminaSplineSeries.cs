namespace LuminaUI.Charts;

/// <summary>
/// A line series rendered through the points with a Catmull-Rom spline for a smooth curve.
/// </summary>
public sealed class LuminaSplineSeries : LuminaChartSeries
{
    private int _samplesPerSegment = 12;

    /// <summary>Samples emitted per source segment. Higher values produce a smoother curve. Default 12.</summary>
    public int SamplesPerSegment
    {
        get => _samplesPerSegment;
        set => Set(ref _samplesPerSegment, value);
    }
}
