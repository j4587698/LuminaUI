using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaLoading : ContentControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<LuminaLoading, bool>(nameof(IsActive), defaultValue: true);

    public static readonly StyledProperty<LuminaLoadingKind> KindProperty =
        AvaloniaProperty.Register<LuminaLoading, LuminaLoadingKind>(nameof(Kind), LuminaLoadingKind.Ring);

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<LuminaLoading, double>(nameof(Size), 22.0);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<LuminaLoading, double>(nameof(StrokeThickness), 3.0);

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public LuminaLoadingKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }
}
