using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using LuminaUI.Localization;

namespace LuminaUI.Controls;

internal sealed class LuminaDateWheelPicker : Grid
{
    private readonly LuminaWheelColumn? _yearColumn;
    private readonly LuminaWheelColumn? _monthColumn;
    private readonly LuminaWheelColumn? _dayColumn;
    private readonly TextBlock _summaryText;
    private readonly DateTime _minDate;
    private readonly DateTime _maxDate;
    private int _year;
    private int _month;
    private int _day;
    private bool _isUpdating;

    public LuminaDateWheelPicker(
        DateTime selectedDate,
        DateTime minDate,
        DateTime maxDate,
        bool yearVisible = true,
        bool monthVisible = true,
        bool dayVisible = true)
    {
        _minDate = minDate.Date <= maxDate.Date ? minDate.Date : maxDate.Date;
        _maxDate = maxDate.Date >= minDate.Date ? maxDate.Date : minDate.Date;

        var clampedDate = ClampDate(selectedDate.Date);
        _year = clampedDate.Year;
        _month = clampedDate.Month;
        _day = clampedDate.Day;

        RowDefinitions = new RowDefinitions("Auto,Auto");
        RowSpacing = 14;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _summaryText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };
        _summaryText.Classes.Add("LuminaWheelSummaryText");

        var summary = new Border
        {
            Padding = new Thickness(14, 10),
            CornerRadius = new CornerRadius(999),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = _summaryText
        };
        summary.Background = LuminaPickerResources.Brush("LuminaPrimaryBgBrush", Brushes.Transparent);
        _summaryText.Foreground = LuminaPickerResources.Brush("LuminaTextPrimaryBrush", Brushes.White);
        summary.Classes.Add("LuminaWheelSummary");
        Children.Add(summary);

        var wheelGrid = new Grid
        {
            ColumnSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetRow(wheelGrid, 1);
        Children.Add(wheelGrid);

        AddColumn(wheelGrid, yearVisible, LuminaLocalization.Get(LuminaLocalizationKeys.PickerYear), out _yearColumn);
        AddColumn(wheelGrid, monthVisible, LuminaLocalization.Get(LuminaLocalizationKeys.PickerMonth), out _monthColumn);
        AddColumn(wheelGrid, dayVisible, LuminaLocalization.Get(LuminaLocalizationKeys.PickerDay), out _dayColumn);

        if (_yearColumn != null)
        {
            _yearColumn.ValueChanged += value =>
            {
                if (_isUpdating) return;
                _year = value;
                SyncMonthAndDay();
            };
        }

        if (_monthColumn != null)
        {
            _monthColumn.ValueChanged += value =>
            {
                if (_isUpdating) return;
                _month = value;
                SyncDay();
            };
        }

        if (_dayColumn != null)
        {
            _dayColumn.ValueChanged += value =>
            {
                if (_isUpdating) return;
                _day = value;
                UpdateSummary();
            };
        }

        SyncAll();
    }

    public DateTime SelectedDate => ClampDate(new DateTime(_year, _month, _day));

    private static void AddColumn(Grid grid, bool isVisible, string label, out LuminaWheelColumn? column)
    {
        column = null;
        if (!isVisible)
        {
            return;
        }

        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        column = new LuminaWheelColumn(label);
        Grid.SetColumn(column, grid.ColumnDefinitions.Count - 1);
        grid.Children.Add(column);
    }

    private void SyncAll()
    {
        _isUpdating = true;
        _yearColumn?.SetOptions(CreateRange(_minDate.Year, _maxDate.Year, value => value.ToString(CultureInfo.CurrentCulture)), _year);
        SyncMonthOptions();
        SyncDayOptions();
        _isUpdating = false;
        UpdateSummary();
    }

    private void SyncMonthAndDay()
    {
        _isUpdating = true;
        SyncMonthOptions();
        SyncDayOptions();
        _isUpdating = false;
        UpdateSummary();
    }

    private void SyncDay()
    {
        _isUpdating = true;
        SyncDayOptions();
        _isUpdating = false;
        UpdateSummary();
    }

    private void SyncMonthOptions()
    {
        var minMonth = _year == _minDate.Year ? _minDate.Month : 1;
        var maxMonth = _year == _maxDate.Year ? _maxDate.Month : 12;
        _monthColumn?.SetOptions(CreateRange(minMonth, maxMonth, FormatMonth), _month);
        _month = _monthColumn?.SelectedValue ?? Math.Clamp(_month, minMonth, maxMonth);
    }

