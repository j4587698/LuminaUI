using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>
/// A single polygon series of a <see cref="LuminaRadarChart"/>. Property changes are observable,
/// so mutating a series in place repaints the chart.
/// </summary>
public sealed class LuminaRadarSeries : INotifyPropertyChanged
{
    private string? _title;
    private IList<double>? _values;
    private IBrush? _stroke;
    private double _strokeThickness = 1.5;
    private bool _showArea = true;
    private IBrush? _fill;

    /// <summary>Raised for every property change.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    /// <summary>
    /// One value per category axis, scaled against the largest value across all series (the
    /// outer ring). The radar has no negative side: negative and non-finite values are drawn at
    /// the centre, and a chart whose values are all zero or negative draws no polygons.
    /// </summary>
    public IList<double>? Values
    {
        get => _values;
        set => Set(ref _values, value);
    }

    public IBrush? Stroke
    {
        get => _stroke;
        set => Set(ref _stroke, value);
    }

    public double StrokeThickness
    {
        get => _strokeThickness;
        set => Set(ref _strokeThickness, value);
    }

    public bool ShowArea
    {
        get => _showArea;
        set => Set(ref _showArea, value);
    }

    public IBrush? Fill
    {
        get => _fill;
        set => Set(ref _fill, value);
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
