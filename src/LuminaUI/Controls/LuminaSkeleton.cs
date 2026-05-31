using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaSkeleton : TemplatedControl
{
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<LuminaSkeleton, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<bool> IsAnimatedProperty =
        AvaloniaProperty.Register<LuminaSkeleton, bool>(nameof(IsAnimated), defaultValue: true);

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public bool IsAnimated
    {
        get => GetValue(IsAnimatedProperty);
        set => SetValue(IsAnimatedProperty, value);
    }
}
