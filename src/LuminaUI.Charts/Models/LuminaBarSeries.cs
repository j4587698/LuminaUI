namespace LuminaUI.Charts;

/// <summary>
/// A series rendered as vertical bars over a category or numeric axis.
/// </summary>
public sealed class LuminaBarSeries : LuminaChartSeries
{
    /// <summary>Fraction of the slot width occupied by each bar (0..1). Default 0.7.</summary>
    public double BarWidthRatio { get; set; } = 0.7;

    /// <summary>Gets or sets the corner radius used for bar tops (rendering only).</summary>
    public double CornerRadius { get; set; }
}