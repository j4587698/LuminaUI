using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

[TemplatePart("PART_CloseButton", typeof(Button))]
[TemplatePart("PART_MaximizeButton", typeof(Button))]
[TemplatePart("PART_MinimizeButton", typeof(Button))]
[TemplatePart("PART_FullScreenButton", typeof(Button))]
[TemplatePart("PART_PinButton", typeof(Button))]
[TemplatePart("PART_DragArea", typeof(Control))]
public class LuminaTitleBar : ContentControl
{
    private readonly List<Button> _decorationButtons = new List<Button>();

    private Button? _pinButton;

    private Control? _dragArea;

    private WindowState _windowStateBeforeFullScreen = WindowState.Normal;

    public static readonly StyledProperty<object?> LeftContentProperty = AvaloniaProperty.Register<LuminaTitleBar, object?>(nameof(LeftContent));

    public static readonly StyledProperty<object?> RightContentProperty = AvaloniaProperty.Register<LuminaTitleBar, object?>(nameof(RightContent));

    public static readonly StyledProperty<Thickness> LeftContentMarginProperty = AvaloniaProperty.Register<LuminaTitleBar, Thickness>(nameof(LeftContentMargin), new Thickness(16.0, 0.0, 16.0, 0.0));

    public static readonly StyledProperty<bool> ShowSystemButtonsProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(ShowSystemButtons), defaultValue: true);

    public static readonly StyledProperty<bool> ShowFullScreenButtonProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(ShowFullScreenButton), defaultValue: false);

    public static readonly StyledProperty<bool> ShowPinButtonProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(ShowPinButton), defaultValue: false);

    public static readonly StyledProperty<bool> ShowMinimizeButtonProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(ShowMinimizeButton), defaultValue: true);

    public static readonly StyledProperty<bool> ShowMaximizeButtonProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(ShowMaximizeButton), defaultValue: true);

    public static readonly StyledProperty<bool> ShowCloseButtonProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(ShowCloseButton), defaultValue: true);

    public static readonly StyledProperty<bool> IsFullScreenProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(IsFullScreen), defaultValue: false);

    public static readonly StyledProperty<bool> IsPinnedProperty = AvaloniaProperty.Register<LuminaTitleBar, bool>(nameof(IsPinned), defaultValue: false);

    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public Thickness LeftContentMargin
    {
        get => GetValue(LeftContentMarginProperty);
        set => SetValue(LeftContentMarginProperty, value);
    }

    public bool ShowSystemButtons
    {
        get => GetValue(ShowSystemButtonsProperty);
        set => SetValue(ShowSystemButtonsProperty, value);
    }

    public bool ShowFullScreenButton
    {
        get => GetValue(ShowFullScreenButtonProperty);
        set => SetValue(ShowFullScreenButtonProperty, value);
    }

    public bool ShowPinButton
    {
        get => GetValue(ShowPinButtonProperty);
        set => SetValue(ShowPinButtonProperty, value);
    }

    public bool ShowMinimizeButton
    {
        get => GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    public bool IsFullScreen
    {
        get => GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }

    public bool IsPinned
    {
        get => GetValue(IsPinnedProperty);
        set => SetValue(IsPinnedProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DetachButtonHandlers();
        TrackDecorationButton(e.NameScope.FindRequired<Button>("PART_CloseButton"));
        TrackDecorationButton(e.NameScope.FindRequired<Button>("PART_FullScreenButton"));
        TrackDecorationButton(e.NameScope.FindRequired<Button>("PART_MaximizeButton"));
        TrackDecorationButton(e.NameScope.FindRequired<Button>("PART_MinimizeButton"));
        _pinButton = e.NameScope.FindRequired<Button>("PART_PinButton");
        _dragArea = e.NameScope.Find<Control>("PART_DragArea");
        AttachButtonHandlers();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachButtonHandlers();
        base.OnDetachedFromVisualTree(e);
    }

    private void AttachButtonHandlers()
    {
        foreach (Button button in _decorationButtons)
        {
            button.Click += OnDecorationButtonClick;
        }
        if (_pinButton != null)
        {
            _pinButton.Click += OnPinButtonClick;
        }
        if (_dragArea != null)
        {
            _dragArea.PointerPressed += OnDragAreaPointerPressed;
            _dragArea.DoubleTapped += OnDragAreaDoubleTapped;
        }
    }

    private void DetachButtonHandlers()
    {
        foreach (Button button in _decorationButtons)
        {
            button.Click -= OnDecorationButtonClick;
        }
        _decorationButtons.Clear();
        if (_pinButton != null)
        {
            _pinButton.Click -= OnPinButtonClick;
        }
        _pinButton = null;
        if (_dragArea != null)
        {
            _dragArea.PointerPressed -= OnDragAreaPointerPressed;
            _dragArea.DoubleTapped -= OnDragAreaDoubleTapped;
        }
        _dragArea = null;
    }

    private void TrackDecorationButton(Button? button)
    {
        if (button != null)
        {
            _decorationButtons.Add(button);
        }
    }

    private void OnDecorationButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || TopLevel.GetTopLevel(this) is not Window window)
        {
            return;
        }
        switch (WindowDecorationProperties.GetElementRole(button))
        {
        case WindowDecorationsElementRole.CloseButton:
            window.Close();
            e.Handled = true;
            break;
        case WindowDecorationsElementRole.MinimizeButton:
            if (window.CanMinimize)
            {
                window.WindowState = WindowState.Minimized;
                e.Handled = true;
            }
            break;
        case WindowDecorationsElementRole.MaximizeButton:
            if (window.CanMaximize)
            {
                window.WindowState = (window.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal;
                e.Handled = true;
            }
            break;
        case WindowDecorationsElementRole.FullScreenButton:
            ToggleFullScreen(window);
            e.Handled = true;
            break;
        }
    }

    private void ToggleFullScreen(Window window)
    {
        if (window.WindowState == WindowState.FullScreen)
        {
            window.WindowState = _windowStateBeforeFullScreen;
            return;
        }
        _windowStateBeforeFullScreen = (window.WindowState != WindowState.Minimized) ? window.WindowState : WindowState.Normal;
        window.WindowState = WindowState.FullScreen;
    }

    private void OnPinButtonClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Topmost = !window.Topmost;
            IsPinned = window.Topmost;
            e.Handled = true;
        }
    }

    private void OnDragAreaPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.ClickCount <= 1 && TopLevel.GetTopLevel(this) is Window { WindowState: not WindowState.FullScreen } window)
        {
            window.BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private void OnDragAreaDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window { CanMaximize: not false, WindowState: not WindowState.FullScreen } window)
        {
            window.WindowState = (window.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal;
            e.Handled = true;
        }
    }
}
