using Avalonia.Markup.Xaml;
using Avalonia.Styling;

[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/luminaui", "LuminaUI.DataGrid")]

namespace LuminaUI.DataGrid;

public sealed class LuminaDataGridTheme : Styles
{
    public LuminaDataGridTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
