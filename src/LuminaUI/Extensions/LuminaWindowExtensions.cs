using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;

namespace LuminaUI.Extensions;

public static class LuminaWindowExtensions
{
    public static Screen? GetHostScreen(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.Screens.ScreenCount == 0)
        {
            return null;
        }

        return window.Screens.ScreenFromWindow(window)
               ?? window.Screens.Primary
               ?? window.Screens.All[0];
    }

    public static void CenterOnHostScreen(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.CenterOnScreen(window.GetHostScreen());
    }

    public static void CenterOnScreen(this Window window, Screen? screen)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (screen is null || window.WindowState != WindowState.Normal)
        {
            return;
        }

        var scaling = window.RenderScaling <= 0 ? 1 : window.RenderScaling;
        var x = (int)(screen.Bounds.X + screen.WorkingArea.Width / 2.0 - window.Bounds.Width * scaling / 2.0);
        var y = (int)(screen.Bounds.Y + screen.WorkingArea.Height / 2.0 - window.Bounds.Height * scaling / 2.0);
        window.Position = new PixelPoint(x, y);
    }

    public static void ConstrainMaxSizeToScreenRatio(this Window window, double maxWidthRatio, double maxHeightRatio)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!double.IsNaN(maxWidthRatio))
        {
            window.MaxWidth = ResolveMaxSize(window, maxWidthRatio, isWidth: true);
        }

        if (!double.IsNaN(maxHeightRatio))
        {
            window.MaxHeight = ResolveMaxSize(window, maxHeightRatio, isWidth: false);
        }
    }

    public static async Task<bool> TryLaunchLinkAsync(this Window window, string? link)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (string.IsNullOrWhiteSpace(link))
        {
            return false;
        }

        var launcher = window.Launcher;

        if (File.Exists(link))
        {
            return await launcher.LaunchFileInfoAsync(new FileInfo(link)).ConfigureAwait(false);
        }

        if (Directory.Exists(link))
        {
            return await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(link)).ConfigureAwait(false);
        }

        return Uri.TryCreate(link, UriKind.Absolute, out var uri)
               && await launcher.LaunchUriAsync(uri).ConfigureAwait(false);
    }

    private static double ResolveMaxSize(Window window, double ratio, bool isWidth)
    {
        if (ratio <= 0 || window.WindowState is WindowState.FullScreen or WindowState.Maximized)
        {
            return double.PositiveInfinity;
        }

        var screen = window.GetHostScreen();
        if (screen is null)
        {
            return double.PositiveInfinity;
        }

        var workingSize = isWidth ? screen.WorkingArea.Width : screen.WorkingArea.Height;
        var minSize = isWidth ? window.MinWidth : window.MinHeight;
        var scaling = window.RenderScaling <= 0 ? 1 : window.RenderScaling;
        return Math.Max(minSize, workingSize / scaling * Math.Clamp(ratio, 0, 1));
    }
}
