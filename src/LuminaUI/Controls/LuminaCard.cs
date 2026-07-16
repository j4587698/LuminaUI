using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using LuminaUI.Enums;

namespace LuminaUI.Controls;

public class LuminaCard : ContentControl
{
    private LuminaBackdropBlur? _backdropBlur;

    public static readonly StyledProperty<LuminaGlassMode> GlassModeProperty =
        AvaloniaProperty.Register<LuminaCard, LuminaGlassMode>(nameof(GlassMode), LuminaGlassMode.Off);

    public static readonly StyledProperty<bool> IsElevatedProperty =
        AvaloniaProperty.Register<LuminaCard, bool>(nameof(IsElevated), defaultValue: false);

    public static readonly StyledProperty<double> BackdropBlurRadiusProperty =
        AvaloniaProperty.Register<LuminaCard, double>(nameof(BackdropBlurRadius), 128.0);

    public static readonly StyledProperty<IBrush?> GlassTintBrushProperty =
        AvaloniaProperty.Register<LuminaCard, IBrush?>(nameof(GlassTintBrush));

    public static readonly StyledProperty<IBrush?> PseudoGlassBrushProperty =
        AvaloniaProperty.Register<LuminaCard, IBrush?>(nameof(PseudoGlassBrush));

    public static readonly StyledProperty<IBrush?> GlassEdgeBrushProperty =
        AvaloniaProperty.Register<LuminaCard, IBrush?>(nameof(GlassEdgeBrush));

    public bool IsElevated
    {
        get => GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    public LuminaGlassMode GlassMode
    {
        get => GetValue(GlassModeProperty);
        set => SetValue(GlassModeProperty, value);
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

    public IBrush? PseudoGlassBrush
    {
        get => GetValue(PseudoGlassBrushProperty);
        set => SetValue(PseudoGlassBrushProperty, value);
    }

    public IBrush? GlassEdgeBrush
    {
        get => GetValue(GlassEdgeBrushProperty);
        set => SetValue(GlassEdgeBrushProperty, value);
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
