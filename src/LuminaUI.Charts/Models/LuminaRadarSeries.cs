using System.Collections.Generic;
using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>A single polygon series of a <see cref="LuminaRadarChart"/>.</summary>
public sealed class LuminaRadarSeries
{
    public string? Title { get; set; }

    public IList<double>? Values { get; set; }

    public IBrush? Stroke { get; set; }

    public double StrokeThickness { get; set; } = 1.5;

    public bool ShowArea { get; set; } = true;

    public IBrush? Fill { get; set; }
}