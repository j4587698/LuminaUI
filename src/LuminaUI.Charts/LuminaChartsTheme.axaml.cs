using Avalonia.Styling;
using Avalonia.Markup.Xaml;

[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/luminaui", "LuminaUI.Charts")]

namespace LuminaUI.Charts;

public sealed class LuminaChartsTheme : Styles
{
    public LuminaChartsTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}