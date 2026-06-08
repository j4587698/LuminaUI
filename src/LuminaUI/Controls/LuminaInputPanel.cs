using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
public class LuminaInputPanel : ContentControl
{
    public static readonly StyledProperty<bool> IsInputPaneAvoidanceEnabledProperty =
        AvaloniaProperty.Register<LuminaInputPanel, bool>(nameof(IsInputPaneAvoidanceEnabled), defaultValue: true);

    public static readonly StyledProperty<bool> AutoBringFocusedElementIntoViewProperty =
        AvaloniaProperty.Register<LuminaInputPanel, bool>(nameof(AutoBringFocusedElementIntoView), defaultValue: true);

    public static readonly StyledProperty<double> ExtraBottomInsetProperty =
        AvaloniaProperty.Register<LuminaInputPanel, double>(nameof(ExtraBottomInset), defaultValue: 12.0);

    public static readonly StyledProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
        ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner<LuminaInputPanel>();

    public static readonly StyledProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
        ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner<LuminaInputPanel>();

    public static readonly DirectProperty<LuminaInputPanel, Thickness> EffectivePaddingProperty =
        AvaloniaProperty.RegisterDirect<LuminaInputPanel, Thickness>(
            nameof(EffectivePadding),
            panel => panel.EffectivePadding);

    private TopLevel? _topLevel;
    private IInputPane? _inputPane;
    private Thickness _effectivePadding;
    private InputPaneState _inputPaneState = InputPaneState.Closed;
    private Rect _occludedRect;

    static LuminaInputPanel()
    {
        IsInputPaneAvoidanceEnabledProperty.Changed.AddClassHandler<LuminaInputPanel>((panel, _) =>
        {
            panel.SyncInputPaneSubscription();
            panel.UpdateEffectivePadding();
        });

        ExtraBottomInsetProperty.Changed.AddClassHandler<LuminaInputPanel>((panel, _) => panel.UpdateEffectivePadding());
        PaddingProperty.Changed.AddClassHandler<LuminaInputPanel>((panel, _) => panel.UpdateEffectivePadding());
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

    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    public Thickness EffectivePadding
    {
        get => _effectivePadding;
        private set => SetAndRaise(EffectivePaddingProperty, ref _effectivePadding, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _topLevel = TopLevel.GetTopLevel(this);
        _inputPane = _topLevel?.InputPane;
        SyncInputPaneSubscription();
        UpdateInputPaneState();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachInputPane();
        _topLevel = null;
        _inputPane = null;
        _inputPaneState = InputPaneState.Closed;
        _occludedRect = default;
        UpdateEffectivePadding();

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

        UpdateEffectivePadding();
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        _inputPaneState = e.NewState;
        _occludedRect = e.EndRect;
        UpdateEffectivePadding();

        if (AutoBringFocusedElementIntoView && e.NewState == InputPaneState.Open)
        {
            Dispatcher.UIThread.Post(BringFocusedElementIntoView, DispatcherPriority.Loaded);
        }
    }

    private void UpdateEffectivePadding()
    {
        Thickness padding = Padding;
        double bottomInset = ResolveBottomInset();

        EffectivePadding = new Thickness(
            padding.Left,
            padding.Top,
            padding.Right,
            padding.Bottom + bottomInset);
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
        if (_topLevel?.FocusManager.GetFocusedElement() is not Control focused ||
            !IsElementInsidePanel(focused))
        {
            return;
        }

        focused.BringIntoView();
    }

    private bool IsElementInsidePanel(Control control)
    {
        return ReferenceEquals(control, this) || control.GetVisualAncestors().Any(ancestor => ReferenceEquals(ancestor, this));
    }
}
