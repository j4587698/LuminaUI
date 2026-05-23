using Avalonia.Styling;
using Avalonia.Markup.Xaml;

[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/luminaui", "LuminaUI.ColorPicker")]

namespace LuminaUI.ColorPicker;

public sealed class LuminaColorPickerTheme : Styles
{
    public LuminaColorPickerTheme()
    {
        LuminaColorPickerSheetBehavior.EnsureInitialized();
        AvaloniaXamlLoader.Load(this);
    }
}
