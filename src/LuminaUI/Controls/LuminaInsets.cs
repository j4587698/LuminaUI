using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

/// <summary>
/// Provides inherited safe-area padding to the visual tree.
/// Set by <see cref="LuminaTopView"/> and consumed by shells, pages, and custom controls.
/// </summary>
public static class LuminaInsets
{
    public static readonly AttachedProperty<Thickness> SafeAreaPaddingProperty =
        AvaloniaProperty.RegisterAttached<Control, Thickness>(
            "SafeAreaPadding",
            typeof(LuminaInsets),
            defaultValue: default,
            inherits: true);

    public static Thickness GetSafeAreaPadding(Control element) => element.GetValue(SafeAreaPaddingProperty);

    public static void SetSafeAreaPadding(Control element, Thickness value) => element.SetValue(SafeAreaPaddingProperty, value);
}
