using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Controls;

public class LuminaOptions : AvaloniaObject
{
	private sealed record ActionSheetEntry(string Text, object? Icon, bool IsEnabled, bool IsChecked, Action? Invoke, bool IsSeparator = false)
	{
		public static ActionSheetEntry Separator()
		{
			return new ActionSheetEntry(string.Empty, null, IsEnabled: false, IsChecked: false, null, IsSeparator: true);
		}
	}

	public static readonly AttachedProperty<LuminaPopupType> PopupTypeProperty;

	public static readonly AttachedProperty<string?> DateFormatProperty;

	public static readonly AttachedProperty<string?> TimeFormatProperty;

	private static readonly AttachedProperty<bool> IsSheetBehaviorAttachedProperty;

	private static readonly AttachedProperty<bool> IsCommandBarSheetOpeningProperty;

	static LuminaOptions()
	{
		PopupTypeProperty = AvaloniaProperty.RegisterAttached<LuminaOptions, Control, LuminaPopupType>("PopupType", LuminaPopupType.Auto);
		DateFormatProperty = AvaloniaProperty.RegisterAttached<LuminaOptions, Control, string?>("DateFormat");
		TimeFormatProperty = AvaloniaProperty.RegisterAttached<LuminaOptions, Control, string?>("TimeFormat");
		IsSheetBehaviorAttachedProperty = AvaloniaProperty.RegisterAttached<LuminaOptions, Control, bool>("IsSheetBehaviorAttached", defaultValue: false);
		IsCommandBarSheetOpeningProperty = AvaloniaProperty.RegisterAttached<LuminaOptions, CommandBar, bool>("IsCommandBarSheetOpening", defaultValue: false);
		PopupTypeProperty.Changed.AddClassHandler(delegate(Control control, AvaloniaPropertyChangedEventArgs change)
		{
			SyncSheetBehavior(control, change.GetNewValue<LuminaPopupType>());
		});
		RegisterSheetTarget<ComboBox>();
		RegisterSheetTarget<DatePicker>();
		RegisterSheetTarget<TimePicker>();
		RegisterSheetTarget<CalendarDatePicker>();
		RegisterSheetTarget<LuminaMultiSelect>();
		RegisterSheetTarget<DropDownButton>();
		RegisterSheetTarget<SplitButton>();
		RegisterSheetTarget<CommandBar>();
		DateFormatProperty.Changed.AddClassHandler(delegate(Control control, AvaloniaPropertyChangedEventArgs change)
		{
			ApplyDateFormat(control, change.GetNewValue<string>());
		});
		TimeFormatProperty.Changed.AddClassHandler(delegate(Control control, AvaloniaPropertyChangedEventArgs change)
		{
			ApplyTimeFormat(control, change.GetNewValue<string>());
		});
		CommandBar.IsOpenProperty.Changed.AddClassHandler<CommandBar>(OnCommandBarIsOpenChanged);
	}

	private static void RegisterSheetTarget<TControl>() where TControl : Control
	{
		Control.LoadedEvent.AddClassHandler<TControl>(OnSheetTargetLoaded);
	}

	private static void OnSheetTargetLoaded(Control control, RoutedEventArgs e)
	{
		SyncSheetBehavior(control, GetPopupType(control));
	}

	public static LuminaPopupType GetPopupType(Control element)
	{
		return element.GetValue(PopupTypeProperty);
	}

	public static void SetPopupType(Control element, LuminaPopupType value)
	{
		element.SetValue(PopupTypeProperty, value);
	}

	public static string? GetDateFormat(Control element)
	{
		return element.GetValue(DateFormatProperty);
	}

	public static void SetDateFormat(Control element, string? value)
	{
		element.SetValue(DateFormatProperty, value);
	}

	public static string? GetTimeFormat(Control element)
	{
		return element.GetValue(TimeFormatProperty);
	}

	public static void SetTimeFormat(Control element, string? value)
	{
		element.SetValue(TimeFormatProperty, value);
	}

