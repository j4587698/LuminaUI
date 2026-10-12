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

    public bool IsEmpty => double.IsNaN(X) || double.IsNaN(Y);

    public static readonly LuminaChartPoint Empty = new(double.NaN, double.NaN);
}