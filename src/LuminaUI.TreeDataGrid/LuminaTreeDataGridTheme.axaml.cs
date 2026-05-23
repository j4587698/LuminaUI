using Avalonia.Markup.Xaml;
using Avalonia.Styling;

[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/luminaui", "LuminaUI.TreeDataGrid")]

namespace LuminaUI.TreeDataGrid;

public sealed class LuminaTreeDataGridTheme : Styles
{
    public LuminaTreeDataGridTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
