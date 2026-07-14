using System;
using Avalonia;
#if DEBUG
using LuminaUI.Diagnostics;
#endif
using LuminaUI.Demo;

namespace LuminaUI.Demo.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.ReadLine();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .UseHarfBuzz()
            .WithInterFont()
            .LogToTrace()
#if DEBUG
            .UseLuminaUIDiagnostics()
#endif
            ;
}
