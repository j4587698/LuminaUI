using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

[TemplatePart("PART_DragHandle", typeof(Control))]
public class LuminaBottomSheet : ContentControl
{
    private Control? _dragHandle;

    private bool _isDragging;

    private Point _startPoint;

    private double _startOffsetY;

    private TranslateTransform? _translateTransform;

    private Thickness _effectiveContentPadding = new Thickness(24.0, 0.0, 24.0, 24.0);

    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<LuminaBottomSheet, Thickness>(nameof(ContentPadding), new Thickness(24.0, 0.0, 24.0, 24.0));

    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty = AvaloniaProperty.Register<LuminaBottomSheet, Thickness>(nameof(SafeAreaPadding));

    public static readonly DirectProperty<LuminaBottomSheet, Thickness> EffectiveContentPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaBottomSheet, Thickness>(nameof(EffectiveContentPadding), (LuminaBottomSheet sheet) => sheet.EffectiveContentPadding);

    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public Thickness SafeAreaPadding
    {
        get => GetValue(SafeAreaPaddingProperty);
        set => SetValue(SafeAreaPaddingProperty, value);
    }

    public Thickness EffectiveContentPadding
    {
        get
        {
            return _effectiveContentPadding;
        }
        private set
        {
            SetAndRaise(EffectiveContentPaddingProperty, ref _effectiveContentPadding, value);
        }
    }

    internal static LuminaBottomSheet? EnsureSheet(object? content)
    {
        if (content == null)
        {
            return null;
        }

        return content as LuminaBottomSheet ?? new LuminaBottomSheet
        {
            Content = content
        };
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_dragHandle != null)
        {
            _dragHandle.PointerPressed -= OnDragHandlePointerPressed;
            _dragHandle.PointerMoved -= OnDragHandlePointerMoved;
            _dragHandle.PointerReleased -= OnDragHandlePointerReleased;
            _dragHandle.PointerCaptureLost -= OnDragHandlePointerCaptureLost;
        }
        _dragHandle = e.NameScope.FindRequired<Control>("PART_DragHandle");
        if (_dragHandle != null)
        {
            _dragHandle.PointerPressed += OnDragHandlePointerPressed;
            _dragHandle.PointerMoved += OnDragHandlePointerMoved;
            _dragHandle.PointerReleased += OnDragHandlePointerReleased;
            _dragHandle.PointerCaptureLost += OnDragHandlePointerCaptureLost;
        }
        if (RenderTransform is not TranslateTransform)
        {
            _translateTransform = new TranslateTransform();
            RenderTransform = _translateTransform;
        }
        else
        {
            _translateTransform = (TranslateTransform)RenderTransform;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentPaddingProperty || change.Property == SafeAreaPaddingProperty)
        {
            UpdateEffectiveContentPadding();
        }
    }

    private void UpdateEffectiveContentPadding()
    {
        Thickness padding = ContentPadding;
        Thickness safeAreaPadding = SafeAreaPadding;
        EffectiveContentPadding = new Thickness(
            padding.Left + safeAreaPadding.Left,
            padding.Top,
            padding.Right + safeAreaPadding.Right,
            padding.Bottom + safeAreaPadding.Bottom);
    }

    private void OnDragHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _startPoint = e.GetPosition(null);
            _startOffsetY = _translateTransform?.Y ?? 0.0;
            e.Pointer.Capture(_dragHandle);
            e.Handled = true;
        }
    }

    private void OnDragHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && _translateTransform != null)
        {
            double deltaY = e.GetPosition(null).Y - _startPoint.Y;
            double newY = _startOffsetY + deltaY;
            _translateTransform.Y = Math.Max(0.0, newY);
        }
    }

    private void OnDragHandlePointerReleased(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            HandleRelease();
        }
    }

    private void OnDragHandlePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            HandleRelease();
        }
    }

    private void HandleRelease()
    {
        if (_translateTransform != null)
        {
            if (_translateTransform.Y > 100.0)
            {
                Dismiss();
            }
            else
            {
                _translateTransform.Y = 0.0;
            }
        }
    }

    private void Dismiss()
    {
        LuminaShell? shell = this.GetVisualAncestors().OfType<LuminaShell>().FirstOrDefault();
        if (shell != null)
        {
            shell.CloseBottomSheet();
            ResetOffset();
            return;
        }
        LuminaOverlayHost? overlayHost = this.GetVisualAncestors().OfType<LuminaOverlayHost>().FirstOrDefault();
        if (overlayHost != null)
        {
            overlayHost.CloseBottomSheet();
            ResetOffset();
            return;
        }

        LuminaTopView? topView = this.GetVisualAncestors().OfType<LuminaTopView>().FirstOrDefault();
        if (topView != null)
        {
            topView.CloseBottomSheet();
            ResetOffset();
        }
    }

    private void ResetOffset()
    {
        if (_translateTransform != null)
        {
            _translateTransform.Y = 0.0;
        }
    }
}