    private void SyncDayOptions()
    {
        var maxDayInMonth = DateTime.DaysInMonth(_year, _month);
        var minDay = _year == _minDate.Year && _month == _minDate.Month ? _minDate.Day : 1;
        var maxDay = _year == _maxDate.Year && _month == _maxDate.Month ? _maxDate.Day : maxDayInMonth;
        _dayColumn?.SetOptions(CreateRange(minDay, maxDay, value => value.ToString("00", CultureInfo.CurrentCulture)), _day);
        _day = _dayColumn?.SelectedValue ?? Math.Clamp(_day, minDay, maxDay);
    }

    private void UpdateSummary()
    {
        _summaryText.Text = SelectedDate.ToString(
            LuminaLocalization.Get(LuminaLocalizationKeys.PickerDateSummaryFormat),
            LuminaLocalization.CurrentCulture);
    }

    private DateTime ClampDate(DateTime date)
    {
        if (date < _minDate) return _minDate;
        if (date > _maxDate) return _maxDate;
        return date;
    }

    private static string FormatMonth(int month)
    {
        return new DateTime(2000, month, 1).ToString("MMM", CultureInfo.CurrentCulture);
    }

    private static IReadOnlyList<LuminaWheelOption> CreateRange(int start, int end, Func<int, string> format)
    {
        var options = new List<LuminaWheelOption>();
        for (var value = start; value <= end; value++)
        {
            options.Add(new LuminaWheelOption(value, format(value)));
        }

        return options;
    }
}

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

    public LuminaTimeWheelPicker(
        TimeSpan selectedTime,
        int minuteIncrement,
        int secondIncrement,
        bool useSeconds,
        bool use12HourClock)
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
        RowSpacing = 14;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _summaryText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };
        _summaryText.Classes.Add("LuminaWheelSummaryText");

        var summary = new Border
        {
            Padding = new Thickness(14, 10),
            CornerRadius = new CornerRadius(999),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = _summaryText
        };
        summary.Background = LuminaPickerResources.Brush("LuminaPrimaryBgBrush", Brushes.Transparent);
        _summaryText.Foreground = LuminaPickerResources.Brush("LuminaTextPrimaryBrush", Brushes.White);
        summary.Classes.Add("LuminaWheelSummary");
        Children.Add(summary);

        var wheelGrid = new Grid
        {
            ColumnSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetRow(wheelGrid, 1);
        Children.Add(wheelGrid);

        _hourColumn = AddColumn(wheelGrid, LuminaLocalization.Get(LuminaLocalizationKeys.PickerHour));
        _minuteColumn = AddColumn(wheelGrid, LuminaLocalization.Get(LuminaLocalizationKeys.PickerMinute));
        _secondColumn = _useSeconds ? AddColumn(wheelGrid, LuminaLocalization.Get(LuminaLocalizationKeys.PickerSecond)) : null;
        _periodColumn = _use12HourClock ? AddColumn(wheelGrid, LuminaLocalization.Get(LuminaLocalizationKeys.PickerPeriod)) : null;

        _hourColumn.ValueChanged += value =>
        {
            if (_isUpdating) return;
            _hour = _use12HourClock ? To24Hour(value, _isPm) : value;
            UpdateSummary();
        };

        _minuteColumn.ValueChanged += value =>
        {
            if (_isUpdating) return;
            _minute = value;
            UpdateSummary();
        };

        if (_secondColumn != null)
        {
            _secondColumn.ValueChanged += value =>
            {
                if (_isUpdating) return;
                _second = value;
                UpdateSummary();
            };
        }

        if (_periodColumn != null)
        {
            _periodColumn.ValueChanged += value =>
            {
                if (_isUpdating) return;
                _isPm = value == 1;
                _hour = To24Hour(To12Hour(_hour), _isPm);
                UpdateSummary();
            };
        }

        SyncAll();
    }

    public TimeSpan SelectedTime => new(_hour, _minute, _useSeconds ? _second : 0);

    private static LuminaWheelColumn AddColumn(Grid grid, string label)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var column = new LuminaWheelColumn(label);
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

        _periodColumn?.SetOptions(new[]
        {
            new LuminaWheelOption(0, GetPeriodText(isPm: false)),
            new LuminaWheelOption(1, GetPeriodText(isPm: true))
        }, _isPm ? 1 : 0);

        _isUpdating = false;
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var format = _use12HourClock
            ? (_useSeconds
                ? LuminaLocalization.Get(LuminaLocalizationKeys.PickerTimeSummaryFormat12HourSeconds)
                : LuminaLocalization.Get(LuminaLocalizationKeys.PickerTimeSummaryFormat12Hour))
            : (_useSeconds ? "HH:mm:ss" : "HH:mm");

        _summaryText.Text = DateTime.Today.Add(SelectedTime).ToString(format, LuminaLocalization.CurrentCulture);
    }

    private IReadOnlyList<LuminaWheelOption> CreateHourOptions()
    {
        return _use12HourClock
            ? CreateRange(1, 12, value => value.ToString(CultureInfo.CurrentCulture))
            : CreateRange(0, 23, value => value.ToString("00", CultureInfo.CurrentCulture));
    }

    private static IReadOnlyList<LuminaWheelOption> CreateSteppedOptions(int start, int end, int increment)
    {
        var options = new List<LuminaWheelOption>();
        for (var value = start; value <= end; value += increment)
        {
            options.Add(new LuminaWheelOption(value, value.ToString("00", CultureInfo.CurrentCulture)));
        }

        return options;
    }

    private static IReadOnlyList<LuminaWheelOption> CreateRange(int start, int end, Func<int, string> format)
    {
        var options = new List<LuminaWheelOption>();
        for (var value = start; value <= end; value++)
        {
            options.Add(new LuminaWheelOption(value, format(value)));
        }

        return options;
    }

    private static int NormalizeIncrement(int increment)
    {
        return increment is >= 1 and <= 59 ? increment : 1;
    }

    private static int To12Hour(int hour)
    {
        var value = hour % 12;
        return value == 0 ? 12 : value;
    }

    private static int To24Hour(int hour, bool isPm)
    {
        if (hour == 12)
        {
            return isPm ? 12 : 0;
        }

        return isPm ? hour + 12 : hour;
    }

    private static string GetPeriodText(bool isPm)
    {
        var format = LuminaLocalization.CurrentCulture.DateTimeFormat;
        var text = isPm ? format.PMDesignator : format.AMDesignator;
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return LuminaLocalization.Get(isPm ? LuminaLocalizationKeys.PickerPm : LuminaLocalizationKeys.PickerAm);
    }
}

