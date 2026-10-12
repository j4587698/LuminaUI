using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>A single slice of a <see cref="LuminaPieChart"/>.</summary>
public sealed class LuminaPieSlice
{
    public string? Label { get; set; }

    public double Value { get; set; }

    public IBrush? Brush { get; set; }
}