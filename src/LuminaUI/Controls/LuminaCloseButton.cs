using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaCloseButton : Button
{
    private const string DefaultIconGeometry = "M18.3,5.71 L16.89,4.29 L12,9.17 L7.11,4.29 L5.7,5.71 L10.59,10.59 L5.7,15.48 L7.11,16.9 L12,12 L16.89,16.9 L18.3,15.48 L13.41,10.59 Z";

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<LuminaCloseButton, double>(nameof(IconSize), 11.0);

    public static readonly StyledProperty<Geometry> IconDataProperty =
        AvaloniaProperty.Register<LuminaCloseButton, Geometry>(nameof(IconData), Geometry.Parse(DefaultIconGeometry));

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Geometry IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }
}