internal sealed class LuminaWheelColumn : Grid
{
    private const double ItemHeight = 44;
    private const int VisibleItemCount = 5;
    private const int CenterRowIndex = VisibleItemCount / 2;
    private readonly Grid _itemsGrid;
    private readonly List<Border> _itemRows = new();
    private readonly List<TextBlock> _itemTexts = new();
    private List<LuminaWheelOption> _options = new();
    private int _selectedIndex;

    public LuminaWheelColumn(string label)
    {
        RowDefinitions = new RowDefinitions("Auto,Auto");
        RowSpacing = 8;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var labelBlock = new TextBlock
        {
            Text = label,
            FontSize = 11,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        labelBlock.Foreground = LuminaPickerResources.Brush("LuminaTextMutedBrush", Brushes.Gray);
        labelBlock.Classes.Add("LuminaWheelColumnLabel");
        Children.Add(labelBlock);

        _itemsGrid = new Grid
        {
            Height = ItemHeight * VisibleItemCount,
            RowDefinitions = new RowDefinitions("44,44,44,44,44"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ZIndex = 2
        };

        for (var rowIndex = 0; rowIndex < VisibleItemCount; rowIndex++)
        {
            var textBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            textBlock.Classes.Add("LuminaWheelItemText");

            var row = new Border
            {
                Height = ItemHeight,
                Background = Brushes.Transparent,
                Child = textBlock
            };
            row.Classes.Add("LuminaWheelItem");
            Grid.SetRow(row, rowIndex);

            var capturedRowIndex = rowIndex;
            row.PointerPressed += (_, e) =>
            {
                SelectIndex(_selectedIndex + capturedRowIndex - CenterRowIndex, true);
                e.Handled = true;
            };

            _itemRows.Add(row);
            _itemTexts.Add(textBlock);
            _itemsGrid.Children.Add(row);
        }

        var selectionBand = new Border
        {
            Height = ItemHeight,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            ZIndex = 1,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Opacity = 0.72
        };
        selectionBand.Background = LuminaPickerResources.Brush("LuminaPrimaryBgBrush", Brushes.Transparent);
        selectionBand.BorderBrush = LuminaPickerResources.Brush("LuminaPrimaryBrush", Brushes.DodgerBlue);
        selectionBand.Classes.Add("LuminaWheelSelectionBand");

        var frame = new Border
        {
            ClipToBounds = true,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Child = new Grid
            {
                Children =
                {
                    selectionBand,
                    _itemsGrid
                }
            }
        };
        frame.Background = LuminaPickerResources.Brush("LuminaSurfaceElevatedBrush", Brushes.Transparent);
        frame.BorderBrush = LuminaPickerResources.Brush("LuminaBorderDefaultBrush", Brushes.Gray);
        frame.Classes.Add("LuminaWheelFrame");
        frame.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        Grid.SetRow(frame, 1);
        Children.Add(frame);
    }

    public event Action<int>? ValueChanged;

    public int SelectedValue { get; private set; }

    public void SetOptions(IReadOnlyList<LuminaWheelOption> options, int preferredValue)
    {
        if (options.Count == 0)
        {
            _options = new List<LuminaWheelOption>();
            _selectedIndex = 0;
            SelectedValue = preferredValue;
            RefreshVisibleItems();
            return;
        }

        _options = options.ToList();
        _selectedIndex = FindNearestIndex(_options, preferredValue);
        SelectedValue = _options[_selectedIndex].Value;
        RefreshVisibleItems();
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_options.Count == 0 || e.Delta.Y == 0)
        {
            return;
        }

        var direction = e.Delta.Y < 0 ? 1 : -1;
        var steps = Math.Clamp((int)Math.Round(Math.Abs(e.Delta.Y)), 1, 3);
        SelectIndex(_selectedIndex + (direction * steps), true);
        e.Handled = true;
    }

    private void SelectIndex(int index, bool notify)
    {
        if (_options.Count == 0)
        {
            return;
        }

        var nextIndex = Math.Clamp(index, 0, _options.Count - 1);
        if (_selectedIndex == nextIndex)
        {
            return;
        }

        _selectedIndex = nextIndex;
        SelectedValue = _options[_selectedIndex].Value;
        RefreshVisibleItems();

        if (notify)
        {
            ValueChanged?.Invoke(SelectedValue);
        }
    }

    private void RefreshVisibleItems()
    {
        for (var rowIndex = 0; rowIndex < VisibleItemCount; rowIndex++)
        {
            var optionIndex = _selectedIndex + rowIndex - CenterRowIndex;
            var row = _itemRows[rowIndex];
            var textBlock = _itemTexts[rowIndex];

            if (optionIndex < 0 || optionIndex >= _options.Count)
            {
                row.Opacity = 0;
                row.IsHitTestVisible = false;
                textBlock.Text = string.Empty;
                continue;
            }

            row.Opacity = rowIndex == CenterRowIndex ? 1 : rowIndex is 1 or 3 ? 0.72 : 0.42;
            row.IsHitTestVisible = true;
            textBlock.Text = _options[optionIndex].Label;

            if (rowIndex == CenterRowIndex)
            {
                textBlock.FontSize = 20;
                textBlock.FontWeight = FontWeight.SemiBold;
                textBlock.Foreground = LuminaPickerResources.Brush("LuminaPrimaryBrush", Brushes.DodgerBlue);
            }
            else
            {
                textBlock.FontSize = rowIndex is 1 or 3 ? 16 : 14;
                textBlock.FontWeight = FontWeight.Medium;
                textBlock.Foreground = LuminaPickerResources.Brush("LuminaTextTertiaryBrush", Brushes.Gray);
            }
        }
    }

    private static int FindNearestIndex(IReadOnlyList<LuminaWheelOption> options, int preferredValue)
    {
        var selectedIndex = 0;
        var selectedDistance = Math.Abs(options[0].Value - preferredValue);

        for (var index = 1; index < options.Count; index++)
        {
            var distance = Math.Abs(options[index].Value - preferredValue);
            if (distance >= selectedDistance)
            {
                continue;
            }

            selectedIndex = index;
            selectedDistance = distance;
        }

        return selectedIndex;
    }
}

internal readonly record struct LuminaWheelOption(int Value, string Label);

internal static class LuminaPickerResources
{
    public static IBrush Brush(string key, IBrush fallback)
    {
        return Application.Current != null &&
               Application.Current.TryFindResource(key, out var resource) &&
               resource is IBrush brush
            ? brush
            : fallback;
    }
}
