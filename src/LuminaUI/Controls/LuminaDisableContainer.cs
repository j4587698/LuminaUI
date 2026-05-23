using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;

namespace LuminaUI.Controls;

[PseudoClasses(new string[] { ":disabled" })]
public class LuminaDisableContainer : ContentControl
{
	public const string PC_Disabled = ":disabled";

	public static readonly StyledProperty<bool> IsDisabledProperty = AvaloniaProperty.Register<LuminaDisableContainer, bool>("IsDisabled", defaultValue: false);

	public static readonly StyledProperty<object?> DisabledTipProperty = AvaloniaProperty.Register<LuminaDisableContainer, object?>("DisabledTip");

	public static readonly StyledProperty<double> DisabledOpacityProperty = AvaloniaProperty.Register<LuminaDisableContainer, double>("DisabledOpacity", 0.48);

	public static readonly StyledProperty<IBrush?> OverlayBackgroundProperty = AvaloniaProperty.Register<LuminaDisableContainer, IBrush?>("OverlayBackground");

	public static readonly DirectProperty<LuminaDisableContainer, double> EffectiveContentOpacityProperty = AvaloniaProperty.RegisterDirect<LuminaDisableContainer, double>("EffectiveContentOpacity", (LuminaDisableContainer container) => container.EffectiveContentOpacity, null, 0.0);

	private double _effectiveContentOpacity = 1.0;

	public bool IsDisabled
	{
		get
		{
			return GetValue(IsDisabledProperty);
		}
		set
		{
			SetValue(IsDisabledProperty, value);
		}
	}

	public object? DisabledTip
	{
		get
		{
			return GetValue(DisabledTipProperty);
		}
		set
		{
			SetValue(DisabledTipProperty, value);
		}
	}

	public double DisabledOpacity
	{
		get
		{
			return GetValue(DisabledOpacityProperty);
		}
		set
		{
			SetValue(DisabledOpacityProperty, value);
		}
	}

	public IBrush? OverlayBackground
	{
		get
		{
			return GetValue(OverlayBackgroundProperty);
		}
		set
		{
			SetValue(OverlayBackgroundProperty, value);
		}
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
			base.PseudoClasses.Set(":disabled", IsDisabled);
			UpdateEffectiveContentOpacity();
		}
		else if (change.Property == DisabledOpacityProperty)
		{
			UpdateEffectiveContentOpacity();
		}
	}

	private void UpdateEffectiveContentOpacity()
	{
		EffectiveContentOpacity = (IsDisabled ? DisabledOpacity : 1.0);
	}
}
