using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace LuminaUI.Charts;

/// <summary>
/// A named collection of points drawn as a single line on a <see cref="LuminaLineChart"/>.
/// Property changes are observable, so mutating a series in place repaints the chart.
/// </summary>
public class LuminaChartSeries : INotifyPropertyChanged
{
    private string? _title;
    private IList<LuminaChartPoint>? _points;
    private IBrush? _stroke;
    private double _strokeThickness = 2.0;
    private bool _showArea;
    private IBrush? _fill;
    private bool _showMarkers;
    private bool _isStep;

    public LuminaChartSeries()
    {
        _points = new List<LuminaChartPoint>();
    }

    /// <summary>Raised for every property change.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    public IList<LuminaChartPoint>? Points
    {
        get => _points;
        set => Set(ref _points, value);
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

    public bool ShowMarkers
    {
        get => _showMarkers;
        set => Set(ref _showMarkers, value);
    }

    /// <summary>When <c>true</c>, connects points with horizontal-then-vertical steps instead of straight segments.</summary>
    public bool IsStep
    {
        get => _isStep;
        set => Set(ref _isStep, value);
    }

    /// <summary>Assigns a backing field and raises <see cref="PropertyChanged"/> when the value changed.</summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
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
