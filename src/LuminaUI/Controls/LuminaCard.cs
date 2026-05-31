using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaCard : ContentControl
{
    public static readonly StyledProperty<bool> IsElevatedProperty =
        AvaloniaProperty.Register<LuminaCard, bool>(nameof(IsElevated), defaultValue: false);

    public static readonly StyledProperty<double> BackdropBlurRadiusProperty =
        AvaloniaProperty.Register<LuminaCard, double>(nameof(BackdropBlurRadius), 128.0);

    public static readonly StyledProperty<IBrush?> GlassTintBrushProperty =
        AvaloniaProperty.Register<LuminaCard, IBrush?>(nameof(GlassTintBrush));

    public static readonly StyledProperty<IBrush?> GlassEdgeBrushProperty =
        AvaloniaProperty.Register<LuminaCard, IBrush?>(nameof(GlassEdgeBrush));

    public bool IsElevated
    {
        get => GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    public double BackdropBlurRadius
    {
        get => GetValue(BackdropBlurRadiusProperty);
        set => SetValue(BackdropBlurRadiusProperty, value);
    }

    public IBrush? GlassTintBrush
    {
        get => GetValue(GlassTintBrushProperty);
        set => SetValue(GlassTintBrushProperty, value);
    }

    public IBrush? GlassEdgeBrush
    {
        get => GetValue(GlassEdgeBrushProperty);
        set => SetValue(GlassEdgeBrushProperty, value);
    }
}
