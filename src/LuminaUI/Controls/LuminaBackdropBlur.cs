using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace LuminaUI.Controls;

public sealed class LuminaBackdropBlur : Control
{
    private sealed class BackdropBlurOperation : ICustomDrawOperation, IEquatable<ICustomDrawOperation>, IDisposable
    {
        private readonly Rect _bounds;

        private readonly double _blurRadius;

        private readonly CornerRadius _cornerRadius;

        public Rect Bounds => _bounds;

        public BackdropBlurOperation(Rect bounds, double blurRadius, CornerRadius cornerRadius)
        {
            _bounds = bounds;
            _blurRadius = blurRadius;
            _cornerRadius = cornerRadius;
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
            return other is BackdropBlurOperation op && op._bounds == _bounds && op._blurRadius == _blurRadius && op._cornerRadius == _cornerRadius;
        }

        public void Render(ImmediateDrawingContext context)
        {
            ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
            {
                return;
            }
            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;
            if (canvas == null)
            {
                return;
            }
            float width = (float)_bounds.Width;
            float height = (float)_bounds.Height;
            if (width <= 0f || height <= 0f)
            {
                return;
            }
            SKRect rect = new SKRect(0f, 0f, width, height);
            float sigma = Math.Clamp((float)_blurRadius, 1f, 320f);
            using SKImageFilter backdrop = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp);
            if (backdrop == null)
            {
                return;
            }
            using SKRoundRect clip = CreateRoundRect(rect, _cornerRadius);
            using SKPaint transparentPaint = new SKPaint
            {
                Color = SKColors.Transparent,
                BlendMode = SKBlendMode.SrcOver
            };
            SKCanvasSaveLayerRec layer = new SKCanvasSaveLayerRec
            {
                Bounds = rect,
                Backdrop = backdrop,
                Flags = SKCanvasSaveLayerRecFlags.None
            };
            canvas.Save();
            canvas.ClipRoundRect(clip, SKClipOperation.Intersect, antialias: true);
            int saveCount = canvas.SaveLayer(in layer);
            canvas.DrawRect(rect, transparentPaint);
            canvas.RestoreToCount(saveCount);
            canvas.Restore();
        }

        private static SKRoundRect CreateRoundRect(SKRect rect, CornerRadius cornerRadius)
        {
            float radiusLimit = Math.Min(rect.Width, rect.Height) / 2f;
            SKPoint[] radii = new SKPoint[4]
            {
                CreateCornerRadius(cornerRadius.TopLeft, radiusLimit),
                CreateCornerRadius(cornerRadius.TopRight, radiusLimit),
                CreateCornerRadius(cornerRadius.BottomRight, radiusLimit),
                CreateCornerRadius(cornerRadius.BottomLeft, radiusLimit)
            };
            SKRoundRect roundRect = new SKRoundRect();
            roundRect.SetRectRadii(rect, radii);
            return roundRect;
        }

        private static SKPoint CreateCornerRadius(double radius, float radiusLimit)
        {
            float clamped = (float)Math.Clamp(radius, 0.0, radiusLimit);
            return new SKPoint(clamped, clamped);
        }
    }

    public static readonly StyledProperty<double> BlurRadiusProperty;

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty;

    public double BlurRadius
    {
        get => GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    static LuminaBackdropBlur()
    {
        BlurRadiusProperty = AvaloniaProperty.Register<LuminaBackdropBlur, double>(nameof(BlurRadius), 28.0);
        CornerRadiusProperty = AvaloniaProperty.Register<LuminaBackdropBlur, CornerRadius>(nameof(CornerRadius));
        InputElement.FocusableProperty.OverrideDefaultValue<LuminaBackdropBlur>(defaultValue: false);
        InputElement.IsHitTestVisibleProperty.OverrideDefaultValue<LuminaBackdropBlur>(defaultValue: false);
        Visual.AffectsRender<LuminaBackdropBlur>(new AvaloniaProperty[2] { BlurRadiusProperty, CornerRadiusProperty });
    }

    public void Refresh()
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        Size size = Bounds.Size;
        if (!(size.Width <= 0.0) && !(size.Height <= 0.0) && !(BlurRadius <= 0.0))
        {
            context.Custom(new BackdropBlurOperation(new Rect(default(Point), size), BlurRadius, CornerRadius));
        }
    }
}
