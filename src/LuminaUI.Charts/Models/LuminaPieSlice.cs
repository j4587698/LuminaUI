using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>
/// A single slice of a <see cref="LuminaPieChart"/>. Property changes are observable, so mutating
/// a slice in place repaints the chart.
/// </summary>
public sealed class LuminaPieSlice : INotifyPropertyChanged
{
    private string? _label;
    private double _value;
    private IBrush? _brush;

    /// <summary>Raised for every property change.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Label
    {
        get => _label;
        set => Set(ref _label, value);
    }

    public double Value
    {
        get => _value;
        set => Set(ref _value, value);
    }

    /// <summary>
    /// Explicit slice brush. When <c>null</c> the slice uses the Lumina chart palette
    /// (<c>LuminaChartColor1..8</c>) based on its position.
    /// </summary>
    public IBrush? Brush
    {
        get => _brush;
        set => Set(ref _brush, value);
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
