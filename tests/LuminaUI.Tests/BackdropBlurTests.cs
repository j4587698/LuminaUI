using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LuminaUI.Controls;
using LuminaUI.Enums;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Pixel checks for <see cref="LuminaBackdropBlur"/>.
/// </summary>
/// <remarks>
/// The backdrop sits on a hard red|blue seam. A working blur mixes both colours around the seam;
/// a blur that silently skipped drawing leaves the seam pure red or pure blue.
/// </remarks>
public class BackdropBlurTests
{
    private const int SeamX = 100;

    [AvaloniaTheory]
    [InlineData(LuminaBackdropBlurMode.Dynamic)]
    [InlineData(LuminaBackdropBlurMode.Cached)]
    public void BlursWhenFullyVisible(LuminaBackdropBlurMode mode)
    {
        WriteableBitmap frame = Render(mode, blurTop: 0, blurHeight: 100);

        AssertMixed(frame, SeamX - 2, 50);
        AssertMixed(frame, SeamX + 1, 50);
    }

    [AvaloniaTheory]
    [InlineData(LuminaBackdropBlurMode.Dynamic)]
    [InlineData(LuminaBackdropBlurMode.Cached)]
    public void BlursWhenPartiallyClippedByAncestor(LuminaBackdropBlurMode mode)
    {
        // Half of the blur element hangs above the clipping container, like a glass card scrolled
        // to the edge of a ScrollViewer viewport. The device clip can then never contain the whole
        // element, and the visible half must still be blurred.
        WriteableBitmap frame = Render(mode, blurTop: -50, blurHeight: 150);

        AssertMixed(frame, SeamX - 2, 5);
        AssertMixed(frame, SeamX - 2, 50);
        AssertMixed(frame, SeamX + 1, 95);
    }

    private static WriteableBitmap Render(LuminaBackdropBlurMode mode, double blurTop, double blurHeight)
    {
        Canvas canvas = new() { Width = 200, Height = 100 };
        canvas.Children.Add(Rect(0, Brushes.Red));
        canvas.Children.Add(Rect(SeamX, Brushes.Blue));

        LuminaBackdropBlur blur = new() { Mode = mode, BlurRadius = 12, Width = 200, Height = blurHeight };
        Canvas.SetLeft(blur, 0);
        Canvas.SetTop(blur, blurTop);
        canvas.Children.Add(blur);

        Border clipHost = new()
        {
            ClipToBounds = true,
            Width = 200,
            Height = 100,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Child = canvas,
        };

        Window window = new() { Width = 200, Height = 100, Background = Brushes.White, Content = clipHost };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            return window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame.");
        }
        finally
        {
            window.Close();
        }
    }

    private static Border Rect(double left, IBrush brush)
    {
        Border border = new() { Width = 100, Height = 100, Background = brush };
        Canvas.SetLeft(border, left);
        Canvas.SetTop(border, 0);
        return border;
    }

    private static void AssertMixed(WriteableBitmap frame, int x, int y)
    {
        using ILockedFramebuffer buffer = frame.Lock();
        bool bgra = buffer.Format == PixelFormat.Bgra8888;
        Assert.True(bgra || buffer.Format == PixelFormat.Rgba8888, $"Unexpected pixel format {buffer.Format}.");
        int offset = (y * buffer.RowBytes) + (x * 4);
        byte r = Marshal.ReadByte(buffer.Address, offset + (bgra ? 2 : 0));
        byte b = Marshal.ReadByte(buffer.Address, offset + (bgra ? 0 : 2));

        Assert.True(
            r > 40 && b > 40,
            $"Expected a red/blue mix at ({x},{y}) but got R={r} B={b}; the backdrop blur was not drawn there.");
    }
}
