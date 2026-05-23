using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace LuminaUI.Controls;

public class LuminaHamburgerButton : ToggleButton
{
	public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<LuminaHamburgerButton, bool>("IsOpen", defaultValue: false, inherits: false, BindingMode.TwoWay);

	public bool IsOpen
	{
		get
		{
			return GetValue(IsOpenProperty);
		}
		set
		{
			SetValue(IsOpenProperty, value);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IsOpenProperty && base.IsChecked != IsOpen)
		{
			SetCurrentValue(ToggleButton.IsCheckedProperty, IsOpen);
		}
		else if (change.Property == ToggleButton.IsCheckedProperty)
		{
			bool isChecked = base.IsChecked == true;
			if (IsOpen != isChecked)
			{
				SetCurrentValue(IsOpenProperty, isChecked);
			}
		}
	}
}
