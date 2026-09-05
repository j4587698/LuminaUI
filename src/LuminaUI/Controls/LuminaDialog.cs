using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace LuminaUI.Controls;

[TemplatePart("PART_DialogRoot", typeof(Border))]
[TemplatePart("PART_TitleText", typeof(TextBlock))]
public class LuminaDialog : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<LuminaDialog, string?>(nameof(Title));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
