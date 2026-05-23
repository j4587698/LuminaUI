using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using LuminaUI.Controls;
using LuminaUI.Localization;
using LuminaUI.Services;
using AvaloniaColorPicker = Avalonia.Controls.ColorPicker;
using AvaloniaColorView = Avalonia.Controls.ColorView;

namespace LuminaUI.ColorPicker;

internal static class LuminaColorPickerSheetBehavior
{
    private static readonly AttachedProperty<bool> IsSheetBehaviorAttachedProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaColorPicker, AvaloniaColorPicker, bool>(
            "IsSheetBehaviorAttached");

    private static bool s_initialized;

    public static void EnsureInitialized()
    {
        if (s_initialized)
        {
            return;
        }

        s_initialized = true;
        LuminaOptions.PopupTypeProperty.Changed.AddClassHandler<AvaloniaColorPicker>(OnPopupTypeChanged);
        Control.LoadedEvent.AddClassHandler<AvaloniaColorPicker>(OnLoaded);
    }

    private static void OnLoaded(AvaloniaColorPicker picker, RoutedEventArgs e)
    {
        SyncSheetBehavior(picker, LuminaOptions.GetPopupType(picker));
    }

    private static void OnPopupTypeChanged(AvaloniaColorPicker picker, AvaloniaPropertyChangedEventArgs change)
    {
        SyncSheetBehavior(picker, change.GetNewValue<LuminaPopupType>());
    }

    private static void SyncSheetBehavior(AvaloniaColorPicker picker, LuminaPopupType popupType)
    {
        if (ShouldAttachSheetBehavior(popupType))
        {
            AttachSheetBehavior(picker);
        }
        else
        {
            DetachSheetBehavior(picker);
        }
    }

    private static bool ShouldAttachSheetBehavior(LuminaPopupType popupType)
    {
        return popupType is LuminaPopupType.Auto or LuminaPopupType.Sheet;
    }

