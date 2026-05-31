using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Controls;

public class LuminaSettingsOption : Button
{
    private Popup? _selectPopup;

    private Popup? _inputPopup;

    private bool _isSyncingSelection;

    private bool _isSyncingOptions;

    private INotifyCollectionChanged? _selectItemsNotifier;

    private readonly AvaloniaList<LuminaSettingsSelectOptionItem> _selectOptions = new AvaloniaList<LuminaSettingsSelectOptionItem>();

    private bool _hasIcon;

    private bool _hasDescription;

    private bool _hasValue;

    private bool _hasTrailingContent;

    private bool _showValueAccessory;

    private bool _showTrailingAccessory;

    private bool _showSwitchAccessory;

    private bool _showTextInputAccessory;

    private bool _showSelectAccessory;

    private bool _showChevronIcon;

    private string _selectedDisplayText = string.Empty;

    private string _inputDisplayText = string.Empty;

    public static readonly StyledProperty<LuminaSettingsOptionKind> KindProperty = AvaloniaProperty.Register<LuminaSettingsOption, LuminaSettingsOptionKind>(nameof(Kind), LuminaSettingsOptionKind.Navigation);

    public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<LuminaSettingsOption, string?>(nameof(Header));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaSettingsOption, string?>(nameof(Description));

    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<LuminaSettingsOption, object?>(nameof(Icon));

    public static readonly StyledProperty<object?> ValueProperty = AvaloniaProperty.Register<LuminaSettingsOption, object?>(nameof(Value));

    public static readonly StyledProperty<object?> TrailingContentProperty = AvaloniaProperty.Register<LuminaSettingsOption, object?>(nameof(TrailingContent));

    public static readonly StyledProperty<bool> ShowChevronProperty = AvaloniaProperty.Register<LuminaSettingsOption, bool>(nameof(ShowChevron), defaultValue: true);

