using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Localization;

namespace LuminaUI.Controls;

[TemplatePart("PART_TextBox", typeof(TextBox))]
[TemplatePart("PART_Button", typeof(Button))]
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_Calendar", typeof(LuminaDateRangeCalendar))]
[TemplatePart("PART_HoursList", typeof(ListBox))]
[TemplatePart("PART_MinutesList", typeof(ListBox))]
[TemplatePart("PART_SecondsList", typeof(ListBox))]
[TemplatePart("PART_NowButton", typeof(Button))]
[TemplatePart("PART_ClearButton", typeof(Button))]
[TemplatePart("PART_ConfirmButton", typeof(Button))]
[PseudoClasses(":has-value", ":dropdownopen")]
public class LuminaDateTimePicker : TemplatedControl
{
    private static readonly List<string> HoursSource = Enumerable.Range(0, 24).Select(i => i.ToString("D2")).ToList();
    private static readonly List<string> MinutesSource = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();
    private static readonly List<string> SecondsSource = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();

    private TextBox? _textBox;
    private Button? _button;
    private Popup? _popup;
    private LuminaDateRangeCalendar? _calendar;
    private ListBox? _hoursList;
    private ListBox? _minutesList;
    private ListBox? _secondsList;
    private Button? _nowButton;
    private Button? _clearButton;
    private Button? _confirmButton;

