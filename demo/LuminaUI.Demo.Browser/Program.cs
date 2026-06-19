using System.Globalization;
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
        ApplyStartupCulture(args);

        return BuildAvaloniaApp().StartBrowserAppAsync(
            "out",
            new BrowserPlatformOptions
            {
                RenderingMode = new[] { BrowserRenderingMode.Software2D }
            });
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .WithSourceHanSansCnFont();
    }

    private static void ApplyStartupCulture(string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
        {
            return;
        }

        CultureInfo culture = CultureInfo.GetCultureInfo(args[1]);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
