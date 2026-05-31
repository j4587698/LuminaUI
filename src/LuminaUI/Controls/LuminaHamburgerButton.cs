using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace LuminaUI.Controls;

public class LuminaHamburgerButton : ToggleButton
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<LuminaHamburgerButton, bool>(nameof(IsOpen), defaultValue: false, inherits: false, BindingMode.TwoWay);

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty && IsChecked != IsOpen)
        {
            SetCurrentValue(IsCheckedProperty, IsOpen);
        }
        else if (change.Property == IsCheckedProperty)
        {
            var isChecked = IsChecked == true;
            if (IsOpen != isChecked)
            {
                SetCurrentValue(IsOpenProperty, isChecked);
            }
        }
    }
}