    private bool _isUpdatingInternally;
    private bool _isDropDownOpen;

    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, DateTime?>(
            nameof(SelectedDateTime),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> DateTimeFormatProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, string>(
            nameof(DateTimeFormat),
            "yyyy-MM-dd HH:mm:ss");

    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, string>(
            nameof(PlaceholderText),
            "Select date and time");

    public static readonly StyledProperty<bool> UseSecondsProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, bool>(
            nameof(UseSeconds),
            defaultValue: true);

    public static readonly StyledProperty<bool> ShowNowButtonProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, bool>(
            nameof(ShowNowButton),
            defaultValue: true);

    public static readonly StyledProperty<bool> ShowClearButtonProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, bool>(
            nameof(ShowClearButton),
            defaultValue: true);

    public static readonly StyledProperty<LuminaPopupType> PopupTypeProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, LuminaPopupType>(
            nameof(PopupType),
            LuminaPopupType.Auto);

    public static readonly DirectProperty<LuminaDateTimePicker, bool> IsDropDownOpenProperty =
        AvaloniaProperty.RegisterDirect<LuminaDateTimePicker, bool>(
            nameof(IsDropDownOpen),
            o => o.IsDropDownOpen,
            (o, v) => o.IsDropDownOpen = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> DisplayDateStartProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, DateTime?>(
            nameof(DisplayDateStart));

    public static readonly StyledProperty<DateTime?> DisplayDateEndProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, DateTime?>(
            nameof(DisplayDateEnd));

    public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<LuminaDateTimePicker, ICommand?>(
            nameof(SelectionChangedCommand));

    public static readonly RoutedEvent<RoutedEventArgs> SelectedDateTimeChangedEvent =
        RoutedEvent.Register<LuminaDateTimePicker, RoutedEventArgs>(
            nameof(SelectedDateTimeChanged),
            RoutingStrategies.Bubble);

    public DateTime? SelectedDateTime
    {
        get => GetValue(SelectedDateTimeProperty);
        set => SetValue(SelectedDateTimeProperty, value);
    }

    public string DateTimeFormat
    {
        get => GetValue(DateTimeFormatProperty);
        set => SetValue(DateTimeFormatProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool UseSeconds
    {
        get => GetValue(UseSecondsProperty);
        set => SetValue(UseSecondsProperty, value);
    }

    public bool ShowNowButton
    {
        get => GetValue(ShowNowButtonProperty);
        set => SetValue(ShowNowButtonProperty, value);
    }

    public bool ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    public LuminaPopupType PopupType
    {
        get => GetValue(PopupTypeProperty);
        set => SetValue(PopupTypeProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => _isDropDownOpen;
        set
        {
            if (SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value))
            {
                PseudoClasses.Set(":dropdownopen", value);
                if (value)
                {
                    OnDropDownOpened();
                }
            }
        }
    }

    public DateTime? DisplayDateStart
    {
        get => GetValue(DisplayDateStartProperty);
        set => SetValue(DisplayDateStartProperty, value);
    }

    public DateTime? DisplayDateEnd
    {
        get => GetValue(DisplayDateEndProperty);
        set => SetValue(DisplayDateEndProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? SelectedDateTimeChanged
    {
        add => AddHandler(SelectedDateTimeChangedEvent, value);
        remove => RemoveHandler(SelectedDateTimeChangedEvent, value);
    }

    static LuminaDateTimePicker()
    {
        FocusableProperty.OverrideDefaultValue<LuminaDateTimePicker>(true);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_button != null) _button.Click -= OnButtonClick;
        if (_textBox != null)
        {
            _textBox.LostFocus -= OnTextBoxLostFocus;
            _textBox.KeyDown -= OnTextBoxKeyDown;
        }
        if (_nowButton != null) _nowButton.Click -= OnNowButtonClick;
        if (_clearButton != null) _clearButton.Click -= OnClearButtonClick;
        if (_confirmButton != null) _confirmButton.Click -= OnConfirmButtonClick;
        if (_calendar != null) _calendar.DateSelected -= OnCalendarDateSelected;
        if (_hoursList != null) _hoursList.SelectionChanged -= OnTimeListSelectionChanged;
        if (_minutesList != null) _minutesList.SelectionChanged -= OnTimeListSelectionChanged;
        if (_secondsList != null) _secondsList.SelectionChanged -= OnTimeListSelectionChanged;

        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        _button = e.NameScope.Find<Button>("PART_Button");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _calendar = e.NameScope.Find<LuminaDateRangeCalendar>("PART_Calendar");
        _hoursList = e.NameScope.Find<ListBox>("PART_HoursList");
        _minutesList = e.NameScope.Find<ListBox>("PART_MinutesList");
        _secondsList = e.NameScope.Find<ListBox>("PART_SecondsList");
        _nowButton = e.NameScope.Find<Button>("PART_NowButton");
        _clearButton = e.NameScope.Find<Button>("PART_ClearButton");
        _confirmButton = e.NameScope.Find<Button>("PART_ConfirmButton");

        if (_hoursList != null) _hoursList.ItemsSource = HoursSource;
        if (_minutesList != null) _minutesList.ItemsSource = MinutesSource;
        if (_secondsList != null) _secondsList.ItemsSource = SecondsSource;

        if (_button != null) _button.Click += OnButtonClick;
        if (_textBox != null)
        {
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.KeyDown += OnTextBoxKeyDown;
        }
        if (_nowButton != null) _nowButton.Click += OnNowButtonClick;
        if (_clearButton != null) _clearButton.Click += OnClearButtonClick;
        if (_confirmButton != null) _confirmButton.Click += OnConfirmButtonClick;
        if (_calendar != null) _calendar.DateSelected += OnCalendarDateSelected;
        if (_hoursList != null) _hoursList.SelectionChanged += OnTimeListSelectionChanged;
        if (_minutesList != null) _minutesList.SelectionChanged += OnTimeListSelectionChanged;
        if (_secondsList != null) _secondsList.SelectionChanged += OnTimeListSelectionChanged;

        SyncControlsFromValue(SelectedDateTime);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedDateTimeProperty)
        {
            var newVal = change.GetNewValue<DateTime?>();
            PseudoClasses.Set(":has-value", newVal.HasValue);
            if (!_isUpdatingInternally)
            {
                SyncControlsFromValue(newVal);
            }
            RaiseEvent(new RoutedEventArgs(SelectedDateTimeChangedEvent, this));
            if (SelectionChangedCommand?.CanExecute(newVal) == true)
            {
                SelectionChangedCommand.Execute(newVal);
            }
        }
        else if (change.Property == DateTimeFormatProperty)
        {
            SyncTextBoxFromValue(SelectedDateTime);
        }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        IsDropDownOpen = !IsDropDownOpen;
    }

    private void OnDropDownOpened()
    {
        var current = SelectedDateTime ?? DateTime.Now;
        _isUpdatingInternally = true;
        try
        {
            if (_calendar != null)
            {
                _calendar.DisplayMonth = current;
                _calendar.RangeStart = SelectedDateTime.HasValue ? current.Date : null;
                _calendar.RangeEnd = SelectedDateTime.HasValue ? current.Date : null;
                _calendar.RefreshSelection();
            }

            if (_hoursList != null) _hoursList.SelectedIndex = Math.Clamp(current.Hour, 0, 23);
            if (_minutesList != null) _minutesList.SelectedIndex = Math.Clamp(current.Minute, 0, 59);
            if (_secondsList != null) _secondsList.SelectedIndex = Math.Clamp(current.Second, 0, 59);

            Dispatcher.UIThread.Post(() =>
            {
                ScrollToCenter(_hoursList, _hoursList?.SelectedIndex ?? -1);
                ScrollToCenter(_minutesList, _minutesList?.SelectedIndex ?? -1);
                ScrollToCenter(_secondsList, _secondsList?.SelectedIndex ?? -1);
            }, DispatcherPriority.Loaded);
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private void OnCalendarDateSelected(object? sender, LuminaDateRangeCalendarDateEventArgs e)
    {
        if (_isUpdatingInternally || !e.Date.HasValue) return;

        var date = e.Date.Value;
        var currentTime = SelectedDateTime?.TimeOfDay ?? DateTime.Now.TimeOfDay;
        if (_hoursList?.SelectedIndex >= 0 && _minutesList?.SelectedIndex >= 0)
        {
            int h = _hoursList.SelectedIndex;
            int m = _minutesList.SelectedIndex;
            int s = (UseSeconds && _secondsList?.SelectedIndex >= 0) ? _secondsList.SelectedIndex : 0;
            currentTime = new TimeSpan(h, m, s);
        }

        _isUpdatingInternally = true;
        try
        {
            SelectedDateTime = date.Date + currentTime;
            if (_calendar != null)
            {
                _calendar.RangeStart = date.Date;
                _calendar.RangeEnd = date.Date;
                _calendar.RefreshSelection();
            }
            SyncTextBoxFromValue(SelectedDateTime);
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private void OnTimeListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingInternally) return;

        var baseDate = SelectedDateTime?.Date ?? DateTime.Today;
        int h = _hoursList?.SelectedIndex >= 0 ? _hoursList.SelectedIndex : DateTime.Now.Hour;
        int m = _minutesList?.SelectedIndex >= 0 ? _minutesList.SelectedIndex : DateTime.Now.Minute;
        int s = (UseSeconds && _secondsList?.SelectedIndex >= 0) ? _secondsList.SelectedIndex : (SelectedDateTime?.Second ?? 0);

        _isUpdatingInternally = true;
        try
        {
            SelectedDateTime = baseDate.Add(new TimeSpan(h, m, s));
            if (_calendar != null && !SelectedDateTime.HasValue)
            {
                _calendar.RangeStart = baseDate;
                _calendar.RangeEnd = baseDate;
                _calendar.RefreshSelection();
            }
            SyncTextBoxFromValue(SelectedDateTime);
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private void OnNowButtonClick(object? sender, RoutedEventArgs e)
    {
        SelectedDateTime = DateTime.Now;
        SyncControlsFromValue(SelectedDateTime);
        IsDropDownOpen = false;
    }

    private void OnClearButtonClick(object? sender, RoutedEventArgs e)
    {
        SelectedDateTime = null;
        SyncControlsFromValue(null);
        IsDropDownOpen = false;
    }

    private void OnConfirmButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!SelectedDateTime.HasValue)
        {
            var baseDate = _calendar?.RangeStart ?? DateTime.Today;
            int h = _hoursList?.SelectedIndex >= 0 ? _hoursList.SelectedIndex : DateTime.Now.Hour;
            int m = _minutesList?.SelectedIndex >= 0 ? _minutesList.SelectedIndex : DateTime.Now.Minute;
            int s = (UseSeconds && _secondsList?.SelectedIndex >= 0) ? _secondsList.SelectedIndex : 0;
            SelectedDateTime = baseDate.Date + new TimeSpan(h, m, s);
        }
        SyncTextBoxFromValue(SelectedDateTime);
        IsDropDownOpen = false;
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        CommitTextBoxText();
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitTextBoxText();
            e.Handled = true;
        }
        else if (e.Key == Key.Down && !IsDropDownOpen)
        {
            IsDropDownOpen = true;
            e.Handled = true;
        }
    }

    private void CommitTextBoxText()
    {
        if (_textBox == null) return;
        var text = _textBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            SelectedDateTime = null;
            return;
        }

        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed) ||
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            SelectedDateTime = parsed;
        }
        else
        {
            SyncTextBoxFromValue(SelectedDateTime);
        }
    }

    private void SyncControlsFromValue(DateTime? dt)
    {
        SyncTextBoxFromValue(dt);

        if (_calendar != null)
        {
            _calendar.RangeStart = dt?.Date;
            _calendar.RangeEnd = dt?.Date;
            if (dt.HasValue)
            {
                _calendar.DisplayMonth = dt.Value.Date;
            }
            _calendar.RefreshSelection();
        }

        if (dt.HasValue)
        {
            if (_hoursList != null) _hoursList.SelectedIndex = dt.Value.Hour;
            if (_minutesList != null) _minutesList.SelectedIndex = dt.Value.Minute;
            if (_secondsList != null) _secondsList.SelectedIndex = dt.Value.Second;
        }
        else
        {
            if (_hoursList != null) _hoursList.SelectedIndex = -1;
            if (_minutesList != null) _minutesList.SelectedIndex = -1;
            if (_secondsList != null) _secondsList.SelectedIndex = -1;
        }
    }

    private void SyncTextBoxFromValue(DateTime? dt)
    {
        if (_textBox != null)
        {
            _textBox.Text = dt.HasValue ? dt.Value.ToString(DateTimeFormat) : string.Empty;
        }
    }

    private static void ScrollToCenter(ListBox? list, int index)
    {
        if (list == null || index < 0) return;
        var scrollViewer = list.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer != null)
        {
            const double itemHeight = 38.0;
            const double visibleItems = 6.0;
            double targetY = Math.Max(0, (index - (visibleItems / 2.0 - 0.5)) * itemHeight);
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, targetY);
        }
        else if (list.SelectedItem != null)
        {
            list.ScrollIntoView(list.SelectedItem);
        }
    }
}