	private static void ApplyDateFormat(Control control, string? format)
	{
		if (!(control is LuminaDateRangePicker dateRangePicker))
		{
			if (!(control is CalendarDatePicker calendarDatePicker))
			{
				if (control is DatePicker datePicker)
				{
					LuminaPickerFormat.ApplyDateFormat(datePicker, format);
				}
			}
			else
			{
				LuminaPickerFormat.ApplyDateFormat(calendarDatePicker, format);
			}
		}
		else
		{
			dateRangePicker.DateFormat = (string.IsNullOrWhiteSpace(format) ? "yyyy-MM-dd" : format);
		}
	}

	private static void ApplyTimeFormat(Control control, string? format)
	{
		if (control is TimePicker timePicker)
		{
			LuminaPickerFormat.ApplyTimeFormat(timePicker, format);
		}
	}

	private static bool IsSheetTarget(Control control)
	{
		if (control is ComboBox || control is DatePicker || control is TimePicker || control is CalendarDatePicker || control is LuminaMultiSelect || control is DropDownButton || control is SplitButton || control is CommandBar)
		{
			return true;
		}
		return false;
	}

	private static bool ShouldAttachSheetBehavior(Control control, LuminaPopupType popupType)
	{
		if (1 == 0)
		{
		}
		bool result = (popupType == LuminaPopupType.Auto || popupType == LuminaPopupType.Sheet) && IsSheetTarget(control);
		if (1 == 0)
		{
		}
		return result;
	}

	private static void SyncSheetBehavior(Control control, LuminaPopupType popupType)
	{
		if (ShouldAttachSheetBehavior(control, popupType))
		{
			AttachSheetBehavior(control);
		}
		else
		{
			DetachSheetBehavior(control);
		}
	}

	private static void AttachSheetBehavior(Control control)
	{
		if (!control.GetValue(IsSheetBehaviorAttachedProperty))
		{
			control.SetValue(IsSheetBehaviorAttachedProperty, value: true);
			control.AddHandler(InputElement.PointerPressedEvent, OnSheetTargetPointerPressed, RoutingStrategies.Tunnel);
			control.AddHandler(InputElement.PointerReleasedEvent, OnSheetTargetPointerReleased, RoutingStrategies.Tunnel);
			control.AddHandler(InputElement.KeyDownEvent, OnSheetTargetKeyDown, RoutingStrategies.Tunnel);
			if (control is DropDownButton dropDownButton)
			{
				dropDownButton.Click += OnDropDownButtonClick;
			}
			control.DetachedFromVisualTree += OnSheetTargetDetachedFromVisualTree;
		}
	}

	private static void DetachSheetBehavior(Control control)
	{
		if (control.GetValue(IsSheetBehaviorAttachedProperty))
		{
			control.SetValue(IsSheetBehaviorAttachedProperty, value: false);
			control.RemoveHandler(InputElement.PointerPressedEvent, OnSheetTargetPointerPressed);
			control.RemoveHandler(InputElement.PointerReleasedEvent, OnSheetTargetPointerReleased);
			control.RemoveHandler(InputElement.KeyDownEvent, OnSheetTargetKeyDown);
			if (control is DropDownButton dropDownButton)
			{
				dropDownButton.Click -= OnDropDownButtonClick;
			}
			control.DetachedFromVisualTree -= OnSheetTargetDetachedFromVisualTree;
		}
	}

	private static void OnSheetTargetDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
	{
		if (sender is Control control)
		{
			DetachSheetBehavior(control);
		}
	}

