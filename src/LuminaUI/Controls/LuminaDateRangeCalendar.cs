using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaDateRangeCalendar : TemplatedControl
{
    private Grid? _daysGrid;

    private Button? _previousButton;

    private Button? _nextButton;

    private ControlTheme? _dayButtonTheme;

    private bool _isMarkingDates;

    public static readonly StyledProperty<DateTime> DisplayMonthProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, DateTime>(nameof(DisplayMonth), StartOfMonth(DateTime.Today));

    public static readonly StyledProperty<DateTime?> DisplayDateStartProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, DateTime?>(nameof(DisplayDateStart));

    public static readonly StyledProperty<DateTime?> DisplayDateEndProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, DateTime?>(nameof(DisplayDateEnd));

    public static readonly StyledProperty<DateTime?> RangeStartProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, DateTime?>(nameof(RangeStart));

    public static readonly StyledProperty<DateTime?> RangeEndProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, DateTime?>(nameof(RangeEnd));

    public static readonly StyledProperty<DateTime?> PreviewDateProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, DateTime?>(nameof(PreviewDate));

    public static readonly StyledProperty<bool> IsPreviewingRangeProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, bool>(nameof(IsPreviewingRange), defaultValue: false);

    public static readonly StyledProperty<bool> ShowPreviousButtonProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, bool>(nameof(ShowPreviousButton), defaultValue: true);

    public static readonly StyledProperty<bool> ShowNextButtonProperty = AvaloniaProperty.Register<LuminaDateRangeCalendar, bool>(nameof(ShowNextButton), defaultValue: true);

    public static readonly DirectProperty<LuminaDateRangeCalendar, string> TitleTextProperty = AvaloniaProperty.RegisterDirect<LuminaDateRangeCalendar, string>(nameof(TitleText), (LuminaDateRangeCalendar calendar) => calendar.TitleText);

    private string _titleText = string.Empty;

    public DateTime DisplayMonth
    {
        get
        {
            return GetValue(DisplayMonthProperty);
        }
        set
        {
            SetValue(DisplayMonthProperty, StartOfMonth(value));
        }
    }

    public DateTime? DisplayDateStart
    {
        get
        {
            return GetValue(DisplayDateStartProperty);
        }
        set
        {
            SetValue(DisplayDateStartProperty, value?.Date);
        }
    }

    public DateTime? DisplayDateEnd
    {
        get
        {
            return GetValue(DisplayDateEndProperty);
        }
        set
        {
            SetValue(DisplayDateEndProperty, value?.Date);
        }
    }

    public DateTime? RangeStart
    {
        get
        {
            return GetValue(RangeStartProperty);
        }
        set
        {
            SetValue(RangeStartProperty, value?.Date);
        }
    }

    public DateTime? RangeEnd
    {
        get
        {
            return GetValue(RangeEndProperty);
        }
        set
        {
            SetValue(RangeEndProperty, value?.Date);
        }
    }

    public DateTime? PreviewDate
    {
        get
        {
            return GetValue(PreviewDateProperty);
        }
        set
        {
            SetValue(PreviewDateProperty, value?.Date);
        }
    }

    public bool IsPreviewingRange
    {
        get => GetValue(IsPreviewingRangeProperty);
        set => SetValue(IsPreviewingRangeProperty, value);
    }

    public bool ShowPreviousButton
    {
        get => GetValue(ShowPreviousButtonProperty);
        set => SetValue(ShowPreviousButtonProperty, value);
    }

    public bool ShowNextButton
    {
        get => GetValue(ShowNextButtonProperty);
        set => SetValue(ShowNextButtonProperty, value);
    }

    public string TitleText
    {
        get
        {
            return _titleText;
        }
        private set
        {
            SetAndRaise(TitleTextProperty, ref _titleText, value);
        }
    }

    public event EventHandler<LuminaDateRangeCalendarDateEventArgs>? DateSelected;

    public event EventHandler<LuminaDateRangeCalendarDateEventArgs>? DatePreviewed;

    public event EventHandler<LuminaDateRangeCalendarMoveEventArgs>? DisplayMonthOffsetRequested;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DetachTemplateHandlers();
        _daysGrid = e.NameScope.FindRequired<Grid>("PART_DaysGrid");
        _previousButton = e.NameScope.FindRequired<Button>("PART_PreviousButton");
        _nextButton = e.NameScope.FindRequired<Button>("PART_NextButton");
        _dayButtonTheme = ResolveDayButtonTheme();
        AttachTemplateHandlers();
        UpdateCalendar();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Avalonia.Threading.Dispatcher.UIThread.Post(UpdateSelectionClasses, DispatcherPriority.Loaded);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayMonthProperty || change.Property == DisplayDateStartProperty || change.Property == DisplayDateEndProperty)
        {
            UpdateCalendar();
        }
        else if ((change.Property == RangeStartProperty || change.Property == RangeEndProperty || change.Property == PreviewDateProperty || change.Property == IsPreviewingRangeProperty) && !_isMarkingDates)
        {
            UpdateSelectionClasses();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Point point = e.GetPosition(this);
        if (point.X < 0.0 || point.Y < 0.0 || point.X > Bounds.Width || point.Y > Bounds.Height)
        {
            DatePreviewed?.Invoke(this, new LuminaDateRangeCalendarDateEventArgs(null));
        }
    }

    internal void RefreshSelection()
    {
        UpdateSelectionClasses();
    }

    internal void MarkDates(DateTime? rangeStart = null, DateTime? rangeEnd = null, DateTime? previewDate = null, bool isPreviewingRange = false)
    {
        try
        {
            _isMarkingDates = true;
            RangeStart = rangeStart;
            RangeEnd = rangeEnd;
            PreviewDate = previewDate;
            IsPreviewingRange = isPreviewingRange;
        }
        finally
        {
            _isMarkingDates = false;
        }
        UpdateSelectionClasses(rangeStart?.Date, rangeEnd?.Date, previewDate?.Date, isPreviewingRange);
    }

    private void AttachTemplateHandlers()
    {
        if (_previousButton != null)
        {
            _previousButton.Click += OnPreviousClick;
        }
        if (_nextButton != null)
        {
            _nextButton.Click += OnNextClick;
        }
    }

    private void DetachTemplateHandlers()
    {
        if (_previousButton != null)
        {
            _previousButton.Click -= OnPreviousClick;
        }
        if (_nextButton != null)
        {
            _nextButton.Click -= OnNextClick;
        }
        _daysGrid = null;
        _previousButton = null;
        _nextButton = null;
    }

    private void OnPreviousClick(object? sender, RoutedEventArgs e)
    {
        RequestMonthMove(-1);
        e.Handled = true;
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        RequestMonthMove(1);
        e.Handled = true;
    }

    private void RequestMonthMove(int offset)
    {
        if (DisplayMonthOffsetRequested != null)
        {
            DisplayMonthOffsetRequested(this, new LuminaDateRangeCalendarMoveEventArgs(offset));
        }
        else
        {
            DisplayMonth = DisplayMonth.AddMonths(offset);
        }
    }

    private void UpdateCalendar()
    {
        TitleText = DisplayMonth.ToString("Y", CultureInfo.CurrentCulture);
        if (_daysGrid != null)
        {
            _daysGrid.Children.Clear();
            _daysGrid.RowDefinitions.Clear();
            _daysGrid.ColumnDefinitions.Clear();
            for (int i = 0; i < 7; i++)
            {
                _daysGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }
            for (int j = 0; j < 7; j++)
            {
                _daysGrid.RowDefinitions.Add(new RowDefinition((j == 0) ? GridLength.Auto : GridLength.Star));
            }
            AddDayHeaders();
            AddDayButtons();
        }
    }

    private void AddDayHeaders()
    {
        if (_daysGrid != null)
        {
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            DayOfWeek firstDayOfWeek = formatInfo.FirstDayOfWeek;
            for (int column = 0; column < 7; column++)
            {
                DayOfWeek day = (DayOfWeek)((int)(firstDayOfWeek + column) % 7);
                TextBlock header = new TextBlock
                {
                    Text = formatInfo.ShortestDayNames[(int)day],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brush("LuminaTextMutedBrush", Brushes.Gray),
                    FontWeight = FontWeight.DemiBold,
                    Margin = LuminaPickerResources.Thickness("LuminaDateRangeCalendarHeaderMargin", new Thickness(0.0, 0.0, 0.0, 6.0))
                };
                LuminaPickerResources.BindResource(header, TextBlock.FontSizeProperty, "LuminaDateRangeCalendarHeaderFontSize");
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, column);
                _daysGrid.Children.Add(header);
            }
        }
    }

    private void AddDayButtons()
    {
        if (_daysGrid != null)
        {
            DateTime month = StartOfMonth(DisplayMonth);
            DayOfWeek firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int offset = (month.DayOfWeek - firstDayOfWeek + 7) % 7;
            DateTime firstVisibleDate = month.AddDays(-offset);
            for (int index = 0; index < 42; index++)
            {
                DateTime date = firstVisibleDate.AddDays(index);
                LuminaDateRangeCalendarDayButton button = CreateDayButton(date);
                Grid.SetRow(button, index / 7 + 1);
                Grid.SetColumn(button, index % 7);
                _daysGrid.Children.Add(button);
            }
        }
    }

    private void UpdateSelectionClasses()
    {
        UpdateSelectionClasses(RangeStart?.Date, RangeEnd?.Date, PreviewDate?.Date, IsPreviewingRange);
    }

    private void UpdateSelectionClasses(DateTime? anchorDate, DateTime? endDate, DateTime? previewDate, bool isPreviewingRange)
    {
        if (_daysGrid == null)
        {
            return;
        }
        foreach (LuminaDateRangeCalendarDayButton child in _daysGrid.Children.OfType<LuminaDateRangeCalendarDayButton>())
        {
            ResetRangeClasses(child);
            ApplyRangeClasses(child, child.Date.Date, anchorDate, endDate, previewDate, isPreviewingRange);
            child.InvalidateVisual();
        }
        _daysGrid.InvalidateVisual();
    }

    private LuminaDateRangeCalendarDayButton CreateDayButton(DateTime date)
    {
        bool isEnabled = IsDateEnabled(date);
        LuminaDateRangeCalendarDayButton button = new LuminaDateRangeCalendarDayButton
        {
            Content = date.Day.ToString(CultureInfo.CurrentCulture),
            Date = date.Date,
            Focusable = false,
            IsEnabled = isEnabled,
            Theme = _dayButtonTheme
        };
        button.Classes.Add("LuminaRangeDay");
        SetClass(button, "OutsideMonth", date.Month != DisplayMonth.Month || date.Year != DisplayMonth.Year);
        SetClass(button, "Today", date.Date == DateTime.Today);
        ApplyRangeClasses(button, date.Date, RangeStart, RangeEnd, PreviewDate, IsPreviewingRange);
        if (isEnabled)
        {
            button.DateSelected += OnDayButtonDateSelected;
            button.DatePreviewed += OnDayButtonDatePreviewed;
        }
        return button;
    }

    private void OnDayButtonDateSelected(object? sender, LuminaDateRangeCalendarDateEventArgs e)
    {
        DateSelected?.Invoke(this, e);
    }

    private void OnDayButtonDatePreviewed(object? sender, LuminaDateRangeCalendarDateEventArgs e)
    {
        DatePreviewed?.Invoke(this, e);
    }

    private static void ApplyRangeClasses(Control button, DateTime date, DateTime? anchorDate, DateTime? endDate, DateTime? previewDate, bool isPreviewingRange)
    {
        if (isPreviewingRange && anchorDate.HasValue && previewDate.HasValue)
        {
            ApplyPreviewRangeClasses(button, date, anchorDate.GetValueOrDefault(), previewDate.GetValueOrDefault());
            return;
        }
        if (!anchorDate.HasValue || !endDate.HasValue)
        {
            if (anchorDate.HasValue && date == anchorDate.Value)
            {
                SetRangeSingle(button, isEnabled: true);
                SetRangeSelected(button, isEnabled: true);
            }
            return;
        }
        DateTime start = (anchorDate.Value <= endDate.Value) ? anchorDate.Value : endDate.Value;
        DateTime end = (anchorDate.Value <= endDate.Value) ? endDate.Value : anchorDate.Value;
        bool isStart = date == start;
        bool isEnd = date == end;
        bool isBetween = date > start && date < end;
        SetRangeStart(button, isStart);
        SetRangeEnd(button, isEnd);
        SetRangeBetween(button, isBetween);
        SetRangeSingle(button, isStart && isEnd);
        SetRangeSelected(button, isStart || isEnd);
    }

    private static void ApplyPreviewRangeClasses(Control button, DateTime date, DateTime anchorDate, DateTime previewDate)
    {
        DateTime start = (anchorDate <= previewDate) ? anchorDate : previewDate;
        DateTime end = (anchorDate <= previewDate) ? previewDate : anchorDate;
        bool isStart = date == start;
        bool isEnd = date == end;
        bool isAnchor = date == anchorDate;
        bool isPreviewEndpoint = date == previewDate && previewDate != anchorDate;
        bool isBetween = date > start && date < end;
        SetRangeStart(button, isStart);
        SetRangeEnd(button, isEnd);
        SetRangeBetween(button, isBetween);
        SetRangeSingle(button, anchorDate == previewDate && date == anchorDate);
        SetRangeSelected(button, isAnchor);
        SetRangePreviewEndpoint(button, isPreviewEndpoint);
    }

    private static void ResetRangeClasses(Control control)
    {
        if (control is LuminaDateRangeCalendarDayButton dayButton)
        {
            dayButton.ResetRangeState();
        }
        control.Classes.Remove("RangeStart");
        control.Classes.Remove("RangeEnd");
        control.Classes.Remove("RangeBetween");
        control.Classes.Remove("RangeSingle");
        control.Classes.Remove("RangeSelected");
        control.Classes.Remove("RangePreviewEndpoint");
    }

    private static void SetRangeStart(Control control, bool isEnabled)
    {
        SetClass(control, "RangeStart", isEnabled);
        if (control is LuminaDateRangeCalendarDayButton dayButton)
        {
            dayButton.IsRangeStart = isEnabled;
        }
    }

    private static void SetRangeEnd(Control control, bool isEnabled)
    {
        SetClass(control, "RangeEnd", isEnabled);
        if (control is LuminaDateRangeCalendarDayButton dayButton)
        {
            dayButton.IsRangeEnd = isEnabled;
        }
    }

    private static void SetRangeBetween(Control control, bool isEnabled)
    {
        SetClass(control, "RangeBetween", isEnabled);
        if (control is LuminaDateRangeCalendarDayButton dayButton)
        {
            dayButton.IsRangeBetween = isEnabled;
        }
    }

    private static void SetRangeSelected(Control control, bool isEnabled)
    {
        SetClass(control, "RangeSelected", isEnabled);
        if (control is LuminaDateRangeCalendarDayButton dayButton)
        {
            dayButton.IsRangeSelected = isEnabled;
        }
    }

    private static void SetRangePreviewEndpoint(Control control, bool isEnabled)
    {
        SetClass(control, "RangePreviewEndpoint", isEnabled);
        if (control is LuminaDateRangeCalendarDayButton dayButton)
        {
            dayButton.IsRangePreviewEndpoint = isEnabled;
        }
    }

    private static void SetRangeSingle(Control control, bool isEnabled)
    {
        SetClass(control, "RangeSingle", isEnabled);
        if (control is LuminaDateRangeCalendarDayButton dayButton)
        {
            dayButton.IsRangeSingle = isEnabled;
        }
    }

    private bool IsDateEnabled(DateTime date)
    {
        if (DisplayDateStart.HasValue && date < DisplayDateStart.Value.Date)
        {
            return false;
        }
        if (DisplayDateEnd.HasValue && date > DisplayDateEnd.Value.Date)
        {
            return false;
        }
        return true;
    }

    private ControlTheme? ResolveDayButtonTheme()
    {
        Application? current = Application.Current;
        object? resource;
        return (current != null && current.TryFindResource("LuminaDateRangeCalendarDayButtonTheme", out resource)) ? (resource as ControlTheme) : null;
    }

    private static void SetClass(Control control, string className, bool isEnabled)
    {
        if (isEnabled)
        {
            if (!control.Classes.Contains(className))
            {
                control.Classes.Add(className);
            }
        }
        else
        {
            control.Classes.Remove(className);
        }
    }

    private static IBrush Brush(string key, IBrush fallback)
    {
        Application? current = Application.Current;
        object? resource;
        return (current != null && current.TryFindResource(key, out resource) && resource is IBrush brush) ? brush : fallback;
    }

    private static DateTime StartOfMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }
}
