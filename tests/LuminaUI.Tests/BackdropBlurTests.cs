using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using LuminaUI.Controls;
using LuminaUI.Enums;
using Xunit;

namespace LuminaUI.Tests;

#pragma warning disable CS0618 // Exercise the compatibility modes.

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
        using Scene scene = new(mode);
        WriteableBitmap frame = scene.Capture();

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
        using Scene scene = new(mode, blurTop: -50, blurHeight: 150);
        WriteableBitmap frame = scene.Capture();

        AssertMixed(frame, SeamX - 2, 5);
        AssertMixed(frame, SeamX - 2, 50);
        AssertMixed(frame, SeamX + 1, 95);
    }

    [AvaloniaTheory]
    [InlineData(LuminaBackdropBlurMode.Dynamic)]
    [InlineData(LuminaBackdropBlurMode.Cached)]
    public void ClipsToCornerRadius(LuminaBackdropBlurMode mode)
    {
        // The element starts at x=80, so the seam runs 20px inside it. With a 40px corner radius the
        // points below sit inside the layout rect but outside the rounded clip: they must show the
        // raw backdrop, not a red/blue mix.
        using Scene scene = new(mode, blurLeft: 80, cornerRadius: 40);
        WriteableBitmap frame = scene.Capture();

        AssertMixed(frame, SeamX - 2, 50);
        AssertMixed(frame, SeamX + 1, 50);
        AssertRaw(frame, 82, 2, red: true);
        AssertRaw(frame, SeamX + 2, 2, red: false);
    }

    [AvaloniaTheory]
    [InlineData(LuminaBackdropBlurMode.Dynamic)]
    [InlineData(LuminaBackdropBlurMode.Cached)]
    public void FollowsChangesBehindTheElementWithoutRefresh(LuminaBackdropBlurMode mode)
    {
        // Volatile mode samples the backdrop again on every draw, so content moving behind the
        // element is reflected without any manual invalidation.
        using Scene scene = new(mode);
        AssertMixed(scene.Capture(), SeamX - 2, 50);

        scene.Backdrop.Background = Brushes.Blue;
        WriteableBitmap frame = scene.Capture();

        (byte r, byte b) = Read(frame, SeamX - 2, 50);
        Assert.True(r < 40 && b > 200, $"Expected the seam to become pure blue but got R={r} B={b}.");
    }

    [AvaloniaTheory]
    [InlineData(LuminaBackdropBlurMode.Dynamic)]
    [InlineData(LuminaBackdropBlurMode.Cached)]
    public void BlursInsideEffectLayer(LuminaBackdropBlurMode mode)
    {
        using Scene scene = new(mode, effectLayer: true);
        WriteableBitmap frame = scene.Capture();

        AssertMixed(frame, SeamX - 2, 50);
        AssertMixed(frame, SeamX + 1, 50);
    }

    [AvaloniaTheory]
    [InlineData(LuminaBackdropBlurMode.Dynamic)]
    [InlineData(LuminaBackdropBlurMode.Cached)]
    public void StopsDrawingWhenTurnedOff(LuminaBackdropBlurMode mode)
    {
        using Scene scene = new(mode);
        AssertMixed(scene.Capture(), SeamX - 2, 50);

        scene.Blur.Mode = LuminaBackdropBlurMode.Off;
        WriteableBitmap frame = scene.Capture();

        AssertRaw(frame, SeamX - 2, 50, red: true);
        AssertRaw(frame, SeamX + 1, 50, red: false);
    }

    [AvaloniaTheory]
    [InlineData(LuminaGlassMode.AcrylicCached)]
    [InlineData(LuminaGlassMode.AcrylicDynamic)]
    public void ThemedCardBlursTheBackdrop(LuminaGlassMode glassMode)
    {
        // Covers the whole template path: the GlassMode style turns the card's internal
        // LuminaBackdropBlur on, and the card then has to blur what is behind it.
        Canvas canvas = new() { Width = 200, Height = 100 };
        canvas.Children.Add(Rect(0, Brushes.Red));
        canvas.Children.Add(Rect(SeamX, Brushes.Blue));

        LuminaCard card = new()
        {
            Width = 160,
            Height = 90,
            GlassMode = glassMode,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };
        Canvas.SetLeft(card, 20);
        Canvas.SetTop(card, 5);
        canvas.Children.Add(card);

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
            WriteableBitmap frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame.");

            AssertMixed(frame, SeamX - 2, 50);
            AssertMixed(frame, SeamX + 1, 50);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(LuminaGlassMode.AcrylicCached, false)]
    [InlineData(LuminaGlassMode.AcrylicCached, true)]
    [InlineData(LuminaGlassMode.AcrylicDynamic, false)]
    [InlineData(LuminaGlassMode.AcrylicDynamic, true)]
    public void AcrylicCardHasNoDefaultEdge(LuminaGlassMode mode, bool dark)
    {
        LuminaCard card = CreateEdgeTestCard(mode);
        WithCardFrame(card, dark, frame =>
        {
            (byte edgeRed, byte edgeBlue) = Read(frame, 20, 50);
            (byte insideRed, byte insideBlue) = Read(frame, 30, 50);
            Assert.InRange(Math.Abs(edgeRed - insideRed), 0, 3);
            Assert.InRange(Math.Abs(edgeBlue - insideBlue), 0, 3);
        });
    }

    [AvaloniaTheory]
    [InlineData(LuminaGlassMode.AcrylicCached, false, false)]
    [InlineData(LuminaGlassMode.AcrylicCached, true, true)]
    [InlineData(LuminaGlassMode.AcrylicDynamic, false, true)]
    [InlineData(LuminaGlassMode.AcrylicDynamic, true, false)]
    public void AcrylicCardHonorsExplicitEdges(LuminaGlassMode mode, bool dark, bool glassEdge)
    {
        LuminaCard card = CreateEdgeTestCard(mode);
        if (glassEdge)
        {
            card.GlassEdgeBrush = Brushes.Red;
        }
        else
        {
            card.BorderThickness = new Thickness(4);
            card.BorderBrush = Brushes.Red;
        }

        WithCardFrame(card, dark, frame =>
        {
            (byte edgeRed, byte edgeBlue) = Read(frame, 20, 50);
            (byte insideRed, byte insideBlue) = Read(frame, 30, 50);
            Assert.True(edgeRed > insideRed + 40 && edgeBlue < insideBlue - 20);
        });
    }

    [AvaloniaTheory]
    [InlineData(LuminaGlassMode.Off)]
    [InlineData(LuminaGlassMode.Pseudo)]
    public void OtherCardModesKeepTheirBorders(LuminaGlassMode mode)
    {
        LuminaCard card = CreateEdgeTestCard(mode);
        WithCardFrame(card, false, _ => Assert.True(card.BorderThickness.Left > 0));
    }

    private static LuminaCard CreateEdgeTestCard(LuminaGlassMode mode) => new()
    {
        Width = 160,
        Height = 80,
        GlassMode = mode,
        GlassTintBrush = Brushes.Transparent,
        CornerRadius = new CornerRadius(0),
    };

    private static void WithCardFrame(LuminaCard card, bool dark, Action<WriteableBitmap> check)
    {
        Canvas canvas = new() { Width = 200, Height = 100, Background = new SolidColorBrush(Color.FromRgb(32, 64, 128)) };
        Canvas.SetLeft(card, 20);
        Canvas.SetTop(card, 10);
        canvas.Children.Add(card);
        Window window = new()
        {
            Width = 200,
            Height = 100,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light,
            Content = canvas,
        };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            using WriteableBitmap frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame.");
            check(frame);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class Scene : IDisposable
    {
        public Scene(
            LuminaBackdropBlurMode mode,
            double blurTop = 0,
            double blurHeight = 100,
            double blurLeft = 0,
            double cornerRadius = 0,
            bool effectLayer = false)
        {
            Canvas canvas = new() { Width = 200, Height = 100 };
            if (effectLayer)
            {
                canvas.Effect = new BlurEffect { Radius = 1 };
            }
            Backdrop = new Border { Width = 100, Height = 100, Background = Brushes.Red };
            Canvas.SetLeft(Backdrop, 0);
            Canvas.SetTop(Backdrop, 0);
            canvas.Children.Add(Backdrop);
            canvas.Children.Add(Rect(SeamX, Brushes.Blue));

            Blur = new LuminaBackdropBlur
            {
                Mode = mode,
                BlurRadius = 12,
                CornerRadius = new CornerRadius(cornerRadius),
                Width = 200 - blurLeft,
                Height = blurHeight,
            };
            Canvas.SetLeft(Blur, blurLeft);
            Canvas.SetTop(Blur, blurTop);
            canvas.Children.Add(Blur);

            Border clipHost = new()
            {
                ClipToBounds = true,
                Width = 200,
                Height = 100,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Child = canvas,
            };

            Window = new Window { Width = 200, Height = 100, Background = Brushes.White, Content = clipHost };
            Window.Show();
        }

        public Window Window { get; }

        public Border Backdrop { get; }

        public LuminaBackdropBlur Blur { get; }

        public WriteableBitmap Capture()
        {
            Dispatcher.UIThread.RunJobs();
            return Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame.");
        }

        public void Dispose()
        {
            Window.Close();
        }
    }

    private static Border Rect(double left, IBrush brush)
    {
        Border border = new() { Width = 100, Height = 100, Background = brush };
        Canvas.SetLeft(border, left);
        Canvas.SetTop(border, 0);
        return border;
    }

    private static (byte R, byte B) Read(WriteableBitmap frame, int x, int y)
    {
        using ILockedFramebuffer buffer = frame.Lock();
        bool bgra = buffer.Format == PixelFormat.Bgra8888;
        Assert.True(bgra || buffer.Format == PixelFormat.Rgba8888, $"Unexpected pixel format {buffer.Format}.");
        int offset = (y * buffer.RowBytes) + (x * 4);
        return (Marshal.ReadByte(buffer.Address, offset + (bgra ? 2 : 0)),
                Marshal.ReadByte(buffer.Address, offset + (bgra ? 0 : 2)));
    }

    private static void AssertMixed(WriteableBitmap frame, int x, int y)
    {
        (byte r, byte b) = Read(frame, x, y);
        Assert.True(
            r > 40 && b > 40,
            $"Expected a red/blue mix at ({x},{y}) but got R={r} B={b}; the backdrop blur was not drawn there.");
    }

    private static void AssertRaw(WriteableBitmap frame, int x, int y, bool red)
    {
        (byte r, byte b) = Read(frame, x, y);
        Assert.True(
            red ? b < 20 && r > 200 : r < 20 && b > 200,
            $"Expected the raw {(red ? "red" : "blue")} backdrop at ({x},{y}) but got R={r} B={b}.");
    }
}

#pragma warning restore CS0618