    public static readonly StyledProperty<bool?> IsCheckedProperty = AvaloniaProperty.Register<LuminaSettingsOption, bool?>(nameof(IsChecked), null, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<string?> InputTextProperty = AvaloniaProperty.Register<LuminaSettingsOption, string?>(nameof(InputText), null, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty = AvaloniaProperty.Register<LuminaSettingsOption, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<IEnumerable?> SelectItemsSourceProperty = AvaloniaProperty.Register<LuminaSettingsOption, IEnumerable?>(nameof(SelectItemsSource));

    public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<LuminaSettingsOption, int>(nameof(SelectedIndex), -1, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty = AvaloniaProperty.Register<LuminaSettingsOption, object?>(nameof(SelectedItem), null, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<double> AccessoryMinWidthProperty = AvaloniaProperty.Register<LuminaSettingsOption, double>(nameof(AccessoryMinWidth), 112.0);

    public static readonly StyledProperty<double> AccessoryMaxWidthProperty = AvaloniaProperty.Register<LuminaSettingsOption, double>(nameof(AccessoryMaxWidth), 220.0);

    public static readonly StyledProperty<LuminaPopupType> PopupTypeProperty = AvaloniaProperty.Register<LuminaSettingsOption, LuminaPopupType>(nameof(PopupType), LuminaPopupType.Auto);

    public static readonly StyledProperty<bool> IsSelectPopupOpenProperty = AvaloniaProperty.Register<LuminaSettingsOption, bool>(nameof(IsSelectPopupOpen), defaultValue: false, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsInputPopupOpenProperty = AvaloniaProperty.Register<LuminaSettingsOption, bool>(nameof(IsInputPopupOpen), defaultValue: false, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<string?> InputDraftTextProperty = AvaloniaProperty.Register<LuminaSettingsOption, string?>(nameof(InputDraftText), null, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty = AvaloniaProperty.Register<LuminaSettingsOption, ICommand?>(nameof(SelectionChangedCommand));

    public static readonly DirectProperty<LuminaSettingsOption, bool> HasIconProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(HasIcon), (LuminaSettingsOption option) => option.HasIcon, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(HasDescription), (LuminaSettingsOption option) => option.HasDescription, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> HasValueProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(HasValue), (LuminaSettingsOption option) => option.HasValue, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> HasTrailingContentProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(HasTrailingContent), (LuminaSettingsOption option) => option.HasTrailingContent, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> ShowValueAccessoryProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(ShowValueAccessory), (LuminaSettingsOption option) => option.ShowValueAccessory, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> ShowTrailingAccessoryProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(ShowTrailingAccessory), (LuminaSettingsOption option) => option.ShowTrailingAccessory, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> ShowSwitchAccessoryProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(ShowSwitchAccessory), (LuminaSettingsOption option) => option.ShowSwitchAccessory, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> ShowTextInputAccessoryProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(ShowTextInputAccessory), (LuminaSettingsOption option) => option.ShowTextInputAccessory, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> ShowSelectAccessoryProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(ShowSelectAccessory), (LuminaSettingsOption option) => option.ShowSelectAccessory, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, bool> ShowChevronIconProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, bool>(nameof(ShowChevronIcon), (LuminaSettingsOption option) => option.ShowChevronIcon, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsOption, AvaloniaList<LuminaSettingsSelectOptionItem>> SelectOptionsProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, AvaloniaList<LuminaSettingsSelectOptionItem>>("SelectOptions", (LuminaSettingsOption option) => option.SelectOptions);

    public static readonly DirectProperty<LuminaSettingsOption, string> SelectedDisplayTextProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, string>(nameof(SelectedDisplayText), (LuminaSettingsOption option) => option.SelectedDisplayText);

    public static readonly DirectProperty<LuminaSettingsOption, string> InputDisplayTextProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsOption, string>(nameof(InputDisplayText), (LuminaSettingsOption option) => option.InputDisplayText);

    public LuminaSettingsOptionKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public bool ShowChevron
    {
        get => GetValue(ShowChevronProperty);
        set => SetValue(ShowChevronProperty, value);
    }

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public string? InputText
    {
        get => GetValue(InputTextProperty);
        set => SetValue(InputTextProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public IEnumerable? SelectItemsSource
    {
        get => GetValue(SelectItemsSourceProperty);
        set => SetValue(SelectItemsSourceProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public double AccessoryMinWidth
    {
        get => GetValue(AccessoryMinWidthProperty);
        set => SetValue(AccessoryMinWidthProperty, value);
    }

    public double AccessoryMaxWidth
    {
        get => GetValue(AccessoryMaxWidthProperty);
        set => SetValue(AccessoryMaxWidthProperty, value);
    }

    public LuminaPopupType PopupType
    {
        get => GetValue(PopupTypeProperty);
        set => SetValue(PopupTypeProperty, value);
    }

    public bool IsSelectPopupOpen
    {
        get => GetValue(IsSelectPopupOpenProperty);
        set => SetValue(IsSelectPopupOpenProperty, value);
    }

    public bool IsInputPopupOpen
    {
        get => GetValue(IsInputPopupOpenProperty);
        set => SetValue(IsInputPopupOpenProperty, value);
    }

    public string? InputDraftText
    {
        get => GetValue(InputDraftTextProperty);
        set => SetValue(InputDraftTextProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public bool HasIcon
    {
        get
        {
            return _hasIcon;
        }
        private set
        {
            SetAndRaise(HasIconProperty, ref _hasIcon, value);
        }
    }

    public bool HasDescription
    {
        get
        {
            return _hasDescription;
        }
        private set
        {
            SetAndRaise(HasDescriptionProperty, ref _hasDescription, value);
        }
    }

    public bool HasValue
    {
        get
        {
            return _hasValue;
        }
        private set
        {
            SetAndRaise(HasValueProperty, ref _hasValue, value);
        }
    }

    public bool HasTrailingContent
    {
        get
        {
            return _hasTrailingContent;
        }
        private set
        {
            SetAndRaise(HasTrailingContentProperty, ref _hasTrailingContent, value);
        }
    }

    public bool ShowValueAccessory
    {
        get
        {
            return _showValueAccessory;
        }
        private set
        {
            SetAndRaise(ShowValueAccessoryProperty, ref _showValueAccessory, value);
        }
    }

    public bool ShowTrailingAccessory
    {
        get
        {
            return _showTrailingAccessory;
        }
        private set
        {
            SetAndRaise(ShowTrailingAccessoryProperty, ref _showTrailingAccessory, value);
        }
    }

    public bool ShowSwitchAccessory
    {
        get
        {
            return _showSwitchAccessory;
        }
        private set
        {
            SetAndRaise(ShowSwitchAccessoryProperty, ref _showSwitchAccessory, value);
        }
    }

    public bool ShowTextInputAccessory
    {
        get
        {
            return _showTextInputAccessory;
        }
        private set
        {
            SetAndRaise(ShowTextInputAccessoryProperty, ref _showTextInputAccessory, value);
        }
    }

    public bool ShowSelectAccessory
    {
        get
        {
            return _showSelectAccessory;
        }
        private set
        {
            SetAndRaise(ShowSelectAccessoryProperty, ref _showSelectAccessory, value);
        }
    }

    public bool ShowChevronIcon
    {
        get
        {
            return _showChevronIcon;
        }
        private set
        {
            SetAndRaise(ShowChevronIconProperty, ref _showChevronIcon, value);
        }
    }

    public AvaloniaList<LuminaSettingsSelectOptionItem> SelectOptions => _selectOptions;

    public string SelectedDisplayText
    {
        get
        {
            return _selectedDisplayText;
        }
        private set
        {
            SetAndRaise(SelectedDisplayTextProperty, ref _selectedDisplayText, value);
        }
    }

    public string InputDisplayText
    {
        get
        {
            return _inputDisplayText;
        }
        private set
        {
            SetAndRaise(InputDisplayTextProperty, ref _inputDisplayText, value);
        }
    }

    public LuminaSettingsOption()
    {
        RebuildSelectOptions();
        UpdateAccessoryState();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_selectPopup != null)
        {
            _selectPopup.Closed -= OnSelectPopupClosed;
        }
        if (_inputPopup != null)
        {
            _inputPopup.Closed -= OnInputPopupClosed;
        }
        RemoveHandler(Button.ClickEvent, OnTemplateButtonClick);
        _selectPopup = e.NameScope.Find<Popup>("PART_SelectPopup");
        if (_selectPopup != null)
        {
            _selectPopup.Closed += OnSelectPopupClosed;
        }
        _inputPopup = e.NameScope.Find<Popup>("PART_InputPopup");
        if (_inputPopup != null)
        {
            _inputPopup.Closed += OnInputPopupClosed;
        }
        AddHandler(Button.ClickEvent, OnTemplateButtonClick, RoutingStrategies.Bubble);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_selectPopup != null)
        {
            _selectPopup.Closed -= OnSelectPopupClosed;
        }
        if (_inputPopup != null)
        {
            _inputPopup.Closed -= OnInputPopupClosed;
        }
        RemoveHandler(Button.ClickEvent, OnTemplateButtonClick);
        SubscribeToSelectItems(null);
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty)
        {
            HasIcon = HasContent(Icon);
        }
        else if (change.Property == DescriptionProperty)
        {
            HasDescription = !string.IsNullOrWhiteSpace(Description);
        }
        else if (change.Property == ValueProperty)
        {
            HasValue = HasContent(Value);
            UpdateAccessoryState();
        }
        else if (change.Property == TrailingContentProperty)
        {
            HasTrailingContent = HasContent(TrailingContent);
            UpdateAccessoryState();
        }
        else if (change.Property == KindProperty || change.Property == ShowChevronProperty)
        {
            UpdateAccessoryState();
        }
        else if (change.Property == SelectItemsSourceProperty)
        {
            SubscribeToSelectItems(SelectItemsSource);
            RebuildSelectOptions();
        }
        else if (change.Property == SelectedIndexProperty && !_isSyncingSelection)
        {
            SelectByIndex(SelectedIndex, closePopup: false, executeCommand: false);
        }
        else if (change.Property == SelectedItemProperty && !_isSyncingSelection)
        {
            SelectByItem(SelectedItem, closePopup: false, executeCommand: false);
        }
        else if (change.Property == PlaceholderTextProperty)
        {
            UpdateSelectedDisplayText();
            UpdateInputDisplayText();
        }
        else if (change.Property == IsSelectPopupOpenProperty && !IsSelectPopupOpen)
        {
            _selectPopup?.Close();
        }
        else if (change.Property == IsSelectPopupOpenProperty && IsSelectPopupOpen)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(AttachPopupButtonHandlers, DispatcherPriority.Loaded);
        }
        else if (change.Property == IsInputPopupOpenProperty && !IsInputPopupOpen)
        {
            _inputPopup?.Close();
        }
        else if (change.Property == IsInputPopupOpenProperty && IsInputPopupOpen)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(AttachPopupButtonHandlers, DispatcherPriority.Loaded);
        }
        else if (change.Property == InputTextProperty)
        {
            UpdateInputDisplayText();
        }
    }

    protected override void OnClick()
    {
        if (Kind == LuminaSettingsOptionKind.Select)
        {
            OpenSelect();
        }
        else if (Kind == LuminaSettingsOptionKind.TextInput)
        {
            OpenInput();
        }
        else
        {
            base.OnClick();
        }
    }

    private static bool HasContent(object? value)
    {
        return value != null && (value is not string text || !string.IsNullOrWhiteSpace(text));
    }

    private void UpdateAccessoryState()
    {
        LuminaSettingsOptionKind kind = Kind;
        ShowValueAccessory = HasValue && kind is LuminaSettingsOptionKind.Navigation or LuminaSettingsOptionKind.Value;
        ShowTrailingAccessory = HasTrailingContent && kind is LuminaSettingsOptionKind.Navigation or LuminaSettingsOptionKind.Value or LuminaSettingsOptionKind.Custom;
        ShowSwitchAccessory = kind == LuminaSettingsOptionKind.Switch;
        ShowTextInputAccessory = kind == LuminaSettingsOptionKind.TextInput;
        ShowSelectAccessory = kind == LuminaSettingsOptionKind.Select;
        ShowChevronIcon = ShowChevron && kind is LuminaSettingsOptionKind.Navigation or LuminaSettingsOptionKind.TextInput or LuminaSettingsOptionKind.Select;
        UpdateInputDisplayText();
    }

    private void OpenInput()
    {
        InputDraftText = InputText;
        if (ShouldUseSheet())
        {
            IsInputPopupOpen = false;
            TryShowInputSheet();
        }
        else
        {
            IsInputPopupOpen = true;
        }
    }

    private bool TryShowInputSheet()
    {
        TextBox input = new TextBox
        {
            Text = InputText,
            PlaceholderText = PlaceholderText,
            MinHeight = 38.0,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        Button cancelButton = new Button
        {
            Content = LuminaLocalization.Get("Lumina.Common.Cancel")
        };
        cancelButton.Classes.Add("Outline");
        cancelButton.Click += (_, _) => LuminaBottomSheetService.Instance.Close(this);
        Button doneButton = new Button
        {
            Content = LuminaLocalization.Get("Lumina.Common.Done")
        };
        doneButton.Classes.Add("Primary");
        doneButton.Click += (_, _) => {
            CommitInput(input.Text);
            LuminaBottomSheetService.Instance.Close(this);
        };
        Grid.SetColumn(cancelButton, 1);
        Grid.SetColumn(doneButton, 2);
        Grid actions = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 8.0,
            Children =
            {
                cancelButton,
                doneButton
            }
        };
        StackPanel content = new StackPanel
        {
            Spacing = 16.0,
            Children =
            {
                new TextBlock
                {
                    Text = (Header?.ToString() ?? PlaceholderText ?? string.Empty),
                    FontSize = 18.0,
                    FontWeight = FontWeight.DemiBold,
                    Foreground = LuminaPickerResources.Brush("LuminaTextForegroundBrush", Brushes.White)
                },
                input,
                actions
            }
        };
        return LuminaBottomSheetService.Instance.TryShow(this, content);
    }

    private void OpenSelect()
    {
        if (ShouldUseSheet())
        {
            IsSelectPopupOpen = false;
            TryShowSelectSheet();
        }
        else
        {
            IsSelectPopupOpen = true;
        }
    }

    private bool ShouldUseSheet()
    {
        return LuminaSheetPlacement.ShouldUseSheet(PopupType);
    }

    private bool TryShowSelectSheet()
    {
        StackPanel list = new StackPanel
        {
            Spacing = 4.0
        };
        if (SelectOptions.Count == 0)
        {
            list.Children.Add(CreateSheetEmptyText());
        }
        else
        {
            foreach (LuminaSettingsSelectOptionItem option in SelectOptions)
            {
                list.Children.Add(CreateSheetOptionButton(option));
            }
        }
        ScrollViewer body = new ScrollViewer
        {
            MaxHeight = 420.0,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = list
        };
        StackPanel content = new StackPanel
        {
            Spacing = 16.0,
            Children =
            {
                new TextBlock
                {
                    Text = (Header?.ToString() ?? LuminaLocalization.Get("Lumina.Picker.SelectOption")),
                    FontSize = 18.0,
                    FontWeight = FontWeight.DemiBold,
                    Foreground = LuminaPickerResources.Brush("LuminaTextForegroundBrush", Brushes.White)
                },
                body
            }
        };
        return LuminaBottomSheetService.Instance.TryShow(this, content);
    }

    private static TextBlock CreateSheetEmptyText()
    {
        return new TextBlock
        {
            Text = LuminaLocalization.Get(LuminaLocalizationKeys.PageEmpty),
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = LuminaPickerResources.Brush("LuminaTextMutedBrush", Brushes.Gray),
            TextWrapping = TextWrapping.Wrap
        };
    }

    private Button CreateSheetOptionButton(LuminaSettingsSelectOptionItem option)
    {
        TextBlock label = new TextBlock
        {
            Text = option.Text,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = LuminaPickerResources.Brush("LuminaTextPrimaryBrush", Brushes.White)
        };
        PathIcon checkIcon = new PathIcon
        {
            Width = 18.0,
            Height = 18.0,
            Data = StreamGeometry.Parse("M9,16.2 L4.8,12 L3.4,13.4 L9,19 L21,7 L19.6,5.6 Z"),
            IsVisible = option.IsSelected,
            Foreground = LuminaPickerResources.Brush("LuminaPrimaryBrush", Brushes.DodgerBlue),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,24"),
            ColumnSpacing = 12.0,
            Children =
            {
                label,
                checkIcon
            }
        };
        Grid.SetColumn(checkIcon, 1);
        Button button = new Button
        {
            Content = grid,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        button.Classes.Add("ActionSheetItem");
        button.Click += (_, _) => {
            SelectOption(option, closePopup: true, executeCommand: true);
        };
        return button;
    }

    private void OnSelectPopupClosed(object? sender, EventArgs e)
    {
        IsSelectPopupOpen = false;
    }

    private void OnTemplateButtonClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button { Name: "PART_SelectOptionButton", DataContext: LuminaSettingsSelectOptionItem option })
        {
            SelectOption(option, closePopup: true, executeCommand: true);
            e.Handled = true;
        }
        else if (e.Source is Button { Name: "PART_InputCancelButton" })
        {
            IsInputPopupOpen = false;
            e.Handled = true;
        }
        else if (e.Source is Button { Name: "PART_InputDoneButton" })
        {
            CommitInput(InputDraftText);
            IsInputPopupOpen = false;
            e.Handled = true;
        }
    }

    private void AttachPopupButtonHandlers()
    {
        foreach (Button button in GetPopupButtons(_selectPopup).Where(b => b.Name == "PART_SelectOptionButton"))
        {
            button.Click -= OnPopupSelectOptionButtonClick;
            button.Click += OnPopupSelectOptionButtonClick;
        }
        foreach (Button button in GetPopupButtons(_inputPopup).Where(b => b.Name is "PART_InputCancelButton" or "PART_InputDoneButton"))
        {
            button.Click -= OnPopupInputButtonClick;
            button.Click += OnPopupInputButtonClick;
        }
    }

    private static IEnumerable<Button> GetPopupButtons(Popup? popup)
    {
        return popup?.GetVisualDescendants().OfType<Button>() ?? Array.Empty<Button>();
    }

    private void OnPopupSelectOptionButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LuminaSettingsSelectOptionItem option })
        {
            SelectOption(option, closePopup: true, executeCommand: true);
            e.Handled = true;
        }
    }

    private void OnPopupInputButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Name: "PART_InputCancelButton" })
        {
            IsInputPopupOpen = false;
            e.Handled = true;
        }
        else if (sender is Button { Name: "PART_InputDoneButton" })
        {
            CommitInput(InputDraftText);
            IsInputPopupOpen = false;
            e.Handled = true;
        }
    }

    private void OnInputPopupClosed(object? sender, EventArgs e)
    {
        IsInputPopupOpen = false;
    }

    private void CommitInput(string? text)
    {
        SetCurrentValue(InputTextProperty, text);
        UpdateInputDisplayText();
    }

    private void SelectOption(LuminaSettingsSelectOptionItem option, bool closePopup, bool executeCommand)
    {
        SelectByIndex(option.Index, closePopup, executeCommand);
    }

    private void SelectByIndex(int index, bool closePopup, bool executeCommand)
    {
        if (index < 0)
        {
            SetSelection(-1, null, closePopup, executeCommand);
        }
        else if (SelectOptions.Count == 0)
        {
            UpdateSelectedDisplayText();
        }
        else if (index >= SelectOptions.Count)
        {
            SetSelection(-1, null, closePopup, executeCommand);
        }
        else
        {
            SetSelection(index, SelectOptions[index].Item, closePopup, executeCommand);
        }
    }

    private void SelectByItem(object? item, bool closePopup, bool executeCommand)
    {
        int index = -1;
        for (int i = 0; i < SelectOptions.Count; i++)
        {
            if (object.Equals(SelectOptions[i].Item, item))
            {
                index = i;
                break;
            }
        }
        SetSelection(index, item, closePopup, executeCommand);
    }

    private void SetSelection(int index, object? item, bool closePopup, bool executeCommand)
    {
        if (_isSyncingSelection)
        {
            return;
        }
        _isSyncingSelection = true;
        SetCurrentValue(SelectedIndexProperty, index);
        SetCurrentValue(SelectedItemProperty, item);
        _isSyncingSelection = false;
        SyncSelectOptionState();
        UpdateSelectedDisplayText();
        if (closePopup)
        {
            IsSelectPopupOpen = false;
            LuminaBottomSheetService.Instance.Close(this);
        }
        if (executeCommand)
        {
            ICommand? selectionChangedCommand = SelectionChangedCommand;
            if (selectionChangedCommand != null && selectionChangedCommand.CanExecute(SelectedItem))
            {
                selectionChangedCommand.Execute(SelectedItem);
            }
        }
    }

    private void RebuildSelectOptions()
    {
        if (_isSyncingOptions)
        {
            return;
        }
        _isSyncingOptions = true;
        SelectOptions.Clear();
        if (SelectItemsSource != null)
        {
            int index = 0;
            foreach (object item in SelectItemsSource)
            {
                SelectOptions.Add(new LuminaSettingsSelectOptionItem(index, item, GetItemText(item)));
                index++;
            }
        }
        _isSyncingOptions = false;
        if (SelectedIndex >= 0)
        {
            SelectByIndex(SelectedIndex, closePopup: false, executeCommand: false);
            return;
        }
        if (SelectedItem != null)
        {
            SelectByItem(SelectedItem, closePopup: false, executeCommand: false);
            return;
        }
        SyncSelectOptionState();
        UpdateSelectedDisplayText();
    }

    private void SyncSelectOptionState()
    {
        for (int i = 0; i < SelectOptions.Count; i++)
        {
            SelectOptions[i].IsSelected = i == SelectedIndex;
        }
    }

    private void UpdateSelectedDisplayText()
    {
        if (SelectedIndex >= 0 && SelectedIndex < SelectOptions.Count)
        {
            SelectedDisplayText = SelectOptions[SelectedIndex].Text;
        }
        else if (SelectedItem != null)
        {
            SelectedDisplayText = GetItemText(SelectedItem);
        }
        else
        {
            SelectedDisplayText = PlaceholderText ?? string.Empty;
        }
    }

    private void UpdateInputDisplayText()
    {
        InputDisplayText = !string.IsNullOrWhiteSpace(InputText) ? InputText : (PlaceholderText ?? string.Empty);
    }

    private void SubscribeToSelectItems(IEnumerable? itemsSource)
    {
        if (_selectItemsNotifier != null)
        {
            _selectItemsNotifier.CollectionChanged -= OnSelectItemsCollectionChanged;
        }
        _selectItemsNotifier = itemsSource as INotifyCollectionChanged;
        if (_selectItemsNotifier != null)
        {
            _selectItemsNotifier.CollectionChanged += OnSelectItemsCollectionChanged;
        }
    }

    private void OnSelectItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildSelectOptions();
    }

    private static string GetItemText(object? item)
    {
        if (item is ComboBoxItem { Content: not null } comboBoxItem)
        {
            return comboBoxItem.Content.ToString() ?? string.Empty;
        }
        return item?.ToString() ?? string.Empty;
    }
}
