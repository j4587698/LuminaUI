using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace LuminaUI.Controls;

public sealed class LuminaPseudoGlass : TemplatedControl
{
    public static readonly StyledProperty<IBrush?> TintBrushProperty =
        AvaloniaProperty.Register<LuminaPseudoGlass, IBrush?>(nameof(TintBrush));

    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<LuminaPseudoGlass, IBrush?>(nameof(HighlightBrush));

    public static readonly StyledProperty<IBrush?> EdgeBrushProperty =
        AvaloniaProperty.Register<LuminaPseudoGlass, IBrush?>(nameof(EdgeBrush));

    public static readonly StyledProperty<Thickness> EdgeThicknessProperty =
        AvaloniaProperty.Register<LuminaPseudoGlass, Thickness>(nameof(EdgeThickness), new Thickness(1.0));

    public static readonly StyledProperty<double> NoiseOpacityProperty =
        AvaloniaProperty.Register<LuminaPseudoGlass, double>(nameof(NoiseOpacity), 0.08);

    public IBrush? TintBrush
    {
        get => GetValue(TintBrushProperty);
        set => SetValue(TintBrushProperty, value);
    }

    public IBrush? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public IBrush? EdgeBrush
    {
        get => GetValue(EdgeBrushProperty);
        set => SetValue(EdgeBrushProperty, value);
    }

    public Thickness EdgeThickness
    {
        get => GetValue(EdgeThicknessProperty);
        set => SetValue(EdgeThicknessProperty, value);
    }

    public double NoiseOpacity
    {
        get => GetValue(NoiseOpacityProperty);
        set => SetValue(NoiseOpacityProperty, value);
    }
}
