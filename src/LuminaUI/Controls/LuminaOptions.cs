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
        PopupTypeProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs change) =>
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
        DateFormatProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs change) =>
        {
            ApplyDateFormat(control, change.GetNewValue<string>());
        });
        TimeFormatProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs change) =>
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
        if (control is not LuminaDateRangePicker dateRangePicker)
        {
            if (control is not CalendarDatePicker calendarDatePicker)
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
            dateRangePicker.DateFormat = string.IsNullOrWhiteSpace(format) ? "yyyy-MM-dd" : format;
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
        bool result = (popupType == LuminaPopupType.Auto || popupType == LuminaPopupType.Sheet) && IsSheetTarget(control);
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
        return control switch
        {
            ComboBox comboBox => ShowComboBoxSheet(comboBox),
            DatePicker datePicker => ShowDatePickerSheet(datePicker),
            TimePicker timePicker => ShowTimePickerSheet(timePicker),
            CalendarDatePicker calendarDatePicker => ShowCalendarDatePickerSheet(calendarDatePicker),
            LuminaMultiSelect multiSelect => multiSelect.TryShowSheet(),
            DropDownButton dropDownButton => ShowMenuFlyoutSheet(dropDownButton, dropDownButton.Flyout, GetContentText(dropDownButton.Content, LuminaLocalization.Get("Lumina.ActionMenu.Title"))),
            SplitButton splitButton => ShowMenuFlyoutSheet(splitButton, splitButton.Flyout, GetContentText(splitButton.Content, LuminaLocalization.Get("Lumina.ActionMenu.Title"))),
            CommandBar commandBar => ShowCommandBarSheet(commandBar),
            _ => false,
        };
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
        return control is ComboBox or DatePicker or TimePicker or CalendarDatePicker or LuminaMultiSelect
            || control switch
            {
                SplitButton => IsSourceInsideNamedPart(source, "PART_SecondaryButton"),
                CommandBar => IsSourceInsideNamedPart(source, "PART_OverflowButton"),
                _ => false,
            };
    }

    private static bool IsKeyboardSheetInvocation(Control control, Key key)
    {
        if (control is ComboBox or DatePicker or TimePicker or CalendarDatePicker or LuminaMultiSelect)
        {
            return key is Key.Return or Key.Space or Key.Down or Key.F4;
        }
        if (control is SplitButton or CommandBar)
        {
            return key is Key.Down or Key.F4;
        }
        return false;
    }

    private static bool IsSourceInsideNamedPart(object? source, string partName)
    {
        if (source is not Control sourceControl)
        {
            return false;
        }
        if (sourceControl.Name == partName)
        {
            return true;
        }
        return sourceControl.GetVisualAncestors().OfType<Control>().Any(control => control.Name == partName);
    }

    private static bool ShowComboBoxSheet(ComboBox comboBox)
    {
        comboBox.IsDropDownOpen = false;
        StackPanel options = new StackPanel();
        LuminaPickerResources.BindResource(options, StackPanel.SpacingProperty, "LuminaComboBoxSheetOptionsSpacing");
        int index = 0;
        foreach (object? item in GetComboBoxItems(comboBox))
        {
            int itemIndex = index;
            object? itemValue = item;
            Button button = CreateSheetButton(GetComboBoxItemText(itemValue), itemIndex == comboBox.SelectedIndex);
            button.Click += (_, _) => {
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
            MaxHeight = LuminaPickerResources.Double("LuminaComboBoxSheetBodyMaxHeight", 360.0),
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
        StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectDate"), picker, CreatePickerActions(datePicker, () => {
            datePicker.Clear();
            LuminaBottomSheetService.Instance.Close(datePicker);
        }, () => {
            datePicker.SelectedDate = new DateTimeOffset(picker.SelectedDate);
            LuminaBottomSheetService.Instance.Close(datePicker);
        }));
        return LuminaBottomSheetService.Instance.TryShow(datePicker, content);
    }

    private static bool ShowTimePickerSheet(TimePicker timePicker)
    {
        LuminaTimeWheelPicker picker = new LuminaTimeWheelPicker(timePicker.SelectedTime ?? DateTime.Now.TimeOfDay, timePicker.MinuteIncrement, timePicker.SecondIncrement, timePicker.UseSeconds, Is12HourClock(timePicker.ClockIdentifier));
        StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectTime"), picker, CreatePickerActions(timePicker, () => {
            timePicker.Clear();
            LuminaBottomSheetService.Instance.Close(timePicker);
        }, () => {
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
        StackPanel content = CreateSheetLayout(LuminaLocalization.Get("Lumina.Picker.SelectDate"), picker, CreatePickerActions(calendarDatePicker, () => {
            calendarDatePicker.Clear();
            calendarDatePicker.IsDropDownOpen = false;
            LuminaBottomSheetService.Instance.Close(calendarDatePicker);
        }, () => {
            calendarDatePicker.SelectedDate = picker.SelectedDate;
            calendarDatePicker.IsDropDownOpen = false;
            LuminaBottomSheetService.Instance.Close(calendarDatePicker);
        }));
        return LuminaBottomSheetService.Instance.TryShow(calendarDatePicker, content);
    }

    private static bool ShowMenuFlyoutSheet(Control owner, FlyoutBase? flyout, string title)
    {
        if (flyout is not MenuFlyout menuFlyout || !TryCreateMenuFlyoutEntries(menuFlyout, out IReadOnlyList<ActionSheetEntry> entries))
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
        StackPanel list = new StackPanel();
        LuminaPickerResources.BindResource(list, StackPanel.SpacingProperty, "LuminaActionSheetListSpacing");
        foreach (ActionSheetEntry entry in entries)
        {
            if (entry.IsSeparator)
            {
                Border separator = new Border
                {
                    Background = LuminaPickerResources.Brush("LuminaDividerBrush", Brushes.Transparent)
                };
                LuminaPickerResources.BindResource(separator, Layoutable.HeightProperty, "LuminaActionSheetSeparatorHeight");
                LuminaPickerResources.BindResource(separator, Border.MarginProperty, "LuminaActionSheetSeparatorMargin");
                list.Children.Add(separator);
            }
            else
            {
                list.Children.Add(CreateActionSheetButton(owner, entry));
            }
        }
        ScrollViewer body = new ScrollViewer
        {
            MaxHeight = LuminaPickerResources.Double("LuminaActionSheetBodyMaxHeight", 420.0),
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
            if (item is MenuItem menuItem)
            {
                if (menuItem.HasSubMenu)
                {
                    entries = result;
                    return false;
                }
                result.Add(new ActionSheetEntry(GetContentText(menuItem.Header, string.Empty), menuItem.Icon, CanInvokeMenuItem(menuItem), menuItem.ToggleType != MenuItemToggleType.None && menuItem.IsChecked, () => InvokeMenuItem(menuItem)));
            }
            else if (item is Separator)
            {
                result.Add(ActionSheetEntry.Separator());
            }
            else if (item != null)
            {
                entries = result;
                return false;
            }
        }
        entries = result;
        return result.Any(entry => !entry.IsSeparator);
    }

    private static IEnumerable? GetMenuFlyoutItems(MenuFlyout menuFlyout)
    {
        if (menuFlyout.Items.Count > 0)
        {
            return menuFlyout.Items;
        }
        IEnumerable? source = menuFlyout.ItemsSource;
        if (source != null && source is not string)
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
            return new ActionSheetEntry(GetContentText(commandBarToggleButton.Label, string.Empty), commandBarToggleButton.Icon, CanExecuteCommand(commandBarToggleButton), commandBarToggleButton.IsChecked == true, () => {
                InvokeCommandBarToggleButton(commandBarToggleButton);
            });
        }
        if (commandBarElement is CommandBarButton commandBarButton)
        {
            return new ActionSheetEntry(GetContentText(commandBarButton.Label, string.Empty), commandBarButton.Icon, CanExecuteCommand(commandBarButton), IsChecked: false, () => {
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
            Children = 
            {
                CreateActionIcon(entry),
                (Control)label
            }
        };
        LuminaPickerResources.BindResource(grid, Grid.ColumnSpacingProperty, "LuminaActionSheetItemColumnSpacing");
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
        button.Click += (_, _) => {
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
            Width = LuminaPickerResources.Double("LuminaActionSheetIconSize", 20.0),
            Height = LuminaPickerResources.Double("LuminaActionSheetIconSize", 20.0)
        };
    }

    private static PathIcon? ClonePathIcon(object? icon)
    {
        return (icon is PathIcon pathIcon) ? new PathIcon
        {
            Width = (double.IsNaN(pathIcon.Width) ? LuminaPickerResources.Double("LuminaActionSheetIconSize", 20.0) : pathIcon.Width),
            Height = (double.IsNaN(pathIcon.Height) ? LuminaPickerResources.Double("LuminaActionSheetIconSize", 20.0) : pathIcon.Height),
            Data = pathIcon.Data,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        } : null;
    }

    private static PathIcon CreatePathIcon(string data, string brushKey)
    {
        return new PathIcon
        {
            Width = LuminaPickerResources.Double("LuminaActionSheetCheckIconSize", 18.0),
            Height = LuminaPickerResources.Double("LuminaActionSheetCheckIconSize", 18.0),
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
        return content switch
        {
            string text => !string.IsNullOrWhiteSpace(text) ? text : (content.ToString() ?? fallback),
            TextBlock textBlock => string.IsNullOrWhiteSpace(textBlock.Text) ? fallback : textBlock.Text,
            ContentControl contentControl => GetContentText(contentControl.Content, fallback),
            Control => fallback,
            not null => content.ToString() ?? fallback,
            _ => fallback
        };
    }

    private static bool Is12HourClock(string? clockIdentifier)
    {
        return string.Equals(clockIdentifier, "12HourClock", StringComparison.OrdinalIgnoreCase);
    }

    private static StackPanel CreateSheetLayout(string title, Control body, Control? footer = null)
    {
        StackPanel layout = new StackPanel();
        LuminaPickerResources.BindResource(layout, StackPanel.SpacingProperty, "LuminaSheetLayoutSpacing");
        TextBlock titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.DemiBold
        };
        LuminaPickerResources.BindResource(titleBlock, TextBlock.FontSizeProperty, "LuminaSheetTitleFontSize");
        layout.Children.Add(titleBlock);
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
        clearButton.Click += (_, _) => {
            onClear();
        };
        Button cancelButton = new Button
        {
            Content = LuminaLocalization.Get("Lumina.Common.Cancel")
        };
        cancelButton.Classes.Add("Outline");
        cancelButton.Click += (_, _) => {
            LuminaBottomSheetService.Instance.Close(owner);
        };
        Button applyButton = new Button
        {
            Content = LuminaLocalization.Get("Lumina.Common.Done")
        };
        applyButton.Classes.Add("Primary");
        applyButton.Click += (_, _) => {
            onApply();
        };
        Grid.SetColumn(cancelButton, 1);
        Grid.SetColumn(applyButton, 2);
        Grid actions = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Children =
            {
                clearButton,
                cancelButton,
                applyButton
            }
        };
        LuminaPickerResources.BindResource(actions, Grid.ColumnSpacingProperty, "LuminaSheetActionsColumnSpacing");
        return actions;
    }

    private static string GetComboBoxItemText(object? item)
    {
        if (item is ComboBoxItem { Content: not null } comboBoxItem)
        {
            return comboBoxItem.Content.ToString() ?? string.Empty;
        }
        return item?.ToString() ?? string.Empty;
    }

    private static IEnumerable GetComboBoxItems(ComboBox comboBox)
    {
        if (comboBox.Items.Count > 0)
        {
            return comboBox.Items;
        }
        IEnumerable? source = comboBox.ItemsSource;
        if (source != null && source is not string)
        {
            return source;
        }
        return Array.Empty<object>();
    }
}
