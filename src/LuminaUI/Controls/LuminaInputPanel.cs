using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

public class LuminaInputPanel : ContentControl
{
    public static readonly StyledProperty<bool> IsInputPaneAvoidanceEnabledProperty =
        AvaloniaProperty.Register<LuminaInputPanel, bool>(nameof(IsInputPaneAvoidanceEnabled), defaultValue: true);

    public static readonly StyledProperty<bool> AutoBringFocusedElementIntoViewProperty =
        AvaloniaProperty.Register<LuminaInputPanel, bool>(nameof(AutoBringFocusedElementIntoView), defaultValue: true);

    private TopLevel? _topLevel;
    private IInputPane? _inputPane;
    private ScrollViewer? _scrollViewer;
    private Border? _rootBorder;
    private Thickness _rootBorderBasePadding;
    private bool _isRootBorderPaddingApplied;
    private InputPaneState _inputPaneState = InputPaneState.Closed;
    private Rect _occludedRect;

    static LuminaInputPanel()
    {
        IsInputPaneAvoidanceEnabledProperty.Changed.AddClassHandler<LuminaInputPanel>((panel, _) =>
        {
            panel.SyncInputPaneSubscription();
        });
    }

    public bool IsInputPaneAvoidanceEnabled
    {
        get => GetValue(IsInputPaneAvoidanceEnabledProperty);
        set => SetValue(IsInputPaneAvoidanceEnabledProperty, value);
    }

    public bool AutoBringFocusedElementIntoView
    {
        get => GetValue(AutoBringFocusedElementIntoViewProperty);
        set => SetValue(AutoBringFocusedElementIntoViewProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _topLevel = TopLevel.GetTopLevel(this);
        _inputPane = _topLevel?.InputPane;
        SyncInputPaneSubscription();
        UpdateInputPaneState();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _rootBorder = e.NameScope.Find<Border>("PART_RootBorder");
        _rootBorderBasePadding = _rootBorder?.Padding ?? default;
        _isRootBorderPaddingApplied = false;
        System.Diagnostics.Debug.WriteLine($"[LuminaInputPanel] OnApplyTemplate: _rootBorderBasePadding={_rootBorderBasePadding}");
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        if (AutoBringFocusedElementIntoView && _inputPaneState == InputPaneState.Open)
        {
            Dispatcher.UIThread.Post(BringFocusedElementIntoView, DispatcherPriority.Render);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachInputPane();
        _scrollViewer = null;
        _rootBorder = null;
        _topLevel = null;
        _inputPane = null;
        _inputPaneState = InputPaneState.Closed;
        _occludedRect = default;
        _isRootBorderPaddingApplied = false;

        base.OnDetachedFromVisualTree(e);
    }

    private void SyncInputPaneSubscription()
    {
        DetachInputPane();

        if (!IsInputPaneAvoidanceEnabled || _inputPane == null)
        {
            _inputPaneState = InputPaneState.Closed;
            _occludedRect = default;
            return;
        }

        _inputPane.StateChanged += OnInputPaneStateChanged;
    }

    private void DetachInputPane()
    {
        if (_inputPane != null)
        {
            _inputPane.StateChanged -= OnInputPaneStateChanged;
        }
    }

    private void UpdateInputPaneState()
    {
        if (IsInputPaneAvoidanceEnabled && _inputPane != null)
        {
            _inputPaneState = _inputPane.State;
            _occludedRect = _inputPane.OccludedRect;
        }
        else
        {
            _inputPaneState = InputPaneState.Closed;
            _occludedRect = default;
        }
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        _inputPaneState = e.NewState;
        _occludedRect = e.EndRect;
        System.Diagnostics.Debug.WriteLine($"[LuminaInputPanel] StateChanged: NewState={e.NewState}, EndRect={e.EndRect}");

        UpdateBorderPadding();

        if (AutoBringFocusedElementIntoView && e.NewState == InputPaneState.Open)
        {
            // 延迟一下，等布局完成后再滚动
            Dispatcher.UIThread.Post(() =>
            {
                Dispatcher.UIThread.Post(BringFocusedElementIntoView, DispatcherPriority.Render);
            }, DispatcherPriority.Loaded);
        }
    }

    private void UpdateBorderPadding()
    {
        if (_rootBorder == null || _topLevel == null)
        {
            return;
        }

        if (_inputPaneState != InputPaneState.Open || _occludedRect.Height <= 0)
        {
            // 键盘关闭，恢复原始 Padding
            if (_isRootBorderPaddingApplied)
            {
                _rootBorder.Padding = _rootBorderBasePadding;
                _isRootBorderPaddingApplied = false;
            }
            return;
        }

        // 键盘打开，直接设置底部 Padding 为键盘高度
        _rootBorder.Padding = new Thickness(
            _rootBorderBasePadding.Left,
            _rootBorderBasePadding.Top,
            _rootBorderBasePadding.Right,
            _occludedRect.Height);
        _isRootBorderPaddingApplied = true;
        System.Diagnostics.Debug.WriteLine($"[LuminaInputPanel] Border Padding={_rootBorder.Padding}, _occludedRect.Height={_occludedRect.Height}");
    }

    private void BringFocusedElementIntoView()
    {
        if (_topLevel?.FocusManager.GetFocusedElement() is not IInputElement focusedElement ||
            ResolveFocusedControl(focusedElement) is not Control focused ||
            !IsElementInsidePanel(focused))
        {
            return;
        }

        if (_inputPaneState != InputPaneState.Open ||
            _topLevel == null ||
            _scrollViewer == null ||
            _occludedRect.Height <= 0)
        {
            focused.BringIntoView();
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[LuminaInputPanel] BringIntoView: calling focused.BringIntoView()");
        focused.BringIntoView();
    }

    private static Control? ResolveFocusedControl(IInputElement focusedElement)
    {
        if (focusedElement is Control control)
        {
            return control;
        }

        return (focusedElement as Visual)?.GetVisualAncestors().OfType<Control>().FirstOrDefault();
    }

    private bool IsElementInsidePanel(Control control)
    {
        return ReferenceEquals(control, this) || control.GetVisualAncestors().Any(ancestor => ReferenceEquals(ancestor, this));
    }

    private static void SetVerticalOffset(ScrollViewer scrollViewer, double y)
    {
        double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Clamp(y, 0, maxY));
    }
}
