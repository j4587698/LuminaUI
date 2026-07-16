using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Enums;
using SkiaSharp;

namespace LuminaUI.Controls;

public sealed class LuminaBackdropBlur : Control
{
    private sealed class CachedBackdrop : IDisposable
    {
        private readonly object _syncRoot = new();
        private SKImage? _image;
        private int _version;

        public int Version
        {
            get
            {
                lock (_syncRoot)
                {
                    return _version;
                }
            }
        }

        public void DrawOrCapture(
            ISkiaSharpApiLease lease,
            SKCanvas canvas,
            SKRect destination,
            float sigma,
            int expectedVersion)
        {
            lock (_syncRoot)
            {
                if (_version != expectedVersion)
                {
                    return;
                }

                if (_image == null)
                {
                    _image = Capture(lease, canvas, destination, sigma);
                }

                if (_image != null)
                {
                    canvas.DrawImage(_image, destination);
                }
            }
        }

        public void Invalidate()
        {
            lock (_syncRoot)
            {
                _image?.Dispose();
                _image = null;
                _version++;
            }
        }

        public void Dispose()
        {
            Invalidate();
        }

        private static SKImage? Capture(ISkiaSharpApiLease lease, SKCanvas canvas, SKRect destination, float sigma)
        {
            SKSurface? sourceSurface = lease.SkSurface;
            if (sourceSurface == null)
            {
                return null;
            }

            SKMatrix matrix = canvas.TotalMatrix;
            SKRect mapped = matrix.MapRect(destination);
            SKRectI clip = canvas.DeviceClipBounds;
            SKRectI sourceBounds = new(
                Math.Max(clip.Left, (int)Math.Floor(mapped.Left)),
                Math.Max(clip.Top, (int)Math.Floor(mapped.Top)),
                Math.Min(clip.Right, (int)Math.Ceiling(mapped.Right)),
                Math.Min(clip.Bottom, (int)Math.Ceiling(mapped.Bottom)));

            if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
            {
                return null;
            }

            using SKImage source = sourceSurface.Snapshot(sourceBounds);
            if (source == null)
            {
                return null;
            }

            SKImageInfo imageInfo = new(sourceBounds.Width, sourceBounds.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using SKSurface? blurredSurface = SKSurface.Create(imageInfo);
            if (blurredSurface == null)
            {
                return null;
            }

            float scaleX = MathF.Sqrt((matrix.ScaleX * matrix.ScaleX) + (matrix.SkewY * matrix.SkewY));
            float scaleY = MathF.Sqrt((matrix.SkewX * matrix.SkewX) + (matrix.ScaleY * matrix.ScaleY));
            using SKImageFilter blur = SKImageFilter.CreateBlur(
                Math.Clamp(sigma * Math.Max(scaleX, 0.01f), 1f, 320f),
                Math.Clamp(sigma * Math.Max(scaleY, 0.01f), 1f, 320f),
                SKShaderTileMode.Clamp);
            using SKPaint paint = new()
            {
                ImageFilter = blur,
                IsAntialias = true
            };
            SKRect imageBounds = SKRect.Create(sourceBounds.Width, sourceBounds.Height);
            blurredSurface.Canvas.Clear(SKColors.Transparent);
            blurredSurface.Canvas.DrawImage(source, imageBounds, paint);
            blurredSurface.Canvas.Flush();
            return blurredSurface.Snapshot();
        }
    }

    private sealed class BackdropBlurOperation : ICustomDrawOperation, IEquatable<ICustomDrawOperation>, IDisposable
    {
        private readonly Rect _bounds;

        private readonly double _blurRadius;

        private readonly CornerRadius _cornerRadius;

        private readonly LuminaBackdropBlurMode _mode;

        private readonly CachedBackdrop _cachedBackdrop;

        private readonly int _cacheVersion;

        public Rect Bounds => _bounds;

        public BackdropBlurOperation(
            Rect bounds,
            double blurRadius,
            CornerRadius cornerRadius,
            LuminaBackdropBlurMode mode,
            CachedBackdrop cachedBackdrop)
        {
            _bounds = bounds;
            _blurRadius = blurRadius;
            _cornerRadius = cornerRadius;
            _mode = mode;
            _cachedBackdrop = cachedBackdrop;
            _cacheVersion = cachedBackdrop.Version;
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
            return other is BackdropBlurOperation op &&
                   op._bounds == _bounds &&
                   op._blurRadius == _blurRadius &&
                   op._cornerRadius == _cornerRadius &&
                   op._mode == _mode &&
                   op._cachedBackdrop == _cachedBackdrop &&
                   op._cacheVersion == _cacheVersion;
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
            using SKRoundRect clip = CreateRoundRect(rect, _cornerRadius);

            if (_mode == LuminaBackdropBlurMode.Cached)
            {
                canvas.Save();
                canvas.ClipRoundRect(clip, SKClipOperation.Intersect, antialias: true);
                _cachedBackdrop.DrawOrCapture(lease, canvas, rect, sigma, _cacheVersion);
                canvas.Restore();
                return;
            }

            using SKImageFilter backdrop = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp);
            if (backdrop == null)
            {
                return;
            }
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

    public static readonly StyledProperty<LuminaBackdropBlurMode> ModeProperty;

    private readonly CachedBackdrop _cachedBackdrop = new();

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

    public LuminaBackdropBlurMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    static LuminaBackdropBlur()
    {
        BlurRadiusProperty = AvaloniaProperty.Register<LuminaBackdropBlur, double>(nameof(BlurRadius), 28.0);
        CornerRadiusProperty = AvaloniaProperty.Register<LuminaBackdropBlur, CornerRadius>(nameof(CornerRadius));
        ModeProperty = AvaloniaProperty.Register<LuminaBackdropBlur, LuminaBackdropBlurMode>(
            nameof(Mode),
            LuminaBackdropBlurMode.Dynamic);
        InputElement.FocusableProperty.OverrideDefaultValue<LuminaBackdropBlur>(defaultValue: false);
        InputElement.IsHitTestVisibleProperty.OverrideDefaultValue<LuminaBackdropBlur>(defaultValue: false);
        Visual.AffectsRender<LuminaBackdropBlur>(
            new AvaloniaProperty[3] { BlurRadiusProperty, CornerRadiusProperty, ModeProperty });
    }

    public void Refresh()
    {
        RefreshBackdrop();
    }

    public void RefreshBackdrop()
    {
        _cachedBackdrop.Invalidate();
        InvalidateVisual();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        _cachedBackdrop.Invalidate();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (this.IsAttachedToVisualTree())
                {
                    RefreshBackdrop();
                }
            },
            DispatcherPriority.Background);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BlurRadiusProperty ||
            change.Property == CornerRadiusProperty ||
            change.Property == ModeProperty)
        {
            _cachedBackdrop.Invalidate();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        _cachedBackdrop.Invalidate();
        base.OnSizeChanged(e);
    }

    public override void Render(DrawingContext context)
    {
        Size size = Bounds.Size;
        if (!(size.Width <= 0.0) &&
            !(size.Height <= 0.0) &&
            !(BlurRadius <= 0.0) &&
            Mode != LuminaBackdropBlurMode.Off)
        {
            context.Custom(new BackdropBlurOperation(
                new Rect(default(Point), size),
                BlurRadius,
                CornerRadius,
                Mode,
                _cachedBackdrop));
        }
    }
}
