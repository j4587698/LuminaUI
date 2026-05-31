using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;

namespace LuminaUI.Controls;

[PseudoClasses(":disabled")]
public class LuminaDisableContainer : ContentControl
{
    public const string PC_Disabled = ":disabled";

    public static readonly StyledProperty<bool> IsDisabledProperty = AvaloniaProperty.Register<LuminaDisableContainer, bool>(nameof(IsDisabled), defaultValue: false);

    public static readonly StyledProperty<object?> DisabledTipProperty = AvaloniaProperty.Register<LuminaDisableContainer, object?>(nameof(DisabledTip));

    public static readonly StyledProperty<double> DisabledOpacityProperty = AvaloniaProperty.Register<LuminaDisableContainer, double>(nameof(DisabledOpacity), 0.48);

    public static readonly StyledProperty<IBrush?> OverlayBackgroundProperty = AvaloniaProperty.Register<LuminaDisableContainer, IBrush?>(nameof(OverlayBackground));

    public static readonly DirectProperty<LuminaDisableContainer, double> EffectiveContentOpacityProperty = AvaloniaProperty.RegisterDirect<LuminaDisableContainer, double>(nameof(EffectiveContentOpacity), (LuminaDisableContainer container) => container.EffectiveContentOpacity, null, 0.0);

    private double _effectiveContentOpacity = 1.0;

    public bool IsDisabled
    {
        get => GetValue(IsDisabledProperty);
        set => SetValue(IsDisabledProperty, value);
    }

    public object? DisabledTip
    {
        get => GetValue(DisabledTipProperty);
        set => SetValue(DisabledTipProperty, value);
    }

    public double DisabledOpacity
    {
        get => GetValue(DisabledOpacityProperty);
        set => SetValue(DisabledOpacityProperty, value);
    }

    public IBrush? OverlayBackground
    {
        get => GetValue(OverlayBackgroundProperty);
        set => SetValue(OverlayBackgroundProperty, value);
    }

    public double EffectiveContentOpacity
    {
        get
        {
            return _effectiveContentOpacity;
        }
        private set
        {
            SetAndRaise(EffectiveContentOpacityProperty, ref _effectiveContentOpacity, value);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDisabledProperty)
        {
            PseudoClasses.Set(":disabled", IsDisabled);
            UpdateEffectiveContentOpacity();
        }
        else if (change.Property == DisabledOpacityProperty)
        {
            UpdateEffectiveContentOpacity();
        }
    }

    private void UpdateEffectiveContentOpacity()
    {
        EffectiveContentOpacity = IsDisabled ? DisabledOpacity : 1.0;
    }
}