    private static void AttachSheetBehavior(AvaloniaColorPicker picker)
    {
        if (picker.GetValue(IsSheetBehaviorAttachedProperty))
        {
            return;
        }

        picker.SetValue(IsSheetBehaviorAttachedProperty, true);
        picker.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        picker.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        picker.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        picker.AddHandler(Button.ClickEvent, OnButtonClick);
        picker.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private static void DetachSheetBehavior(AvaloniaColorPicker picker)
    {
        if (!picker.GetValue(IsSheetBehaviorAttachedProperty))
        {
            return;
        }

        picker.SetValue(IsSheetBehaviorAttachedProperty, false);
        picker.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        picker.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        picker.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        picker.RemoveHandler(Button.ClickEvent, OnButtonClick);
        picker.DetachedFromVisualTree -= OnDetachedFromVisualTree;
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is AvaloniaColorPicker picker)
        {
            DetachSheetBehavior(picker);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not AvaloniaColorPicker picker ||
            !e.GetCurrentPoint(picker).Properties.IsLeftButtonPressed ||
            !IsSourceInsidePicker(picker, e.Source) ||
            !TryShowSheet(picker))
        {
            return;
        }

        e.Handled = true;
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is AvaloniaColorPicker picker &&
            ShouldUseSheet(picker) &&
            IsSourceInsidePicker(picker, e.Source))
        {
            e.Handled = true;
        }
    }

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not AvaloniaColorPicker picker ||
            !IsKeyboardInvocation(e.Key) ||
            !TryShowSheet(picker))
        {
            return;
        }

        e.Handled = true;
    }

    private static void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is AvaloniaColorPicker picker &&
            e.Source is DropDownButton &&
            TryShowSheet(picker))
        {
            e.Handled = true;
        }
    }

    private static bool TryShowSheet(AvaloniaColorPicker picker)
    {
        return ShouldUseSheet(picker) && ShowSheet(picker);
    }

    private static bool ShouldUseSheet(AvaloniaColorPicker picker)
    {
        return LuminaSheetPlacement.ShouldUseSheet(LuminaOptions.GetPopupType(picker));
    }

    private static bool IsSourceInsidePicker(AvaloniaColorPicker picker, object? source)
    {
        if (ReferenceEquals(source, picker))
        {
            return true;
        }

        return source is Control sourceControl &&
               sourceControl.GetVisualAncestors().OfType<AvaloniaColorPicker>().Contains(picker);
    }

    private static bool IsKeyboardInvocation(Key key)
    {
        return key is Key.Return or Key.Space or Key.Down or Key.F4;
    }

    private static bool ShowSheet(AvaloniaColorPicker picker)
    {
        picker.GetVisualDescendants().OfType<DropDownButton>().FirstOrDefault()?.Flyout?.Hide();

        var colorView = CreateColorView(picker);
        var body = new Viewbox
        {
            Width = 356,
            Height = 338,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = colorView
        };

        var content = CreateSheetLayout(
            LuminaLocalization.Get(LuminaLocalizationKeys.PickerSelectColor),
            body,
            CreateDoneAction(picker));

        return LuminaBottomSheetService.Instance.TryShow(picker, content);
    }

    private static AvaloniaColorView CreateColorView(AvaloniaColorPicker picker)
    {
        var view = new AvaloniaColorView();
        var bindings = new List<IDisposable>();

        Bind(view, AvaloniaColorView.ColorProperty, picker, bindings, BindingMode.TwoWay);
        Bind(view, AvaloniaColorView.HsvColorProperty, picker, bindings, BindingMode.TwoWay);
        Bind(view, AvaloniaColorView.SelectedIndexProperty, picker, bindings, BindingMode.TwoWay);

        Bind(view, AvaloniaColorView.ColorModelProperty, picker, bindings);
        Bind(view, AvaloniaColorView.ColorSpectrumComponentsProperty, picker, bindings);
        Bind(view, AvaloniaColorView.ColorSpectrumShapeProperty, picker, bindings);
        Bind(view, AvaloniaColorView.HexInputAlphaPositionProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsAccentColorsVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsAlphaEnabledProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsAlphaVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsColorComponentsVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsColorModelVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsColorPaletteVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsColorPreviewVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsColorSpectrumVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsColorSpectrumSliderVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsComponentSliderVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsComponentTextInputVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.IsHexInputVisibleProperty, picker, bindings);
        Bind(view, AvaloniaColorView.MaxHueProperty, picker, bindings);
        Bind(view, AvaloniaColorView.MaxSaturationProperty, picker, bindings);
        Bind(view, AvaloniaColorView.MaxValueProperty, picker, bindings);
        Bind(view, AvaloniaColorView.MinHueProperty, picker, bindings);
        Bind(view, AvaloniaColorView.MinSaturationProperty, picker, bindings);
        Bind(view, AvaloniaColorView.MinValueProperty, picker, bindings);
        Bind(view, AvaloniaColorView.PaletteProperty, picker, bindings);
        Bind(view, AvaloniaColorView.PaletteColorsProperty, picker, bindings);
        Bind(view, AvaloniaColorView.PaletteColumnCountProperty, picker, bindings);

        view.DetachedFromVisualTree += (_, _) =>
        {
            foreach (var binding in bindings)
            {
                binding.Dispose();
            }

            bindings.Clear();
        };

        return view;
    }

    private static void Bind(
        AvaloniaObject target,
        AvaloniaProperty property,
        AvaloniaObject source,
        ICollection<IDisposable> bindings,
        BindingMode mode = BindingMode.OneWay)
    {
        var state = new PropertySyncState();
        CopyValue(source, target, property, state);
        bindings.Add(source.GetObservable(property).Subscribe(new PropertySyncObserver(target, property, state)));

        if (mode == BindingMode.TwoWay)
        {
            bindings.Add(target.GetObservable(property).Subscribe(new PropertySyncObserver(source, property, state)));
        }
    }

    private static void CopyValue(
        AvaloniaObject source,
        AvaloniaObject target,
        AvaloniaProperty property,
        PropertySyncState state)
    {
        if (state.IsUpdating)
        {
            return;
        }

        state.IsUpdating = true;
        try
        {
            target.SetValue(property, source.GetValue(property));
        }
        finally
        {
            state.IsUpdating = false;
        }
    }

    private sealed class PropertySyncState
    {
        public bool IsUpdating { get; set; }
    }

    private sealed class PropertySyncObserver : IObserver<object?>
    {
        private readonly AvaloniaObject _target;
        private readonly AvaloniaProperty _property;
        private readonly PropertySyncState _state;

        public PropertySyncObserver(AvaloniaObject target, AvaloniaProperty property, PropertySyncState state)
        {
            _target = target;
            _property = property;
            _state = state;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(object? value)
        {
            if (_state.IsUpdating)
            {
                return;
            }

            _state.IsUpdating = true;
            try
            {
                _target.SetValue(_property, value);
            }
            finally
            {
                _state.IsUpdating = false;
            }
        }
    }

    private static Control CreateDoneAction(AvaloniaColorPicker picker)
    {
        var button = new Button
        {
            Content = LuminaLocalization.Get(LuminaLocalizationKeys.CommonDone),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        button.Classes.Add("Primary");
        button.Click += (_, _) => LuminaBottomSheetService.Instance.Close(picker);
        return button;
    }

    private static StackPanel CreateSheetLayout(string title, Control body, Control footer)
    {
        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 18,
                    FontWeight = FontWeight.DemiBold
                },
                body,
                footer
            }
        };
    }
}
