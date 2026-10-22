namespace LuminaUI.Charts;

/// <summary>
/// A single data point on a Lumina cartesian chart.
/// </summary>
public sealed class LuminaChartPoint
{
    public LuminaChartPoint()
    {
    }

    public LuminaChartPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; set; }

    public double Y { get; set; }

    /// <summary>
    /// <c>true</c> when either coordinate is not a finite number. Empty points are skipped by the
    /// chart instead of being mapped, so NaN and ±Infinity both punch a hole in a series.
    /// </summary>
    public bool IsEmpty => !double.IsFinite(X) || !double.IsFinite(Y);

    /// <summary>
    /// A point that the chart skips. A fresh instance is returned on every access so callers
    /// cannot corrupt a shared sentinel by mutating it.
    /// </summary>
    public static LuminaChartPoint Empty => new(double.NaN, double.NaN);
}