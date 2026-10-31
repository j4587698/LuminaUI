using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Controls;

[PseudoClasses(":open", ":icon")]
public class LuminaPopConfirm : ContentControl
{
    public const string PART_TargetHost = "PART_TargetHost";

    public const string PART_Popup = "PART_Popup";

    /// <summary>
    /// 可选部件：默认模板不再包含关闭按钮（与底部"取消"重复）。
    /// 自定义模板若声明了同名 Button，仍按取消处理。
    /// </summary>
    public const string PART_CloseButton = "PART_CloseButton";

    public const string PART_CancelButton = "PART_CancelButton";

    public const string PART_ConfirmButton = "PART_ConfirmButton";

    public const string PC_Open = ":open";

    public const string PC_Icon = ":icon";

    private Control? _targetHost;

    private Button? _closeButton;

    private Button? _cancelButton;

    private Button? _confirmButton;

    private Control? _targetChild;

    private bool _toggleQueued;

    public static readonly StyledProperty<object?> PopupHeaderProperty;

    public static readonly StyledProperty<IDataTemplate?> PopupHeaderTemplateProperty;

    public static readonly StyledProperty<object?> PopupContentProperty;

    public static readonly StyledProperty<IDataTemplate?> PopupContentTemplateProperty;

    public static readonly StyledProperty<object?> IconProperty;

    public static readonly StyledProperty<ICommand?> ConfirmCommandProperty;

    public static readonly StyledProperty<ICommand?> CancelCommandProperty;

    public static readonly StyledProperty<object?> ConfirmCommandParameterProperty;

    public static readonly StyledProperty<object?> CancelCommandParameterProperty;

    public static readonly StyledProperty<string?> ConfirmTextProperty;

    public static readonly StyledProperty<string?> CancelTextProperty;

    public static readonly StyledProperty<bool> IsOpenProperty;

    public static readonly StyledProperty<PlacementMode> PlacementProperty;

    public static readonly StyledProperty<LuminaPopupType> PopupTypeProperty;

    public static readonly StyledProperty<LuminaPopConfirmTriggerMode> TriggerModeProperty;

    public static readonly StyledProperty<bool> CloseOnConfirmProperty;

    public static readonly StyledProperty<bool> CloseOnCancelProperty;

    public object? PopupHeader
    {
        get => GetValue(PopupHeaderProperty);
        set => SetValue(PopupHeaderProperty, value);
    }

    public IDataTemplate? PopupHeaderTemplate
    {
        get => GetValue(PopupHeaderTemplateProperty);
        set => SetValue(PopupHeaderTemplateProperty, value);
    }

    public object? PopupContent
    {
        get => GetValue(PopupContentProperty);
        set => SetValue(PopupContentProperty, value);
    }

