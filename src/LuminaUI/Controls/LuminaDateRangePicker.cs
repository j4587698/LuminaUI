using System;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Extensions;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Controls;

public class LuminaDateRangePicker : TemplatedControl
{
	private enum DateRangePart
	{
		Start,
		End
	}

	private TextBox? _startTextBox;

	private TextBox? _endTextBox;

	private Button? _button;

	private Popup? _popup;

	private LuminaDateRangeCalendar? _startCalendar;

	private LuminaDateRangeCalendar? _endCalendar;

	private bool _isSyncing;

	private bool _isSelectingDesktopRange;

	private bool _isRangeComplete;

	private bool _isDropDownOpen;

	private bool _commitTextOnDropDownClose;

	private DateTime? _desktopPreviewDate;

	private string _selectedRangeText = string.Empty;

	public static readonly StyledProperty<DateTime?> StartDateProperty = AvaloniaProperty.Register<LuminaDateRangePicker, DateTime?>("StartDate", null, inherits: false, BindingMode.TwoWay);

	public static readonly StyledProperty<DateTime?> EndDateProperty = AvaloniaProperty.Register<LuminaDateRangePicker, DateTime?>("EndDate", null, inherits: false, BindingMode.TwoWay);

	public static readonly StyledProperty<DateTime?> DisplayDateStartProperty = AvaloniaProperty.Register<LuminaDateRangePicker, DateTime?>("DisplayDateStart");

	public static readonly StyledProperty<DateTime?> DisplayDateEndProperty = AvaloniaProperty.Register<LuminaDateRangePicker, DateTime?>("DisplayDateEnd");

	public static readonly StyledProperty<string> StartPlaceholderProperty = AvaloniaProperty.Register<LuminaDateRangePicker, string>("StartPlaceholder", "Start date");

	public static readonly StyledProperty<string> EndPlaceholderProperty = AvaloniaProperty.Register<LuminaDateRangePicker, string>("EndPlaceholder", "End date");

	public static readonly StyledProperty<string> SeparatorTextProperty = AvaloniaProperty.Register<LuminaDateRangePicker, string>("SeparatorText", "-");

	public static readonly StyledProperty<string> DateFormatProperty = AvaloniaProperty.Register<LuminaDateRangePicker, string>("DateFormat", "yyyy-MM-dd");

	public static readonly StyledProperty<LuminaPopupType> PopupTypeProperty = AvaloniaProperty.Register<LuminaDateRangePicker, LuminaPopupType>("PopupType", LuminaPopupType.Auto);

	public static readonly StyledProperty<bool> EnableMonthSyncProperty = AvaloniaProperty.Register<LuminaDateRangePicker, bool>("EnableMonthSync", defaultValue: true);

	public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty = AvaloniaProperty.Register<LuminaDateRangePicker, ICommand?>("SelectionChangedCommand");

	public static readonly DirectProperty<LuminaDateRangePicker, bool> IsRangeCompleteProperty = AvaloniaProperty.RegisterDirect<LuminaDateRangePicker, bool>("IsRangeComplete", (LuminaDateRangePicker picker) => picker.IsRangeComplete, null, unsetValue: false);

	public static readonly DirectProperty<LuminaDateRangePicker, bool> IsDropDownOpenProperty = AvaloniaProperty.RegisterDirect<LuminaDateRangePicker, bool>("IsDropDownOpen", (LuminaDateRangePicker picker) => picker.IsDropDownOpen, delegate(LuminaDateRangePicker picker, bool value)
	{
		picker.IsDropDownOpen = value;
	}, unsetValue: false, BindingMode.TwoWay);

	public static readonly DirectProperty<LuminaDateRangePicker, string> SelectedRangeTextProperty = AvaloniaProperty.RegisterDirect<LuminaDateRangePicker, string>("SelectedRangeText", (LuminaDateRangePicker picker) => picker.SelectedRangeText);

