namespace LuminaUI.Charts;

/// <summary>
/// A series rendered as discrete point markers without connecting lines.
/// </summary>
public sealed class LuminaScatterSeries : LuminaChartSeries
{
    /// <summary>Marker radius in DIPs. Default 3.</summary>
    public double MarkerSize { get; set; } = 3.0;
}