using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LuminaUI.Charts;

/// <summary>
/// The kind of scale an axis uses.
/// </summary>
public enum LuminaChartAxisKind
{
    /// <summary>Smooth, continuous numeric values.</summary>
    Numeric,

    /// <summary>Discrete values mapped to equal-width slots (used by bar charts).</summary>
    Category
}

/// <summary>
/// Configures a single axis of a <see cref="LuminaLineChart"/>. Mutations notify the chart so it
/// can rebuild; bind `Minimum`/`Maximum` to <c>null</c> for automatic scaling.
/// </summary>
public sealed class LuminaChartAxis : INotifyPropertyChanged
{
    private LuminaChartAxisKind _kind = LuminaChartAxisKind.Numeric;
    private double? _minimum;
    private double? _maximum;
    private double? _tickStep;
    private Func<double, string>? _labelFormatter;
    private IReadOnlyList<string>? _categories;

    /// <summary>Raised for every property change.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets whether the axis is numeric or categorical.</summary>
    public LuminaChartAxisKind Kind
    {
        get => _kind;
        set
        {
            if (_kind != value)
            {
                _kind = value;
                Raise(nameof(Kind));
            }
        }
    }

    /// <summary>
    /// Fixed lower bound. <c>null</c> derives the bound from the data (auto-scale).
    /// </summary>
    public double? Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum != value)
            {
                _minimum = value;
                Raise(nameof(Minimum));
            }
        }
    }

    /// <summary>
    /// Fixed upper bound. <c>null</c> derives the bound from the data (auto-scale).
    /// </summary>
    public double? Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum != value)
            {
                _maximum = value;
                Raise(nameof(Maximum));
            }
        }
    }

    /// <summary>Optional fixed tick spacing. <c>null</c> picks a "nice" step.</summary>
    public double? TickStep
    {
        get => _tickStep;
        set
        {
            if (_tickStep != value)
            {
                _tickStep = value;
                Raise(nameof(TickStep));
            }
        }
    }

    /// <summary>Optional label formatter. <c>null</c> uses the default tick formatting.</summary>
    public Func<double, string>? LabelFormatter
    {
        get => _labelFormatter;
        set
        {
            if (!ReferenceEquals(_labelFormatter, value))
            {
                _labelFormatter = value;
                Raise(nameof(LabelFormatter));
            }
        }
    }

    /// <summary>
    /// Category labels used when <see cref="Kind"/> is <see cref="LuminaChartAxisKind.Category"/>.
    /// The label at index <c>i</c> is drawn below the slot containing point index <c>i</c>.
    /// </summary>
    public IReadOnlyList<string>? Categories
    {
        get => _categories;
        set
        {
            if (!ReferenceEquals(_categories, value))
            {
                _categories = value;
                Raise(nameof(Categories));
            }
        }
    }

    private void Raise(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}