	public DateTime? StartDate
	{
		get
		{
			return GetValue(StartDateProperty);
		}
		set
		{
			SetValue(StartDateProperty, value);
		}
	}

	public DateTime? EndDate
	{
		get
		{
			return GetValue(EndDateProperty);
		}
		set
		{
			SetValue(EndDateProperty, value);
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
			SetValue(DisplayDateStartProperty, value);
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
			SetValue(DisplayDateEndProperty, value);
		}
	}

	public string StartPlaceholder
	{
		get
		{
			return GetValue(StartPlaceholderProperty);
		}
		set
		{
			SetValue(StartPlaceholderProperty, value);
		}
	}

	public string EndPlaceholder
	{
		get
		{
			return GetValue(EndPlaceholderProperty);
		}
		set
		{
			SetValue(EndPlaceholderProperty, value);
		}
	}

	public string SeparatorText
	{
		get
		{
			return GetValue(SeparatorTextProperty);
		}
		set
		{
			SetValue(SeparatorTextProperty, value);
		}
	}

	public string DateFormat
	{
		get
		{
			return GetValue(DateFormatProperty);
		}
		set
		{
			SetValue(DateFormatProperty, value);
		}
	}

	public LuminaPopupType PopupType
	{
		get
		{
			return GetValue(PopupTypeProperty);
		}
		set
		{
			SetValue(PopupTypeProperty, value);
		}
	}

	public bool EnableMonthSync
	{
		get
		{
			return GetValue(EnableMonthSyncProperty);
		}
		set
		{
			SetValue(EnableMonthSyncProperty, value);
		}
	}

	public ICommand? SelectionChangedCommand
	{
		get
		{
			return GetValue(SelectionChangedCommandProperty);
		}
		set
		{
			SetValue(SelectionChangedCommandProperty, value);
		}
	}

	public bool IsRangeComplete
	{
		get
		{
			return _isRangeComplete;
		}
		private set
		{
			SetAndRaise(IsRangeCompleteProperty, ref _isRangeComplete, value);
		}
	}

