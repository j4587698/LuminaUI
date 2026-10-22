namespace LuminaUI.Charts;

/// <summary>
/// A series rendered as vertical bars over a category or numeric axis.
/// </summary>
public sealed class LuminaBarSeries : LuminaChartSeries
{
    private double _barWidthRatio = 0.7;
    private double _cornerRadius;

    /// <summary>Fraction of the slot width occupied by each bar (0..1). Default 0.7.</summary>
    public double BarWidthRatio
    {
        get => _barWidthRatio;
        set => Set(ref _barWidthRatio, value);
    }

    /// <summary>Corner radius applied to all four bar corners (rendering only).</summary>
    public double CornerRadius
    {
        get => _cornerRadius;
        set => Set(ref _cornerRadius, value);
    }
}
