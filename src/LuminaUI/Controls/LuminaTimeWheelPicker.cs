using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LuminaUI.Localization;

namespace LuminaUI.Controls;

internal sealed class LuminaTimeWheelPicker : Grid
{
    private readonly LuminaWheelColumn _hourColumn;

    private readonly LuminaWheelColumn _minuteColumn;

    private readonly LuminaWheelColumn? _secondColumn;

    private readonly LuminaWheelColumn? _periodColumn;

    private readonly TextBlock _summaryText;

    private readonly bool _useSeconds;

    private readonly bool _use12HourClock;

    private readonly int _minuteIncrement;

    private readonly int _secondIncrement;

    private int _hour;

    private int _minute;

    private int _second;

    private bool _isPm;

    private bool _isUpdating;

    public TimeSpan SelectedTime => new TimeSpan(_hour, _minute, _useSeconds ? _second : 0);

    public LuminaTimeWheelPicker(TimeSpan selectedTime, int minuteIncrement, int secondIncrement, bool useSeconds, bool use12HourClock)
    {
        _minuteIncrement = NormalizeIncrement(minuteIncrement);
        _secondIncrement = NormalizeIncrement(secondIncrement);
        _useSeconds = useSeconds;
        _use12HourClock = use12HourClock;
        _hour = Math.Clamp(selectedTime.Hours, 0, 23);
        _minute = selectedTime.Minutes;
        _second = selectedTime.Seconds;
        _isPm = _hour >= 12;
        RowDefinitions = new RowDefinitions("Auto,Auto");
        RowSpacing = 14.0;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _summaryText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 15.0,
            FontWeight = FontWeight.DemiBold
        };
        _summaryText.Classes.Add("LuminaWheelSummaryText");
        Border summary = new Border
        {
            Padding = new Thickness(14.0, 10.0),
            CornerRadius = new CornerRadius(999.0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = _summaryText
        };
        summary.Background = LuminaPickerResources.Brush("LuminaPrimaryBgBrush", Brushes.Transparent);
        _summaryText.Foreground = LuminaPickerResources.Brush("LuminaTextPrimaryBrush", Brushes.White);
        summary.Classes.Add("LuminaWheelSummary");
        Children.Add(summary);
        Grid wheelGrid = new Grid
        {
            ColumnSpacing = 8.0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetRow(wheelGrid, 1);
        Children.Add(wheelGrid);
        _hourColumn = AddColumn(wheelGrid, LuminaLocalization.Get("Lumina.Picker.Hour"));
        _minuteColumn = AddColumn(wheelGrid, LuminaLocalization.Get("Lumina.Picker.Minute"));
        _secondColumn = _useSeconds ? AddColumn(wheelGrid, LuminaLocalization.Get("Lumina.Picker.Second")) : null;
        _periodColumn = _use12HourClock ? AddColumn(wheelGrid, LuminaLocalization.Get("Lumina.Picker.Period")) : null;
        _hourColumn.ValueChanged += (int value) =>
        {
            if (!_isUpdating)
            {
                _hour = _use12HourClock ? To24Hour(value, _isPm) : value;
                UpdateSummary();
            }
        };
        _minuteColumn.ValueChanged += (int value) =>
        {
            if (!_isUpdating)
            {
                _minute = value;
                UpdateSummary();
            }
        };
        if (_secondColumn != null)
        {
            _secondColumn.ValueChanged += (int value) =>
            {
                if (!_isUpdating)
                {
                    _second = value;
                    UpdateSummary();
                }
            };
        }
        if (_periodColumn != null)
        {
            _periodColumn.ValueChanged += (int value) =>
            {
                if (!_isUpdating)
                {
                    _isPm = value == 1;
                    _hour = To24Hour(To12Hour(_hour), _isPm);
                    UpdateSummary();
                }
            };
        }
        SyncAll();
    }

    private static LuminaWheelColumn AddColumn(Grid grid, string label)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        LuminaWheelColumn column = new LuminaWheelColumn(label);
        Grid.SetColumn(column, grid.ColumnDefinitions.Count - 1);
        grid.Children.Add(column);
        return column;
    }

    private void SyncAll()
    {
        _isUpdating = true;
        _hourColumn.SetOptions(CreateHourOptions(), _use12HourClock ? To12Hour(_hour) : _hour);
        _minuteColumn.SetOptions(CreateSteppedOptions(0, 59, _minuteIncrement), _minute);
        _minute = _minuteColumn.SelectedValue;
        if (_secondColumn != null)
        {
            _secondColumn.SetOptions(CreateSteppedOptions(0, 59, _secondIncrement), _second);
            _second = _secondColumn.SelectedValue;
        }
        _periodColumn?.SetOptions(new LuminaWheelOption[2]
        {
            new LuminaWheelOption(0, GetPeriodText(isPm: false)),
            new LuminaWheelOption(1, GetPeriodText(isPm: true))
        }, _isPm ? 1 : 0);
        _isUpdating = false;
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        string format = _use12HourClock
            ? (_useSeconds ? LuminaLocalization.Get("Lumina.Picker.TimeSummaryFormat12HourSeconds") : LuminaLocalization.Get("Lumina.Picker.TimeSummaryFormat12Hour"))
            : (_useSeconds ? "HH:mm:ss" : "HH:mm");
        _summaryText.Text = DateTime.Today.Add(SelectedTime).ToString(format, LuminaLocalization.CurrentCulture);
    }

    private IReadOnlyList<LuminaWheelOption> CreateHourOptions()
    {
        return _use12HourClock ? CreateRange(1, 12, (int value) => value.ToString(CultureInfo.CurrentCulture)) : CreateRange(0, 23, (int value) => value.ToString("00", CultureInfo.CurrentCulture));
    }

    private static IReadOnlyList<LuminaWheelOption> CreateSteppedOptions(int start, int end, int increment)
    {
        List<LuminaWheelOption> options = new List<LuminaWheelOption>();
        for (int value = start; value <= end; value += increment)
        {
            options.Add(new LuminaWheelOption(value, value.ToString("00", CultureInfo.CurrentCulture)));
        }
        return options;
    }

    private static IReadOnlyList<LuminaWheelOption> CreateRange(int start, int end, Func<int, string> format)
    {
        List<LuminaWheelOption> options = new List<LuminaWheelOption>();
        for (int value = start; value <= end; value++)
        {
            options.Add(new LuminaWheelOption(value, format(value)));
        }
        return options;
    }

    private static int NormalizeIncrement(int increment)
    {
        return (increment < 1 || increment > 59) ? 1 : increment;
    }

    private static int To12Hour(int hour)
    {
        int value = hour % 12;
        return (value == 0) ? 12 : value;
    }

    private static int To24Hour(int hour, bool isPm)
    {
        if (hour == 12)
        {
            return isPm ? 12 : 0;
        }
        return isPm ? (hour + 12) : hour;
    }

    private static string GetPeriodText(bool isPm)
    {
        DateTimeFormatInfo format = LuminaLocalization.CurrentCulture.DateTimeFormat;
        string text = isPm ? format.PMDesignator : format.AMDesignator;
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        return LuminaLocalization.Get(isPm ? "Lumina.Picker.Pm" : "Lumina.Picker.Am");
    }
}
