using Avalonia;
using Avalonia.Media;

namespace LuminaUI.Demo.Browser;

internal static class AvaloniaAppBuilderExtensions
{
    private const string SourceHanSansCnFontFamily = "avares://LuminaUI.Demo.Browser/Assets/Fonts#Source Han Sans CN";

    public static AppBuilder WithSourceHanSansCnFont(this AppBuilder builder)
    {
        FontFamily fontFamily = new(SourceHanSansCnFontFamily);

        return builder.With(new FontManagerOptions
        {
            DefaultFamilyName = SourceHanSansCnFontFamily,
            FontFallbacks =
            [
                new FontFallback
                {
                    FontFamily = fontFamily
                }
            ]
        });
    }
}
