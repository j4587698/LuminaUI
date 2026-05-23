using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;

namespace LuminaUI;

public class LuminaTheme : Styles
{
    public LuminaTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}