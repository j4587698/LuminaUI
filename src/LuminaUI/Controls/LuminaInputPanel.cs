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
    private const double FocusedElementBottomSpacing = 8.0;

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
            panel.UpdateInputPaneState();
            panel.UpdateBorderPadding();
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
        UpdateBorderPadding();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ResetBorderPadding();
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _rootBorder = e.NameScope.Find<Border>("PART_RootBorder");
        _rootBorderBasePadding = _rootBorder?.Padding ?? default;
        _isRootBorderPaddingApplied = false;
        UpdateBorderPadding();
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        if (AutoBringFocusedElementIntoView && _inputPaneState == InputPaneState.Open)
        {
            QueueBringFocusedElementIntoView();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachInputPane();
        ResetBorderPadding();
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

        UpdateBorderPadding();

        if (AutoBringFocusedElementIntoView && e.NewState == InputPaneState.Open)
        {
            QueueBringFocusedElementIntoView();
        }
    }

    private void QueueBringFocusedElementIntoView()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_topLevel == null)
            {
                return;
            }

            UpdateInputPaneState();
            UpdateBorderPadding();
            Dispatcher.UIThread.Post(BringFocusedElementIntoView, DispatcherPriority.Render);
        }, DispatcherPriority.Loaded);
    }

    private void UpdateBorderPadding()
    {
        if (_rootBorder == null || _topLevel == null)
        {
            return;
        }

        if (_inputPaneState != InputPaneState.Open || _occludedRect.Height <= 0)
        {
            ResetBorderPadding();
            return;
        }

        double bottomInset = ResolveBottomInset();
        if (bottomInset <= 0)
        {
            ResetBorderPadding();
            return;
        }

        _rootBorder.Padding = new Thickness(
            _rootBorderBasePadding.Left,
            _rootBorderBasePadding.Top,
            _rootBorderBasePadding.Right,
            _rootBorderBasePadding.Bottom + bottomInset);
        _isRootBorderPaddingApplied = true;
    }

    private void ResetBorderPadding()
    {
        if (_rootBorder != null && _isRootBorderPaddingApplied)
        {
            _rootBorder.Padding = _rootBorderBasePadding;
            _isRootBorderPaddingApplied = false;
        }
    }

    private double ResolveBottomInset()
    {
        if (_topLevel == null || _occludedRect.Height <= 0)
        {
            return 0;
        }

        return Math.Max(0, _topLevel.ClientSize.Height - _occludedRect.Top);
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

        Point? focusedBottom = focused.TranslatePoint(new Point(0, focused.Bounds.Height), _topLevel);
        if (!focusedBottom.HasValue)
        {
            focused.BringIntoView();
            return;
        }

        double visibleBottom = ResolveVisibleBottom(_scrollViewer, _topLevel) - FocusedElementBottomSpacing;
        double requiredDelta = focusedBottom.Value.Y - visibleBottom;
        if (requiredDelta <= 0)
        {
            focused.BringIntoView();
            return;
        }

        SetVerticalOffset(_scrollViewer, _scrollViewer.Offset.Y + requiredDelta);
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

    private double ResolveVisibleBottom(ScrollViewer scrollViewer, TopLevel topLevel)
    {
        Point? scrollViewerBottom = scrollViewer.TranslatePoint(new Point(0, scrollViewer.Bounds.Height), topLevel);
        if (!scrollViewerBottom.HasValue)
        {
            return _occludedRect.Top;
        }

        return Math.Min(scrollViewerBottom.Value.Y, _occludedRect.Top);
    }

    private static void SetVerticalOffset(ScrollViewer scrollViewer, double y)
    {
        double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Clamp(y, 0, maxY));
    }
}