    public IDataTemplate? PopupContentTemplate
    {
        get => GetValue(PopupContentTemplateProperty);
        set => SetValue(PopupContentTemplateProperty, value);
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public object? ConfirmCommandParameter
    {
        get => GetValue(ConfirmCommandParameterProperty);
        set => SetValue(ConfirmCommandParameterProperty, value);
    }

    public object? CancelCommandParameter
    {
        get => GetValue(CancelCommandParameterProperty);
        set => SetValue(CancelCommandParameterProperty, value);
    }

    public string? ConfirmText
    {
        get => GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    public string? CancelText
    {
        get => GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public PlacementMode Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public LuminaPopupType PopupType
    {
        get => GetValue(PopupTypeProperty);
        set => SetValue(PopupTypeProperty, value);
    }

    public LuminaPopConfirmTriggerMode TriggerMode
    {
        get => GetValue(TriggerModeProperty);
        set => SetValue(TriggerModeProperty, value);
    }

    public bool CloseOnConfirm
    {
        get => GetValue(CloseOnConfirmProperty);
        set => SetValue(CloseOnConfirmProperty, value);
    }

    public bool CloseOnCancel
    {
        get => GetValue(CloseOnCancelProperty);
        set => SetValue(CloseOnCancelProperty, value);
    }

    static LuminaPopConfirm()
    {
        PopupHeaderProperty = AvaloniaProperty.Register<LuminaPopConfirm, object?>(nameof(PopupHeader));
        PopupHeaderTemplateProperty = AvaloniaProperty.Register<LuminaPopConfirm, IDataTemplate?>(nameof(PopupHeaderTemplate));
        PopupContentProperty = AvaloniaProperty.Register<LuminaPopConfirm, object?>(nameof(PopupContent));
        PopupContentTemplateProperty = AvaloniaProperty.Register<LuminaPopConfirm, IDataTemplate?>(nameof(PopupContentTemplate));
        IconProperty = AvaloniaProperty.Register<LuminaPopConfirm, object?>(nameof(Icon));
        ConfirmCommandProperty = AvaloniaProperty.Register<LuminaPopConfirm, ICommand?>(nameof(ConfirmCommand));
        CancelCommandProperty = AvaloniaProperty.Register<LuminaPopConfirm, ICommand?>(nameof(CancelCommand));
        ConfirmCommandParameterProperty = AvaloniaProperty.Register<LuminaPopConfirm, object?>(nameof(ConfirmCommandParameter));
        CancelCommandParameterProperty = AvaloniaProperty.Register<LuminaPopConfirm, object?>(nameof(CancelCommandParameter));
        ConfirmTextProperty = AvaloniaProperty.Register<LuminaPopConfirm, string?>(nameof(ConfirmText));
        CancelTextProperty = AvaloniaProperty.Register<LuminaPopConfirm, string?>(nameof(CancelText));
        IsOpenProperty = AvaloniaProperty.Register<LuminaPopConfirm, bool>(nameof(IsOpen), defaultValue: false, inherits: false, BindingMode.TwoWay);
        PlacementProperty = Popup.PlacementProperty.AddOwner<LuminaPopConfirm>();
        PopupTypeProperty = AvaloniaProperty.Register<LuminaPopConfirm, LuminaPopupType>(nameof(PopupType), LuminaPopupType.Auto);
        TriggerModeProperty = AvaloniaProperty.Register<LuminaPopConfirm, LuminaPopConfirmTriggerMode>(nameof(TriggerMode), LuminaPopConfirmTriggerMode.Click);
        CloseOnConfirmProperty = AvaloniaProperty.Register<LuminaPopConfirm, bool>(nameof(CloseOnConfirm), defaultValue: true);
        CloseOnCancelProperty = AvaloniaProperty.Register<LuminaPopConfirm, bool>(nameof(CloseOnCancel), defaultValue: true);
        IsOpenProperty.Changed.AddClassHandler((LuminaPopConfirm control, AvaloniaPropertyChangedEventArgs<bool> args) =>
        {
            control.PseudoClasses.Set(":open", args.GetNewValue<bool>());
        });
        IconProperty.Changed.AddClassHandler((LuminaPopConfirm control, AvaloniaPropertyChangedEventArgs<object?> args) =>
        {
            control.PseudoClasses.Set(":icon", args.GetNewValue<object>() != null);
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
        UpdateDefaultButtonText();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LuminaLocalization.LanguageChanged -= OnLanguageChanged;
        DetachTemplateHandlers();
        DetachTargetChild();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        DetachTemplateHandlers();
        base.OnApplyTemplate(e);
        _targetHost = e.NameScope.Find<Control>("PART_TargetHost");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");
        _cancelButton = e.NameScope.Find<Button>("PART_CancelButton");
        _confirmButton = e.NameScope.Find<Button>("PART_ConfirmButton");
        AttachTemplateHandlers();
        SetTargetChild(Content as Control);
        UpdateDefaultButtonText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentControl.ContentProperty)
        {
            SetTargetChild(change.GetNewValue<object>() as Control);
        }
        else
        {
            if (change.Property != IsOpenProperty || !change.GetNewValue<bool>() || !ShouldUseSheet())
            {
                return;
            }
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (IsOpen && ShouldUseSheet() && TryShowSheet())
                {
                    SetCurrentValue(IsOpenProperty, value: false);
                }
            }, DispatcherPriority.Background);
        }
    }

    private void AttachTemplateHandlers()
    {
        if (_targetHost != null)
        {
            _targetHost.AddHandler(InputElement.PointerReleasedEvent, OnTargetPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            _targetHost.GotFocus += OnTargetGotFocus;
            _targetHost.LostFocus += OnTargetLostFocus;
        }
        if (_closeButton != null)
        {
            _closeButton.Click += OnCancelClick;
        }
        if (_cancelButton != null)
        {
            _cancelButton.Click += OnCancelClick;
        }
        if (_confirmButton != null)
        {
            _confirmButton.Click += OnConfirmClick;
        }
    }

    private void DetachTemplateHandlers()
    {
        if (_targetHost != null)
        {
            _targetHost.RemoveHandler(InputElement.PointerReleasedEvent, OnTargetPointerReleased);
            _targetHost.GotFocus -= OnTargetGotFocus;
            _targetHost.LostFocus -= OnTargetLostFocus;
        }
        if (_closeButton != null)
        {
            _closeButton.Click -= OnCancelClick;
        }
        if (_cancelButton != null)
        {
            _cancelButton.Click -= OnCancelClick;
        }
        if (_confirmButton != null)
        {
            _confirmButton.Click -= OnConfirmClick;
        }
        _targetHost = null;
        _closeButton = null;
        _cancelButton = null;
        _confirmButton = null;
    }

    private void OnTargetPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (TriggerMode.HasFlag(LuminaPopConfirmTriggerMode.Click))
        {
            e.Handled = true;
            QueueToggleOpen();
        }
    }

    private void OnTargetButtonClick(object? sender, RoutedEventArgs e)
    {
        if (TriggerMode.HasFlag(LuminaPopConfirmTriggerMode.Click))
        {
            e.Handled = true;
            QueueToggleOpen();
        }
    }

    private void QueueToggleOpen()
    {
        if (_toggleQueued)
        {
            return;
        }
        _toggleQueued = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            _toggleQueued = false;
            if (!IsOpen && ShouldUseSheet() && TryShowSheet())
            {
                SetCurrentValue(IsOpenProperty, value: false);
            }
            else
            {
                SetCurrentValue(IsOpenProperty, !IsOpen);
            }
        }, DispatcherPriority.Background);
    }

