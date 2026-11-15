using Avalonia.Markup.Xaml;
using Avalonia.Styling;

[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/luminaui", "LuminaUI.Markdown")]
[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/luminaui", "LuminaUI.Markdown.Controls")]

namespace LuminaUI.Markdown;

/// <summary>
/// The Markdown renderer's visual layer. Register it in <c>Application.Styles</c> after
/// <see cref="LuminaTheme"/>.
/// </summary>
public sealed class LuminaMarkdownTheme : Styles
{
    public LuminaMarkdownTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
