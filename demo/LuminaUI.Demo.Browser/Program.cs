using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using LuminaUI.Demo;

[assembly: SupportedOSPlatform("browser")]

namespace LuminaUI.Demo.Browser;

internal static class Program
{
    private static Task Main(string[] args)
    {
        return BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }
}