	private static void OnSheetTargetPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (sender is Control control && ShouldAttachSheetBehavior(control, GetPopupType(control)) && e.GetCurrentPoint(control).Properties.IsLeftButtonPressed && IsPointerSheetInvocation(control, e.Source) && TryShowSheet(control))
		{
			e.Handled = true;
		}
	}

	private static void OnSheetTargetPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (sender is Control control && ShouldAttachSheetBehavior(control, GetPopupType(control)) && ShouldUseSheet(control) && IsPointerSheetInvocation(control, e.Source))
		{
			e.Handled = true;
		}
	}

	private static void OnSheetTargetKeyDown(object? sender, KeyEventArgs e)
	{
		if (sender is Control control && ShouldAttachSheetBehavior(control, GetPopupType(control)) && IsKeyboardSheetInvocation(control, e.Key) && TryShowSheet(control))
		{
			e.Handled = true;
		}
	}

	private static void OnDropDownButtonClick(object? sender, RoutedEventArgs e)
	{
		if (sender is DropDownButton dropDownButton && TryShowSheet(dropDownButton))
		{
			e.Handled = true;
		}
	}

	private static bool TryShowSheet(Control control)
	{
		return ShouldUseSheet(control) && ShowSheet(control);
	}

	private static bool ShowSheet(Control control)
	{
		if (1 == 0)
		{
		}
		bool result = ((control is ComboBox comboBox) ? ShowComboBoxSheet(comboBox) : ((control is DatePicker datePicker) ? ShowDatePickerSheet(datePicker) : ((control is TimePicker timePicker) ? ShowTimePickerSheet(timePicker) : ((control is CalendarDatePicker calendarDatePicker) ? ShowCalendarDatePickerSheet(calendarDatePicker) : ((control is LuminaMultiSelect multiSelect) ? multiSelect.TryShowSheet() : ((control is DropDownButton dropDownButton) ? ShowMenuFlyoutSheet(dropDownButton, dropDownButton.Flyout, GetContentText(dropDownButton.Content, LuminaLocalization.Get("Lumina.ActionMenu.Title"))) : ((control is SplitButton splitButton) ? ShowMenuFlyoutSheet(splitButton, splitButton.Flyout, GetContentText(splitButton.Content, LuminaLocalization.Get("Lumina.ActionMenu.Title"))) : (control is CommandBar commandBar && ShowCommandBarSheet(commandBar)))))))));
		if (1 == 0)
		{
		}
		return result;
	}

	private static void OnCommandBarIsOpenChanged(CommandBar commandBar, AvaloniaPropertyChangedEventArgs change)
	{
		if (!change.GetNewValue<bool>() || !commandBar.GetValue(IsSheetBehaviorAttachedProperty) || commandBar.GetValue(IsCommandBarSheetOpeningProperty) || !ShouldUseSheet(commandBar))
		{
			return;
		}
		commandBar.SetValue(IsCommandBarSheetOpeningProperty, value: true);
		commandBar.IsOpen = false;
		try
		{
			ShowCommandBarSheet(commandBar);
		}
		finally
		{
			commandBar.SetValue(IsCommandBarSheetOpeningProperty, value: false);
		}
	}

	private static bool ShouldUseSheet(Control control)
	{
		return LuminaSheetPlacement.ShouldUseSheet(GetPopupType(control));
	}

	private static bool IsPointerSheetInvocation(Control control, object? source)
	{
		if (1 == 0)
		{
		}
		bool result = control is ComboBox || control is DatePicker || control is TimePicker || control is CalendarDatePicker || control is LuminaMultiSelect || ((control is SplitButton) ? IsSourceInsideNamedPart(source, "PART_SecondaryButton") : (control is CommandBar && IsSourceInsideNamedPart(source, "PART_OverflowButton")));
		if (1 == 0)
		{
		}
		return result;
	}

	private static bool IsKeyboardSheetInvocation(Control control, Key key)
	{
		if (1 == 0)
		{
		}
		bool result;
		if (!(control is ComboBox) && !(control is DatePicker) && !(control is TimePicker) && !(control is CalendarDatePicker) && !(control is LuminaMultiSelect))
		{
			if (control is SplitButton || control is CommandBar)
			{
				bool flag = ((key == Key.Down || key == Key.F4) ? true : false);
				result = flag;
			}
			else
			{
				result = false;
			}
		}
		else
		{
			bool flag;
			switch (key)
			{
			case Key.Return:
			case Key.Space:
			case Key.Down:
			case Key.F4:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			result = flag;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private static bool IsSourceInsideNamedPart(object? source, string partName)
	{
		if (!(source is Control sourceControl))
		{
			return false;
		}
		if (sourceControl.Name == partName)
		{
			return true;
		}
		return sourceControl.GetVisualAncestors().OfType<Control>().Any((Control control) => control.Name == partName);
	}

	private static bool ShowComboBoxSheet(ComboBox comboBox)
	{
		comboBox.IsDropDownOpen = false;
		StackPanel options = new StackPanel
		{
			Spacing = 6.0
		};
		int index = 0;
		foreach (object? item in GetComboBoxItems(comboBox))
		{
			int itemIndex = index;
			object? itemValue = item;
			Button button = CreateSheetButton(GetComboBoxItemText(itemValue), itemIndex == comboBox.SelectedIndex);
			button.Click += delegate
			{
				comboBox.SelectedIndex = itemIndex;
				comboBox.SelectedItem = itemValue;
				comboBox.IsDropDownOpen = false;
				LuminaBottomSheetService.Instance.Close(comboBox);
			};
			options.Children.Add(button);
			index++;
		}
		if (index == 0)
		{
			return false;
		}
		StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectOption"), new ScrollViewer
		{
			MaxHeight = 360.0,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = options
		});
		return LuminaBottomSheetService.Instance.TryShow(comboBox, content);
	}

	private static bool ShowDatePickerSheet(DatePicker datePicker)
	{
		DateTime selectedDate = (datePicker.SelectedDate ?? DateTimeOffset.Now).Date;
		LuminaDateWheelPicker picker = new LuminaDateWheelPicker(selectedDate, datePicker.MinYear.Date, datePicker.MaxYear.Date, datePicker.YearVisible, datePicker.MonthVisible, datePicker.DayVisible);
		StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectDate"), picker, CreatePickerActions(datePicker, delegate
		{
			datePicker.Clear();
			LuminaBottomSheetService.Instance.Close(datePicker);
		}, delegate
		{
			datePicker.SelectedDate = new DateTimeOffset(picker.SelectedDate);
			LuminaBottomSheetService.Instance.Close(datePicker);
		}));
		return LuminaBottomSheetService.Instance.TryShow(datePicker, content);
	}

	private static bool ShowTimePickerSheet(TimePicker timePicker)
	{
		LuminaTimeWheelPicker picker = new LuminaTimeWheelPicker(timePicker.SelectedTime ?? DateTime.Now.TimeOfDay, timePicker.MinuteIncrement, timePicker.SecondIncrement, timePicker.UseSeconds, Is12HourClock(timePicker.ClockIdentifier));
		StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectTime"), picker, CreatePickerActions(timePicker, delegate
		{
			timePicker.Clear();
			LuminaBottomSheetService.Instance.Close(timePicker);
		}, delegate
		{
			timePicker.SelectedTime = picker.SelectedTime;
			LuminaBottomSheetService.Instance.Close(timePicker);
		}));
		return LuminaBottomSheetService.Instance.TryShow(timePicker, content);
	}

	private static bool ShowCalendarDatePickerSheet(CalendarDatePicker calendarDatePicker)
	{
		calendarDatePicker.IsDropDownOpen = false;
		DateTime selectedDate = calendarDatePicker.SelectedDate?.Date ?? calendarDatePicker.DisplayDate.Date;
		DateTime minDate = calendarDatePicker.DisplayDateStart?.Date ?? selectedDate.AddYears(-100);
		DateTime maxDate = calendarDatePicker.DisplayDateEnd?.Date ?? selectedDate.AddYears(100);
		LuminaDateWheelPicker picker = new LuminaDateWheelPicker(selectedDate, minDate, maxDate);
		StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectDate"), picker, CreatePickerActions(calendarDatePicker, delegate
		{
			calendarDatePicker.Clear();
			calendarDatePicker.IsDropDownOpen = false;
			LuminaBottomSheetService.Instance.Close(calendarDatePicker);
		}, delegate
		{
			calendarDatePicker.SelectedDate = picker.SelectedDate;
			calendarDatePicker.IsDropDownOpen = false;
			LuminaBottomSheetService.Instance.Close(calendarDatePicker);
		}));
		return LuminaBottomSheetService.Instance.TryShow(calendarDatePicker, content);
	}

	private static bool ShowMenuFlyoutSheet(Control owner, FlyoutBase? flyout, string title)
	{
		if (!(flyout is MenuFlyout menuFlyout) || !TryCreateMenuFlyoutEntries(menuFlyout, out IReadOnlyList<ActionSheetEntry> entries))
		{
			return false;
		}
		menuFlyout.Hide();
		return ShowActionSheet(owner, title, entries);
	}

	private static bool ShowCommandBarSheet(CommandBar commandBar)
	{
		commandBar.IsOpen = false;
		IReadOnlyList<ActionSheetEntry> entries = CreateCommandBarEntries(commandBar);
		if (entries.Count == 0)
		{
			return false;
		}
		return ShowActionSheet(commandBar, LuminaLocalization.Get("Lumina.ActionMenu.Title"), entries);
	}

	private static bool ShowActionSheet(Control owner, string title, IReadOnlyList<ActionSheetEntry> entries)
	{
		StackPanel list = new StackPanel
		{
			Spacing = 4.0
		};
		foreach (ActionSheetEntry entry in entries)
		{
			if (entry.IsSeparator)
			{
				list.Children.Add(new Border
				{
					Height = 1.0,
					Margin = new Thickness(4.0, 6.0),
					Background = LuminaPickerResources.Brush("LuminaDividerBrush", Brushes.Transparent)
				});
			}
			else
			{
				list.Children.Add(CreateActionSheetButton(owner, entry));
			}
		}
		ScrollViewer body = new ScrollViewer
		{
			MaxHeight = 420.0,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = list
		};
		StackPanel content = CreateSheetLayout(title, body);
		return LuminaBottomSheetService.Instance.TryShow(owner, content);
	}

	private static bool TryCreateMenuFlyoutEntries(MenuFlyout menuFlyout, out IReadOnlyList<ActionSheetEntry> entries)
	{
		List<ActionSheetEntry> result = new List<ActionSheetEntry>();
		IEnumerable? source = GetMenuFlyoutItems(menuFlyout);
		if (source == null)
		{
			entries = result;
			return false;
		}
		foreach (object item in source)
		{
			object obj = item;
			object obj2 = obj;
			MenuItem? menuItem = obj2 as MenuItem;
			if (menuItem != null)
			{
				if (menuItem.HasSubMenu)
				{
					entries = result;
					return false;
				}
				result.Add(new ActionSheetEntry(GetContentText(menuItem.Header, string.Empty), menuItem.Icon, CanInvokeMenuItem(menuItem), menuItem.ToggleType != MenuItemToggleType.None && menuItem.IsChecked, delegate
				{
					InvokeMenuItem(menuItem);
				}));
			}
			else if (!(obj2 is Separator))
			{
				if (obj2 != null)
				{
					entries = result;
					return false;
				}
			}
			else
			{
				result.Add(ActionSheetEntry.Separator());
			}
		}
		entries = result;
		return result.Any((ActionSheetEntry entry) => !entry.IsSeparator);
	}

	private static IEnumerable? GetMenuFlyoutItems(MenuFlyout menuFlyout)
	{
		if (menuFlyout.Items.Count > 0)
		{
			return menuFlyout.Items;
		}
		IEnumerable? source = menuFlyout.ItemsSource;
		if (source != null && !(source is string))
		{
			return source;
		}
		return (menuFlyout is IEnumerable enumerable) ? enumerable : null;
	}

	private static IReadOnlyList<ActionSheetEntry> CreateCommandBarEntries(CommandBar commandBar)
	{
		List<ActionSheetEntry> entries = new List<ActionSheetEntry>();
		IList<ICommandBarElement> source = commandBar.OverflowItems.Any() ? commandBar.OverflowItems : commandBar.SecondaryCommands;
		AddCommandBarEntries(entries, source);
		TrimTrailingSeparator(entries);
		return entries;
	}

	private static void AddCommandBarEntries(List<ActionSheetEntry> entries, IEnumerable<ICommandBarElement> source)
	{
		foreach (ICommandBarElement item in source)
		{
			ActionSheetEntry? entry = CreateCommandBarEntry(item);
			if (entry == null)
			{
				continue;
			}
			if (entry.IsSeparator)
			{
				if (entries.Count > 0 && !entries[^1].IsSeparator)
				{
					entries.Add(entry);
				}
				continue;
			}
			entries.Add(entry);
		}
	}

	private static ActionSheetEntry? CreateCommandBarEntry(ICommandBarElement commandBarElement)
	{
		if (commandBarElement is CommandBarToggleButton commandBarToggleButton)
		{
			return new ActionSheetEntry(GetContentText(commandBarToggleButton.Label, string.Empty), commandBarToggleButton.Icon, CanExecuteCommand(commandBarToggleButton), commandBarToggleButton.IsChecked == true, delegate
			{
				InvokeCommandBarToggleButton(commandBarToggleButton);
			});
		}
		if (commandBarElement is CommandBarButton commandBarButton)
		{
			return new ActionSheetEntry(GetContentText(commandBarButton.Label, string.Empty), commandBarButton.Icon, CanExecuteCommand(commandBarButton), IsChecked: false, delegate
			{
				InvokeCommandBarButton(commandBarButton);
			});
		}
		return commandBarElement is CommandBarSeparator ? ActionSheetEntry.Separator() : null;
	}

	private static void TrimTrailingSeparator(List<ActionSheetEntry> entries)
	{
		if (entries.Count > 0 && entries[^1].IsSeparator)
		{
			entries.RemoveAt(entries.Count - 1);
		}
	}

	private static Button CreateActionSheetButton(Control owner, ActionSheetEntry entry)
	{
		TextBlock label = new TextBlock
		{
			Text = entry.Text,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		Grid grid = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("24,*,24"),
			ColumnSpacing = 12.0,
			Children = 
			{
				CreateActionIcon(entry),
				(Control)label
			}
		};
		Grid.SetColumn(label, 1);
		if (entry.IsChecked)
		{
			PathIcon checkIcon = CreatePathIcon("M9,16.2 L4.8,12 L3.4,13.4 L9,19 L21,7 L19.6,5.6 Z", "LuminaPrimaryBrush");
			Grid.SetColumn(checkIcon, 2);
			grid.Children.Add(checkIcon);
		}
		Button button = new Button
		{
			Content = grid,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			IsEnabled = entry.IsEnabled
		};
		button.Classes.Add("ActionSheetItem");
		button.Click += delegate
		{
			InvokeSheetAction(owner, entry);
		};
		return button;
	}

	private static Control CreateActionIcon(ActionSheetEntry entry)
	{
		PathIcon? icon = ClonePathIcon(entry.Icon);
		if (icon != null)
		{
			icon.Foreground = LuminaPickerResources.Brush(entry.IsChecked ? "LuminaPrimaryBrush" : "LuminaTextMutedBrush", Brushes.Gray);
			return icon;
		}
		return new Border
		{
			Width = 20.0,
			Height = 20.0
		};
	}

	private static PathIcon? ClonePathIcon(object? icon)
	{
		return (icon is PathIcon pathIcon) ? new PathIcon
		{
			Width = (double.IsNaN(pathIcon.Width) ? 20.0 : pathIcon.Width),
			Height = (double.IsNaN(pathIcon.Height) ? 20.0 : pathIcon.Height),
			Data = pathIcon.Data,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		} : null;
	}

	private static PathIcon CreatePathIcon(string data, string brushKey)
	{
		return new PathIcon
		{
			Width = 18.0,
			Height = 18.0,
			Data = StreamGeometry.Parse(data),
			Foreground = LuminaPickerResources.Brush(brushKey, Brushes.DodgerBlue),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	private static void InvokeSheetAction(Control owner, ActionSheetEntry entry)
	{
		if (entry.IsEnabled && entry.Invoke != null)
		{
			LuminaBottomSheetService.Instance.Close(owner);
			Avalonia.Threading.Dispatcher.UIThread.Post(entry.Invoke);
		}
	}

	private static bool CanInvokeMenuItem(MenuItem menuItem)
	{
		return menuItem.IsEnabled && (menuItem.Command == null || menuItem.Command.CanExecute(menuItem.CommandParameter));
	}

	private static void InvokeMenuItem(MenuItem menuItem)
	{
		if (menuItem.IsEnabled)
		{
			if (menuItem.ToggleType == MenuItemToggleType.CheckBox)
			{
				menuItem.IsChecked = !menuItem.IsChecked;
			}
			else if (menuItem.ToggleType == MenuItemToggleType.Radio)
			{
				menuItem.IsChecked = true;
			}
			ExecuteCommand(menuItem);
			menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
		}
	}

	private static bool CanExecuteCommand(ICommandSource commandSource)
	{
		return commandSource.Command == null || commandSource.Command.CanExecute(commandSource.CommandParameter);
	}

	private static void InvokeCommandBarButton(CommandBarButton commandButton)
	{
		ExecuteCommand(commandButton);
		commandButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
	}

	private static void InvokeCommandBarToggleButton(CommandBarToggleButton toggleButton)
	{
		toggleButton.IsChecked = !toggleButton.IsChecked;
		ExecuteCommand(toggleButton);
		toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
	}

	private static void ExecuteCommand(ICommandSource commandSource)
	{
		ICommand? command = commandSource.Command;
		if (command != null && command.CanExecute(commandSource.CommandParameter))
		{
			command.Execute(commandSource.CommandParameter);
		}
	}

	private static string GetContentText(object? content, string fallback)
	{
		if (1 == 0)
		{
		}
		string result;
		if (!(content is string text))
		{
			if (!(content is TextBlock textBlock))
			{
				if (!(content is ContentControl contentControl))
				{
					if (content is Control)
					{
						goto IL_006f;
					}
					if (content != null)
					{
						goto IL_0077;
					}
					result = fallback;
				}
				else
				{
					result = GetContentText(contentControl.Content, fallback);
				}
			}
			else
			{
				if (string.IsNullOrWhiteSpace(textBlock.Text))
				{
					goto IL_006f;
				}
				result = textBlock.Text;
			}
		}
		else
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				goto IL_0077;
			}
			result = text;
		}
		goto IL_0085;
		IL_006f:
		result = fallback;
		goto IL_0085;
		IL_0077:
		result = content.ToString() ?? fallback;
		goto IL_0085;
		IL_0085:
		if (1 == 0)
		{
		}
		return result;
	}

	private static bool Is12HourClock(string? clockIdentifier)
	{
		return string.Equals(clockIdentifier, "12HourClock", StringComparison.OrdinalIgnoreCase);
	}

	private static StackPanel CreateSheetLayout(string title, Control body, Control? footer = null)
	{
		StackPanel layout = new StackPanel
		{
			Spacing = 16.0
		};
		layout.Children.Add(new TextBlock
		{
			Text = title,
			FontSize = 18.0,
			FontWeight = FontWeight.DemiBold
		});
		layout.Children.Add(body);
		if (footer != null)
		{
			layout.Children.Add(footer);
		}
		return layout;
	}

	private static Button CreateSheetButton(string text, bool isSelected)
	{
		Button button = new Button
		{
			Content = text,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Left
		};
		button.Classes.Add(isSelected ? "Primary" : "Ghost");
		return button;
	}

	private static Control CreatePickerActions(Control owner, Action onClear, Action onApply)
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
			LuminaBottomSheetService.Instance.Close(owner);
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

	private static string GetComboBoxItemText(object? item)
	{
		if (1 == 0)
		{
		}
		string result;
		if (!(item is ComboBoxItem comboBoxItem))
		{
			if (item != null)
			{
				goto IL_003f;
			}
			result = string.Empty;
		}
		else
		{
			if (comboBoxItem.Content == null)
			{
				goto IL_003f;
			}
			result = comboBoxItem.Content.ToString() ?? string.Empty;
		}
		goto IL_0051;
		IL_0051:
		if (1 == 0)
		{
		}
		return result;
		IL_003f:
		result = item.ToString() ?? string.Empty;
		goto IL_0051;
	}

	private static IEnumerable GetComboBoxItems(ComboBox comboBox)
	{
		if (comboBox.Items.Count > 0)
		{
			return comboBox.Items;
		}
		IEnumerable? source = comboBox.ItemsSource;
		if (source != null && !(source is string))
		{
			return source;
		}
		return Array.Empty<object>();
	}
}
