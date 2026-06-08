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

    public static readonly StyledProperty<double> ExtraBottomInsetProperty =
        AvaloniaProperty.Register<LuminaInputPanel, double>(nameof(ExtraBottomInset), defaultValue: 12.0);

    private TopLevel? _topLevel;
    private IInputPane? _inputPane;
    private ScrollViewer? _scrollViewer;
    private Thickness _scrollViewerBasePadding;
    private bool _isScrollViewerPaddingApplied;
    private InputPaneState _inputPaneState = InputPaneState.Closed;
    private Rect _occludedRect;

    static LuminaInputPanel()
    {
        IsInputPaneAvoidanceEnabledProperty.Changed.AddClassHandler<LuminaInputPanel>((panel, _) =>
        {
            panel.SyncInputPaneSubscription();
            panel.UpdateScrollViewerPadding();
        });

        ExtraBottomInsetProperty.Changed.AddClassHandler<LuminaInputPanel>((panel, _) => panel.UpdateScrollViewerPadding());
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

    public double ExtraBottomInset
    {
        get => GetValue(ExtraBottomInsetProperty);
        set => SetValue(ExtraBottomInsetProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _topLevel = TopLevel.GetTopLevel(this);
        _inputPane = _topLevel?.InputPane;
        SyncInputPaneSubscription();
        UpdateInputPaneState();
        QueueScrollViewerRefresh();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        QueueScrollViewerRefresh();
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
        ResetScrollViewerPadding();
        _scrollViewer = null;
        _topLevel = null;
        _inputPane = null;
        _inputPaneState = InputPaneState.Closed;
        _occludedRect = default;

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

        UpdateScrollViewerPadding();
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        _inputPaneState = e.NewState;
        _occludedRect = e.EndRect;
        UpdateScrollViewerPadding();

        if (AutoBringFocusedElementIntoView && e.NewState == InputPaneState.Open)
        {
            QueueBringFocusedElementIntoView();
        }
    }

    private void QueueScrollViewerRefresh()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_topLevel == null)
            {
                return;
            }

            UpdateScrollViewerPadding();
        }, DispatcherPriority.Loaded);
    }

    private void QueueBringFocusedElementIntoView()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_topLevel == null)
            {
                return;
            }

            UpdateScrollViewerPadding();
            Dispatcher.UIThread.Post(BringFocusedElementIntoView, DispatcherPriority.Render);
        }, DispatcherPriority.Loaded);
    }

    private void UpdateScrollViewerPadding()
    {
        SetScrollViewer(ResolveScrollViewer());

        if (_scrollViewer == null)
        {
            return;
        }

        if (!_isScrollViewerPaddingApplied)
        {
            _scrollViewerBasePadding = _scrollViewer.Padding;
        }

        double bottomInset = ResolveBottomInset();
        if (bottomInset <= 0)
        {
            ResetScrollViewerPadding();
            return;
        }

        _scrollViewer.Padding = new Thickness(
            _scrollViewerBasePadding.Left,
            _scrollViewerBasePadding.Top,
            _scrollViewerBasePadding.Right,
            _scrollViewerBasePadding.Bottom + bottomInset);
        _isScrollViewerPaddingApplied = true;
    }

    private void SetScrollViewer(ScrollViewer? scrollViewer)
    {
        if (ReferenceEquals(_scrollViewer, scrollViewer))
        {
            return;
        }

        ResetScrollViewerPadding();
        _scrollViewer = scrollViewer;
        _scrollViewerBasePadding = scrollViewer?.Padding ?? default;
        _isScrollViewerPaddingApplied = false;
    }

    private void ResetScrollViewerPadding()
    {
        if (_scrollViewer != null && _isScrollViewerPaddingApplied)
        {
            _scrollViewer.Padding = _scrollViewerBasePadding;
            _isScrollViewerPaddingApplied = false;
        }
    }

    private double ResolveBottomInset()
    {
        if (!IsInputPaneAvoidanceEnabled ||
            _inputPaneState != InputPaneState.Open ||
            _topLevel == null ||
            _occludedRect.Height <= 0)
        {
            return 0;
        }

        double inset = Math.Max(0, _topLevel.ClientSize.Height - _occludedRect.Top);
        if (inset <= 0)
        {
            return 0;
        }

        return inset + Math.Max(0, ExtraBottomInset);
    }

    private void BringFocusedElementIntoView()
    {
        if (_topLevel?.FocusManager.GetFocusedElement() is not IInputElement focusedElement ||
            ResolveFocusedControl(focusedElement) is not Control focused ||
            !IsElementInsidePanel(focused))
        {
            return;
        }

        UpdateScrollViewerPadding();

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

        double visibleBottom = ResolveVisibleBottom(_scrollViewer, _topLevel) - Math.Max(FocusedElementBottomSpacing, ExtraBottomInset);
        double requiredDelta = focusedBottom.Value.Y - visibleBottom;
        if (requiredDelta <= 0)
        {
            focused.BringIntoView();
            return;
        }

        SetVerticalOffset(_scrollViewer, _scrollViewer.Offset.Y + requiredDelta);
    }

    private ScrollViewer? ResolveScrollViewer()
    {
        if (Content is ScrollViewer contentScrollViewer)
        {
            return contentScrollViewer;
        }

        if (Content is Visual contentVisual)
        {
            ScrollViewer? descendantScrollViewer = contentVisual.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (descendantScrollViewer != null)
            {
                return descendantScrollViewer;
            }
        }

        return this.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();
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
