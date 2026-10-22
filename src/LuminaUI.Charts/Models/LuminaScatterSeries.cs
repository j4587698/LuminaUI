namespace LuminaUI.Charts;

/// <summary>
/// A series rendered as discrete point markers without connecting lines.
/// </summary>
public sealed class LuminaScatterSeries : LuminaChartSeries
{
    private double _markerSize = 3.0;

    /// <summary>Marker radius in DIPs. Default 3.</summary>
    public double MarkerSize
    {
        get => _markerSize;
        set => Set(ref _markerSize, value);
    }
}