	public bool IsDropDownOpen
	{
		get
		{
			return _isDropDownOpen;
		}
		set
		{
			SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value);
		}
	}

	public string SelectedRangeText
	{
		get
		{
			return _selectedRangeText;
		}
		private set
		{
			SetAndRaise(SelectedRangeTextProperty, ref _selectedRangeText, value);
		}
	}

	public LuminaDateRangePicker()
	{
		AddHandler(InputElement.PointerPressedEvent, OnPickerPointerPressed, RoutingStrategies.Tunnel);
		AddHandler(InputElement.KeyDownEvent, OnPickerKeyDown, RoutingStrategies.Tunnel);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		DetachTemplateHandlers();
		_startTextBox = e.NameScope.FindRequired<TextBox>("PART_StartTextBox");
		_endTextBox = e.NameScope.FindRequired<TextBox>("PART_EndTextBox");
		_button = e.NameScope.FindRequired<Button>("PART_Button");
		_popup = e.NameScope.FindRequired<Popup>("PART_Popup");
		_startCalendar = e.NameScope.FindRequired<LuminaDateRangeCalendar>("PART_StartCalendar");
		_endCalendar = e.NameScope.FindRequired<LuminaDateRangeCalendar>("PART_EndCalendar");
		AttachTemplateHandlers();
		SetCalendarDisplayMonths(GetInitialDisplayMonth());
		SyncTemplate();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		DetachTemplateHandlers();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (_isSyncing && (change.Property == StartDateProperty || change.Property == EndDateProperty))
		{
			return;
		}
		if (change.Property == StartDateProperty || change.Property == EndDateProperty || change.Property == DisplayDateStartProperty || change.Property == DisplayDateEndProperty || change.Property == DateFormatProperty || change.Property == SeparatorTextProperty || change.Property == PopupTypeProperty || change.Property == EnableMonthSyncProperty)
		{
			NormalizeRange();
			SyncTemplate();
			UpdateRangeState(change.Property == StartDateProperty || change.Property == EndDateProperty);
		}
		else if (change.Property == IsDropDownOpenProperty)
		{
			_desktopPreviewDate = null;
			_isSelectingDesktopRange = false;
			if (IsDropDownOpen)
			{
				SetCalendarDisplayMonths(GetInitialDisplayMonth());
				SyncCalendars();
				QueueCalendarSync();
			}
			else
			{
				CommitDeferredTextInput();
				SyncCalendars();
			}
		}
	}

	private void AttachTemplateHandlers()
	{
		if (_startTextBox != null)
		{
			_startTextBox.LostFocus += OnTextBoxLostFocus;
			_startTextBox.KeyDown += OnTextBoxKeyDown;
		}
		if (_endTextBox != null)
		{
			_endTextBox.LostFocus += OnTextBoxLostFocus;
			_endTextBox.KeyDown += OnTextBoxKeyDown;
		}
		if (_button != null)
		{
			_button.Click += OnButtonClick;
		}
		if (_startCalendar != null)
		{
			_startCalendar.DateSelected += OnDesktopCalendarDateSelected;
			_startCalendar.DatePreviewed += OnDesktopCalendarDatePreviewed;
			_startCalendar.DisplayMonthOffsetRequested += OnDesktopCalendarMonthMoveRequested;
		}
		if (_endCalendar != null)
		{
			_endCalendar.DateSelected += OnDesktopCalendarDateSelected;
			_endCalendar.DatePreviewed += OnDesktopCalendarDatePreviewed;
			_endCalendar.DisplayMonthOffsetRequested += OnDesktopCalendarMonthMoveRequested;
		}
	}

	private void DetachTemplateHandlers()
	{
		if (_startTextBox != null)
		{
			_startTextBox.LostFocus -= OnTextBoxLostFocus;
			_startTextBox.KeyDown -= OnTextBoxKeyDown;
		}
		if (_endTextBox != null)
		{
			_endTextBox.LostFocus -= OnTextBoxLostFocus;
			_endTextBox.KeyDown -= OnTextBoxKeyDown;
		}
		if (_button != null)
		{
			_button.Click -= OnButtonClick;
		}
		if (_startCalendar != null)
		{
			_startCalendar.DateSelected -= OnDesktopCalendarDateSelected;
			_startCalendar.DatePreviewed -= OnDesktopCalendarDatePreviewed;
			_startCalendar.DisplayMonthOffsetRequested -= OnDesktopCalendarMonthMoveRequested;
		}
		if (_endCalendar != null)
		{
			_endCalendar.DateSelected -= OnDesktopCalendarDateSelected;
			_endCalendar.DatePreviewed -= OnDesktopCalendarDatePreviewed;
			_endCalendar.DisplayMonthOffsetRequested -= OnDesktopCalendarMonthMoveRequested;
		}
		_startTextBox = null;
		_endTextBox = null;
		_button = null;
		_popup = null;
		_startCalendar = null;
		_endCalendar = null;
	}

	private void OnButtonClick(object? sender, RoutedEventArgs e)
	{
		OpenPicker();
		e.Handled = true;
	}

	private void OnPickerPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			return;
		}
		if (ShouldUseSheet())
		{
			if (ShowRangeSheet())
			{
				e.Handled = true;
			}
		}
		else
		{
			if (IsDropDownOpen)
			{
				return;
			}
			Avalonia.Threading.Dispatcher.UIThread.Post(delegate
			{
				if (!ShouldUseSheet() && !IsDropDownOpen)
				{
					PrepareDesktopPickerOpen();
					IsDropDownOpen = true;
				}
			});
		}
	}

	private void OnPickerKeyDown(object? sender, KeyEventArgs e)
	{
		Key key = e.Key;
		bool flag = ((key == Key.Down || key == Key.F4) ? true : false);
		bool flag2 = flag;
		bool flag3 = flag2;
		if (!flag3)
		{
			bool flag4 = !(e.Source is TextBox);
			bool flag5 = flag4;
			if (flag5)
			{
				Key key2 = e.Key;
				bool flag6 = ((key2 == Key.Return || key2 == Key.Space) ? true : false);
				flag5 = flag6;
			}
			flag3 = flag5;
		}
		if (!flag3)
		{
			return;
		}
		if (ShouldUseSheet())
		{
			if (ShowRangeSheet())
			{
				e.Handled = true;
			}
		}
		else
		{
			PrepareDesktopPickerOpen();
			IsDropDownOpen = true;
			e.Handled = true;
		}
	}

	private void OpenPicker()
	{
		if (ShouldUseSheet())
		{
			ShowRangeSheet();
			return;
		}
		if (!IsDropDownOpen)
		{
			PrepareDesktopPickerOpen();
		}
		IsDropDownOpen = true;
	}

	private bool ShouldUseSheet()
	{
		return LuminaSheetPlacement.ShouldUseSheet(PopupType);
	}

	private bool ShowRangeSheet()
	{
		IsDropDownOpen = false;
		DateTime minDate = DisplayDateStart?.Date ?? DateTime.Today.AddYears(-100);
		DateTime maxDate = DisplayDateEnd?.Date ?? DateTime.Today.AddYears(100);
		DateTime pendingStart = ClampDate(StartDate?.Date ?? DateTime.Today, minDate, maxDate);
		DateTime pendingEnd = ClampDate(EndDate?.Date ?? pendingStart, minDate, maxDate);
		if (pendingStart > pendingEnd)
		{
			pendingEnd = pendingStart;
		}
		DateRangePart activePart = DateRangePart.Start;
		LuminaDateWheelPicker? activePicker = null;
		ContentControl pickerHost = new ContentControl
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch
		};
		Button startButton = CreateSheetDateButton(StartPlaceholder, pendingStart, isSelected: true);
		Button endButton = CreateSheetDateButton(EndPlaceholder, pendingEnd, isSelected: false);
		startButton.Click += delegate
		{
			Activate(DateRangePart.Start);
		};
		endButton.Click += delegate
		{
			Activate(DateRangePart.End);
		};
		Activate(DateRangePart.Start);
		Grid selectorGrid = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,*"),
			ColumnSpacing = 10.0,
			Children = 
			{
				(Control)startButton,
				(Control)endButton
			}
		};
		Grid.SetColumn(endButton, 1);
		StackPanel body = new StackPanel
		{
			Spacing = 14.0,
			Children = 
			{
				(Control)selectorGrid,
				(Control)pickerHost
			}
		};
		StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectDate"), body, CreateSheetActions(delegate
		{
			StartDate = null;
			EndDate = null;
			LuminaBottomSheetService.Instance.Close(this);
		}, delegate
		{
			SaveActivePicker();
			StartDate = pendingStart;
			EndDate = pendingEnd;
			LuminaBottomSheetService.Instance.Close(this);
		}));
		return LuminaBottomSheetService.Instance.TryShow(this, content);
		void Activate(DateRangePart part)
		{
			SaveActivePicker();
			activePart = part;
			activePicker = new LuminaDateWheelPicker((part == DateRangePart.Start) ? pendingStart : pendingEnd, minDate, maxDate);
			pickerHost.Content = activePicker;
			UpdateSheetButtons();
		}
		void SaveActivePicker()
		{
			if (activePicker != null)
			{
				if (activePart == DateRangePart.Start)
				{
					pendingStart = activePicker.SelectedDate;
					if (pendingStart > pendingEnd)
					{
						pendingEnd = pendingStart;
					}
				}
				else
				{
					pendingEnd = activePicker.SelectedDate;
					if (pendingEnd < pendingStart)
					{
						pendingStart = pendingEnd;
					}
				}
			}
		}
		void UpdateSheetButtons()
		{
			startButton.Content = CreateSheetDateButtonContent(StartPlaceholder, pendingStart);
			endButton.Content = CreateSheetDateButtonContent(EndPlaceholder, pendingEnd);
			SetSheetButtonState(startButton, activePart == DateRangePart.Start);
			SetSheetButtonState(endButton, activePart == DateRangePart.End);
		}
	}

	private Button CreateSheetDateButton(string title, DateTime date, bool isSelected)
	{
		Button button = new Button
		{
			Content = CreateSheetDateButtonContent(title, date),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Padding = new Thickness(12.0, 10.0)
		};
		SetSheetButtonState(button, isSelected);
		return button;
	}

	private Control CreateSheetDateButtonContent(string title, DateTime date)
	{
		return new StackPanel
		{
			Spacing = 4.0,
			Children = 
			{
				(Control)new TextBlock
				{
					Text = title,
					FontSize = 12.0,
					Foreground = LuminaPickerResources.Brush("LuminaTextMutedBrush", Brushes.Gray)
				},
				(Control)new TextBlock
				{
					Text = FormatDate(date),
					FontWeight = FontWeight.DemiBold,
					Foreground = LuminaPickerResources.Brush("LuminaTextForegroundBrush", Brushes.White)
				}
			}
		};
	}

	private static void SetSheetButtonState(Button button, bool isSelected)
	{
		button.Classes.Remove("Primary");
		button.Classes.Remove("Outline");
		button.Classes.Add(isSelected ? "Primary" : "Outline");
	}

	private Control CreateSheetActions(Action onClear, Action onApply)
	{
		Button clearButton = new Button
		{
			Content = LuminaLocalization.Get("Lumina.Common.Clear")
		};
		clearButton.Classes.Add("Ghost");
		clearButton.Click += delegate
		{
			onClear();
		};
		Button cancelButton = new Button
		{
			Content = LuminaLocalization.Get("Lumina.Common.Cancel")
		};
		cancelButton.Classes.Add("Outline");
		cancelButton.Click += delegate
		{
			LuminaBottomSheetService.Instance.Close(this);
		};
		Button applyButton = new Button
		{
			Content = LuminaLocalization.Get("Lumina.Common.Done")
		};
		applyButton.Classes.Add("Primary");
		applyButton.Click += delegate
		{
			onApply();
		};
		Grid.SetColumn(cancelButton, 1);
		Grid.SetColumn(applyButton, 2);
		return new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
			ColumnSpacing = 8.0,
			Children = 
			{
				(Control)clearButton,
				(Control)cancelButton,
				(Control)applyButton
			}
		};
	}

	private static StackPanel CreateSheetLayout(string title, Control body, Control footer)
	{
		return new StackPanel
		{
			Spacing = 16.0,
			Children = 
			{
				(Control)new TextBlock
				{
					Text = title,
					FontSize = 18.0,
					FontWeight = FontWeight.DemiBold
				},
				body,
				footer
			}
		};
	}

	private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
	{
		if (IsDropDownOpen)
		{
			_commitTextOnDropDownClose = true;
		}
		else
		{
			CommitTextBox(sender as TextBox);
		}
	}

	private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			CommitTextBox(sender as TextBox);
			e.Handled = true;
		}
	}

	private void CommitTextBox(TextBox? textBox)
	{
		if (textBox == null || _isSyncing)
		{
			return;
		}
		string? text = textBox.Text?.Trim();
		DateTime date;
		if (string.IsNullOrWhiteSpace(text))
		{
			if (textBox == _startTextBox)
			{
				StartDate = null;
			}
			else
			{
				EndDate = null;
			}
		}
		else if (!TryParseDate(text, out date))
		{
			SyncTextBoxes();
		}
		else if (textBox == _startTextBox)
		{
			StartDate = date;
		}
		else
		{
			EndDate = date;
		}
	}

	private void OnDesktopCalendarDateSelected(object? sender, LuminaDateRangeCalendarDateEventArgs e)
	{
		if (_isSyncing || !e.Date.HasValue)
		{
			return;
		}
		DateTime selectedDate = ClampDate(e.Date.Value);
		if (!_isSelectingDesktopRange || !StartDate.HasValue || EndDate.HasValue)
		{
			_isSelectingDesktopRange = true;
			_desktopPreviewDate = null;
			SetRangeDates(selectedDate, null);
			return;
		}
		DateTime start = StartDate.Value.Date;
		DateTime rangeStart;
		DateTime rangeEnd;
		if (selectedDate < start)
		{
			rangeStart = selectedDate;
			rangeEnd = start;
		}
		else
		{
			rangeStart = start;
			rangeEnd = selectedDate;
		}
		_isSelectingDesktopRange = false;
		_desktopPreviewDate = null;
		SetRangeDates(rangeStart, rangeEnd);
		IsDropDownOpen = false;
		SyncCalendars();
	}

	private void OnDesktopCalendarDatePreviewed(object? sender, LuminaDateRangeCalendarDateEventArgs e)
	{
		if (_isSelectingDesktopRange && StartDate.HasValue && !EndDate.HasValue)
		{
			_desktopPreviewDate = ((!e.Date.HasValue) ? ((DateTime?)null) : new DateTime?(ClampDate(e.Date.Value)));
			SyncCalendars();
		}
	}

	private void OnDesktopCalendarMonthMoveRequested(object? sender, LuminaDateRangeCalendarMoveEventArgs e)
	{
		if (!EnableMonthSync && sender is LuminaDateRangeCalendar calendar)
		{
			calendar.DisplayMonth = ClampDisplayMonth(calendar.DisplayMonth.AddMonths(e.MonthOffset), pairWithNextMonth: false);
		}
		else
		{
			SetCalendarDisplayMonths((_startCalendar?.DisplayMonth ?? GetInitialDisplayMonth()).AddMonths(e.MonthOffset));
		}
	}

	private void NormalizeRange()
	{
		if (StartDate.HasValue)
		{
			StartDate = ClampDate(StartDate.Value);
		}
		if (EndDate.HasValue)
		{
			EndDate = ClampDate(EndDate.Value);
		}
		if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
		{
			EndDate = StartDate;
		}
	}

	private DateTime ClampDate(DateTime date)
	{
		return ClampDate(date, DisplayDateStart, DisplayDateEnd);
	}

	private static DateTime ClampDate(DateTime date, DateTime? minDate, DateTime? maxDate)
	{
		DateTime result = date.Date;
		if (minDate.HasValue && result < minDate.Value.Date)
		{
			result = minDate.Value.Date;
		}
		if (maxDate.HasValue && result > maxDate.Value.Date)
		{
			result = maxDate.Value.Date;
		}
		return result;
	}

	private void SyncTemplate()
	{
		_isSyncing = true;
		SyncTextBoxes();
		SyncCalendars();
		_isSyncing = false;
	}

	private void SetRangeDates(DateTime? startDate, DateTime? endDate)
	{
		_isSyncing = true;
		SetCurrentValue(StartDateProperty, startDate?.Date);
		SetCurrentValue(EndDateProperty, endDate?.Date);
		_isSyncing = false;
		NormalizeRange();
		SyncTemplate();
		UpdateRangeState(executeCommand: true);
	}

	private void SyncTextBoxes()
	{
		if (_startTextBox != null)
		{
			_startTextBox.Text = ((!StartDate.HasValue) ? string.Empty : FormatDate(StartDate.Value));
		}
		if (_endTextBox != null)
		{
			_endTextBox.Text = ((!EndDate.HasValue) ? string.Empty : FormatDate(EndDate.Value));
		}
	}

	private void SyncCalendars()
	{
		SyncCalendar(_startCalendar);
		SyncCalendar(_endCalendar);
	}

	private void SyncCalendar(LuminaDateRangeCalendar? calendar)
	{
		if (calendar != null)
		{
			calendar.DisplayDateStart = DisplayDateStart;
			calendar.DisplayDateEnd = DisplayDateEnd;
			calendar.MarkDates(StartDate, EndDate, _desktopPreviewDate, _isSelectingDesktopRange && StartDate.HasValue && _desktopPreviewDate.HasValue);
		}
	}

	private void PrepareDesktopPickerOpen()
	{
		_desktopPreviewDate = null;
		_isSelectingDesktopRange = false;
		SetCalendarDisplayMonths(GetInitialDisplayMonth());
		SyncCalendars();
		QueueCalendarSync();
	}

	private void SetCalendarDisplayMonths(DateTime month)
	{
		DateTime startMonth = ClampDisplayMonth(month, pairWithNextMonth: true);
		if (_startCalendar != null)
		{
			_startCalendar.DisplayMonth = startMonth;
		}
		if (_endCalendar != null)
		{
			_endCalendar.DisplayMonth = startMonth.AddMonths(1);
		}
	}

	private DateTime GetInitialDisplayMonth()
	{
		DateTime anchor = StartDate ?? EndDate?.AddMonths(-1) ?? DateTime.Today;
		return ClampDisplayMonth(StartOfMonth(ClampDate(anchor)), pairWithNextMonth: true);
	}

	private DateTime ClampDisplayMonth(DateTime month, bool pairWithNextMonth)
	{
		DateTime result = StartOfMonth(month);
		if (DisplayDateStart.HasValue)
		{
			DateTime minMonth = StartOfMonth(DisplayDateStart.Value);
			if (result < minMonth)
			{
				result = minMonth;
			}
		}
		if (DisplayDateEnd.HasValue)
		{
			DateTime maxMonth = StartOfMonth(DisplayDateEnd.Value);
			DateTime maxStartMonth = (pairWithNextMonth ? maxMonth.AddMonths(-1) : maxMonth);
			if (result > maxStartMonth)
			{
				result = maxStartMonth;
			}
		}
		if (DisplayDateStart.HasValue)
		{
			DateTime minMonth2 = StartOfMonth(DisplayDateStart.Value);
			if (result < minMonth2)
			{
				result = minMonth2;
			}
		}
		return result;
	}

	private void QueueCalendarSync()
	{
		Avalonia.Threading.Dispatcher.UIThread.Post(delegate
		{
			SyncCalendars();
			_startCalendar?.RefreshSelection();
			_endCalendar?.RefreshSelection();
		}, DispatcherPriority.Loaded);
	}

	private void CommitDeferredTextInput()
	{
		if (_commitTextOnDropDownClose)
		{
			_commitTextOnDropDownClose = false;
			CommitTextBox(_startTextBox);
			CommitTextBox(_endTextBox);
		}
	}

	private static DateTime StartOfMonth(DateTime date)
	{
		return new DateTime(date.Year, date.Month, 1);
	}

	private bool TryParseDate(string text, out DateTime date)
	{
		if (DateTime.TryParseExact(text, DateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
		{
			date = ClampDate(date);
			return true;
		}
		if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
		{
			date = ClampDate(date);
			return true;
		}
		date = default(DateTime);
		return false;
	}

	private string FormatDate(DateTime date)
	{
		return date.ToString(DateFormat, CultureInfo.CurrentCulture);
	}

	private void UpdateRangeState(bool executeCommand)
	{
		IsRangeComplete = StartDate.HasValue && EndDate.HasValue;
		SelectedRangeText = ((StartDate is DateTime startDate && EndDate is DateTime endDate) ? $"{FormatDate(startDate)} {SeparatorText} {FormatDate(endDate)}" : string.Empty);
		if (executeCommand)
		{
			ICommand? selectionChangedCommand = SelectionChangedCommand;
			if (selectionChangedCommand != null && selectionChangedCommand.CanExecute(new LuminaDateRange(StartDate, EndDate)))
			{
				selectionChangedCommand.Execute(new LuminaDateRange(StartDate, EndDate));
			}
		}
	}
}
