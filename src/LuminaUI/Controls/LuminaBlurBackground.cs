using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using LuminaUI.Enums;

namespace LuminaUI.Controls;

public class LuminaBlurBackground : ContentControl
{
    private LuminaBackdropBlur? _backdropBlur;

    public static readonly StyledProperty<LuminaGlassMode> GlassModeProperty =
        AvaloniaProperty.Register<LuminaBlurBackground, LuminaGlassMode>(
            nameof(GlassMode),
            LuminaGlassMode.AcrylicDynamic);

    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<LuminaBlurBackground, double>(nameof(BlurRadius), 44.0);

    public static readonly StyledProperty<IBrush?> TintBrushProperty =
        AvaloniaProperty.Register<LuminaBlurBackground, IBrush?>(nameof(TintBrush));

    public static readonly StyledProperty<IBrush?> PseudoTintBrushProperty =
        AvaloniaProperty.Register<LuminaBlurBackground, IBrush?>(nameof(PseudoTintBrush));

    public static readonly StyledProperty<IBrush?> EdgeBrushProperty =
        AvaloniaProperty.Register<LuminaBlurBackground, IBrush?>(nameof(EdgeBrush));

    public static readonly StyledProperty<Thickness> EdgeThicknessProperty =
        AvaloniaProperty.Register<LuminaBlurBackground, Thickness>(nameof(EdgeThickness), new Thickness(1.0));

    public double BlurRadius
    {
        get => GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    public LuminaGlassMode GlassMode
    {
        get => GetValue(GlassModeProperty);
        set => SetValue(GlassModeProperty, value);
    }

    public IBrush? TintBrush
    {
        get => GetValue(TintBrushProperty);
        set => SetValue(TintBrushProperty, value);
    }

    public IBrush? PseudoTintBrush
    {
        get => GetValue(PseudoTintBrushProperty);
        set => SetValue(PseudoTintBrushProperty, value);
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

    public void RefreshBackdrop()
    {
        _backdropBlur?.RefreshBackdrop();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _backdropBlur = e.NameScope.Find<LuminaBackdropBlur>("PART_BackdropBlur");
    }
}
