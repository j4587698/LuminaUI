namespace LuminaUI.Charts;

/// <summary>
/// A single data point on a Lumina cartesian chart.
/// </summary>
/// <remarks>
/// Points do not raise change notifications. Editing <see cref="X"/> or <see cref="Y"/> on a
/// point that is already displayed does not repaint the chart; replace the point in an observable
/// <c>Points</c> collection (or assign a new collection) instead.
/// </remarks>
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
    /// <c>true</c> when either coordinate is not a finite number (NaN or ±Infinity). Empty points
    /// are skipped: they get no marker, bar or tooltip, and a line or area simply connects the
    /// neighbouring valid points across them. On a category axis the skipped point still keeps
    /// its category slot.
    /// </summary>
    public bool IsEmpty => !double.IsFinite(X) || !double.IsFinite(Y);

    /// <summary>
    /// A point that the chart skips. A fresh instance is returned on every access so callers
    /// cannot corrupt a shared sentinel by mutating it.
    /// </summary>
    public static LuminaChartPoint Empty => new(double.NaN, double.NaN);
}