    private void SetTargetChild(Control? child)
    {
        if (_targetChild != child)
        {
            DetachTargetChild();
            _targetChild = child;
            AttachTargetChild();
        }
    }

    private void AttachTargetChild()
    {
        if (_targetChild != null)
        {
            if (_targetChild is Button button)
            {
                button.Click += OnTargetButtonClick;
            }
            else
            {
                _targetChild.AddHandler(InputElement.PointerReleasedEvent, OnTargetPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            }
        }
    }

    private void DetachTargetChild()
    {
        if (_targetChild != null)
        {
            if (_targetChild is Button button)
            {
                button.Click -= OnTargetButtonClick;
            }
            else
            {
                _targetChild.RemoveHandler(InputElement.PointerReleasedEvent, OnTargetPointerReleased);
            }
            _targetChild = null;
        }
    }

    private void OnTargetGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (TriggerMode.HasFlag(LuminaPopConfirmTriggerMode.Focus))
        {
            if (ShouldUseSheet() && TryShowSheet())
            {
                SetCurrentValue(IsOpenProperty, value: false);
            }
            else
            {
                SetCurrentValue(IsOpenProperty, value: true);
            }
        }
    }

    private void OnTargetLostFocus(object? sender, RoutedEventArgs e)
    {
        if (TriggerMode.HasFlag(LuminaPopConfirmTriggerMode.Focus))
        {
            IInputElement? focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focused is not ILogical logical || !logical.GetLogicalAncestors().Any(ancestor => ancestor == this))
            {
                SetCurrentValue(IsOpenProperty, value: false);
            }
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (CloseOnCancel)
        {
            SetCurrentValue(IsOpenProperty, value: false);
            LuminaBottomSheetService.Instance.Close(this);
        }
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (CloseOnConfirm)
        {
            SetCurrentValue(IsOpenProperty, value: false);
            LuminaBottomSheetService.Instance.Close(this);
        }
    }

    private bool TryShowSheet()
    {
        Control content = CreateSheetContent();
        return LuminaBottomSheetService.Instance.TryShow(this, content);
    }

