using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using LuminaUI.Theming;

namespace LuminaUI.Demo.ViewModels;

internal static class SandboxWindowActions
{
    public static void RefreshThemeAndWindowMaterial()
    {
        Dispatcher.UIThread.Post(() =>
        {
            LuminaThemeManager.Refresh();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow })
            {
                mainWindow.RefreshWindowMaterial();
            }
        });
    }
}
