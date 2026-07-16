using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace LuminaUI.Controls;

public sealed class LuminaNoiseTexture : Control
{
    private sealed class NoiseTextureResource
    {
        public SKBitmap Bitmap { get; }

        public SKImage Image { get; }

        public SKShader Shader { get; }

        public NoiseTextureResource()
        {
            const int textureSize = 64;
            Bitmap = new SKBitmap(new SKImageInfo(textureSize, textureSize, SKColorType.Rgba8888, SKAlphaType.Premul));
            uint state = 0x9E3779B9u;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    state ^= state << 13;
                    state ^= state >> 17;
                    state ^= state << 5;
                    byte value = (byte)(96 + (state % 32));
                    Bitmap.SetPixel(x, y, new SKColor(value, value, value));
                }
            }

            Image = SKImage.FromBitmap(Bitmap);
            Shader = Image.ToShader(
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat,
                new SKSamplingOptions(SKFilterMode.Nearest));
        }
    }

    private sealed class NoiseOperation : ICustomDrawOperation
    {
        private static readonly NoiseTextureResource Texture = new();
        private readonly Rect _bounds;

        public Rect Bounds => _bounds;

        public NoiseOperation(Rect bounds)
        {
            _bounds = bounds;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Dispose()
        {
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return other is NoiseOperation operation && operation._bounds == _bounds;
        }

        public void Render(ImmediateDrawingContext context)
        {
            ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
            {
                return;
            }

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            using SKPaint paint = new()
            {
                Shader = Texture.Shader,
                BlendMode = SKBlendMode.SoftLight,
                IsAntialias = false
            };
            lease.SkCanvas.DrawRect(SKRect.Create((float)_bounds.Width, (float)_bounds.Height), paint);
        }
    }

    static LuminaNoiseTexture()
    {
        InputElement.FocusableProperty.OverrideDefaultValue<LuminaNoiseTexture>(defaultValue: false);
        InputElement.IsHitTestVisibleProperty.OverrideDefaultValue<LuminaNoiseTexture>(defaultValue: false);
    }

    public override void Render(DrawingContext context)
    {
        Size size = Bounds.Size;
        if (size.Width > 0.0 && size.Height > 0.0)
        {
            context.Custom(new NoiseOperation(new Rect(default(Point), size)));
        }
    }
}