    private Control CreateSheetContent()
    {
        Grid message = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
        };
        LuminaPickerResources.BindResource(message, Grid.ColumnSpacingProperty, "LuminaPopConfirmSheetMessageColumnSpacing");
        message.Children.Add(CreateSheetIcon());
        StackPanel textPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        LuminaPickerResources.BindResource(textPanel, StackPanel.SpacingProperty, "LuminaPopConfirmSheetContentSpacing");
        Grid.SetColumn(textPanel, 1);
        if (PopupHeader != null)
        {
            textPanel.Children.Add(CreateSheetContentPresenter(PopupHeader, PopupHeaderTemplate, isHeader: true));
        }
        if (PopupContent != null)
        {
            textPanel.Children.Add(CreateSheetContentPresenter(PopupContent, PopupContentTemplate, isHeader: false));
        }
        message.Children.Add(textPanel);
        Button cancelButton = new Button
        {
            Content = CancelText,
            Command = CancelCommand,
            CommandParameter = CancelCommandParameter,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        cancelButton.Classes.Add("Outline");
        cancelButton.Click += OnCancelClick;
        Button confirmButton = new Button
        {
            Content = ConfirmText,
            Command = ConfirmCommand,
            CommandParameter = ConfirmCommandParameter,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        confirmButton.Classes.Add(Classes.Contains("Danger") ? "Danger" : "Primary");
        confirmButton.Click += OnConfirmClick;
        StackPanel actions = new StackPanel();
        LuminaPickerResources.BindResource(actions, StackPanel.SpacingProperty, "LuminaPopConfirmSheetActionsSpacing");
        actions.Children.Add(confirmButton);
        actions.Children.Add(cancelButton);
        StackPanel layout = new StackPanel();
        LuminaPickerResources.BindResource(layout, StackPanel.SpacingProperty, "LuminaPopConfirmSheetLayoutSpacing");
        layout.Children.Add(message);
        layout.Children.Add(actions);
        return layout;
    }

    private Control CreateSheetIcon()
    {
        // 与弹层模板一致：浅色圆底徽章 + 实心圆镂空感叹号，相对文字上下居中。
        // 颜色走 DynamicResource：Application.TryFindResource 看不到主题变体里的画刷，
        // 之前会落到硬编码的 OrangeRed 等兜底色，而且切换主题也不跟着变。
        (string iconBrushKey, string badgeBrushKey) = Classes.Contains("Danger")
            ? ("LuminaDangerBrush", "LuminaDangerBgBrush")
            : Classes.Contains("Success")
                ? ("LuminaSuccessBrush", "LuminaSuccessBgBrush")
                : Classes.Contains("Info")
                    ? ("LuminaPrimaryBrush", "LuminaPrimaryBgBrush")
                    : ("LuminaWarningBrush", "LuminaWarningBgBrush");

        Control glyph;
        if (Icon != null)
        {
            glyph = new ContentControl
            {
                Content = Icon,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            LuminaPickerResources.BindResource(glyph, Layoutable.WidthProperty, "LuminaPopConfirmSheetCustomIconSize");
            LuminaPickerResources.BindResource(glyph, Layoutable.HeightProperty, "LuminaPopConfirmSheetCustomIconSize");
        }
        else
        {
            PathIcon icon = new PathIcon
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            LuminaPickerResources.BindResource(icon, PathIcon.DataProperty, "LuminaPopConfirmDefaultIconGeometry");
            LuminaPickerResources.BindResource(icon, Layoutable.WidthProperty, "LuminaPopConfirmSheetDefaultIconSize");
            LuminaPickerResources.BindResource(icon, Layoutable.HeightProperty, "LuminaPopConfirmSheetDefaultIconSize");
            LuminaPickerResources.BindBrush(icon, TemplatedControl.ForegroundProperty, iconBrushKey);
            glyph = icon;
        }

        Border badge = new Border
        {
            Child = glyph,
            VerticalAlignment = VerticalAlignment.Center
        };
        LuminaPickerResources.BindResource(badge, Layoutable.WidthProperty, "LuminaPopConfirmSheetIconHostSize");
        LuminaPickerResources.BindResource(badge, Layoutable.HeightProperty, "LuminaPopConfirmSheetIconHostSize");
        LuminaPickerResources.BindResource(badge, Border.CornerRadiusProperty, "LuminaPopConfirmSheetIconHostCornerRadius");
        if (Icon == null)
        {
            LuminaPickerResources.BindBrush(badge, Border.BackgroundProperty, badgeBrushKey);
        }
        return badge;
    }

    private static Control CreateSheetContentPresenter(object? content, IDataTemplate? template, bool isHeader)
    {
        if (content is string text)
        {
            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = isHeader
                    ? LuminaPickerResources.Double("LuminaPopConfirmSheetHeaderFontSize", 18.0)
                    : LuminaPickerResources.Double("LuminaPopConfirmSheetContentFontSize", 14.0),
                FontWeight = (isHeader ? FontWeight.DemiBold : FontWeight.Normal)
            };
        }
        return new ContentControl
        {
            Content = content,
            ContentTemplate = template,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }

    private bool ShouldUseSheet()
    {
        return LuminaSheetPlacement.ShouldUseSheet(GetEffectivePopupType());
    }

    private LuminaPopupType GetEffectivePopupType()
    {
        return IsSet(PopupTypeProperty) ? PopupType : LuminaOptions.GetPopupType(this);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateDefaultButtonText();
    }

    private void UpdateDefaultButtonText()
    {
        if (!IsSet(CancelTextProperty))
        {
            SetCurrentValue(CancelTextProperty, LuminaLocalization.Get("Lumina.Common.Cancel"));
        }
        if (!IsSet(ConfirmTextProperty))
        {
            SetCurrentValue(ConfirmTextProperty, LuminaLocalization.Get("Lumina.Common.Confirm"));
        }
    }
}
