using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Theming;

/// <summary>
/// Provides attached properties for controlling component density across the visual/logical tree.
/// </summary>
public static class LuminaDensity
{
    /// <summary>
    /// Attached property that inherits down the visual tree to control component density.
    /// </summary>
    public static readonly AttachedProperty<LuminaDensityMode> ModeProperty =
        AvaloniaProperty.RegisterAttached<Control, LuminaDensityMode>(
            "Mode",
            typeof(LuminaDensity),
            defaultValue: LuminaDensityMode.Normal,
            inherits: true);

    public static LuminaDensityMode GetMode(Control control)
    {
        return control.GetValue(ModeProperty);
    }

    public static void SetMode(Control control, LuminaDensityMode value)
    {
        control.SetValue(ModeProperty, value);
    }
}