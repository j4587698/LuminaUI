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
    /// <summary>
    /// 元素在设备坐标下的两个矩形。
    /// </summary>
    /// <remarks>
    /// Avalonia 只重绘失效矩形，祖先 ClipToBounds / ScrollViewer 视口和窗口边界也会裁剪，
    /// 所以表面上"本帧有效"的背景像素只有 <see cref="Visible"/> = <see cref="Full"/> ∩ DeviceClipBounds。
    /// clip 之外的像素是上一帧的合成结果（可能包含本元素自己上一帧的模糊、遮罩色、对话框），不能当背景用。
    /// </remarks>
    private readonly record struct BackdropRegion(SKRectI Full, SKRectI Visible)
    {
        public bool IsEmpty => Visible.Width <= 0 || Visible.Height <= 0;

        /// <summary>整个元素都落在 clip 内，截到的是完整背景。</summary>
        public bool IsComplete => Visible == Full;
    }

    private sealed class CachedBackdrop : IDisposable
    {
        private readonly object _syncRoot = new();
        private SKImage? _image;
        private SKRect _imageLocalRect;
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

        /// <summary>
        /// 画缓存；没有缓存时，只有拿到完整背景才建立缓存。
        /// 局部重绘帧（失效矩形、祖先裁剪）只画本帧的局部模糊、不入缓存，
        /// 否则残缺快照会被长期复用。
        /// </summary>
        public void Draw(
            SKCanvas canvas,
            SKSurface surface,
            BackdropRegion region,
            SKMatrix matrix,
            float deviceSigmaX,
            float deviceSigmaY,
            SKRect destination,
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
                    if (!region.IsComplete)
                    {
                        DrawBlurredRegion(canvas, surface, region.Visible, deviceSigmaX, deviceSigmaY);
                        return;
                    }

                    _image = CaptureBlurred(surface, region.Visible, deviceSigmaX, deviceSigmaY);
                    if (_image == null)
                    {
                        return;
                    }

                    // 记录缓存对应的局部坐标矩形（而不是设备坐标）：元素之后平移/滚动时缓存跟着元素走，
                    // 这是 Cached 模式"背景只取一次"的既有语义。用取整后的设备矩形反算，避免亚像素拉伸。
                    _imageLocalRect = matrix.TryInvert(out SKMatrix inverse)
                        ? inverse.MapRect(ToRect(region.Full))
                        : destination;
                }

                canvas.DrawImage(_image, _imageLocalRect);
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

        private static SKImage? CaptureBlurred(SKSurface surface, SKRectI visible, float sigmaX, float sigmaY)
        {
            using SKImage? source = SnapshotRegion(surface, visible);
            if (source == null)
            {
                return null;
            }

            SKImageInfo imageInfo = new(visible.Width, visible.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using SKSurface? blurredSurface = SKSurface.Create(imageInfo);
            if (blurredSurface == null)
            {
                return null;
            }

            using SKImageFilter blur = SKImageFilter.CreateBlur(sigmaX, sigmaY, SKShaderTileMode.Clamp);
            using SKPaint paint = new() { ImageFilter = blur };
            blurredSurface.Canvas.Clear(SKColors.Transparent);
            blurredSurface.Canvas.DrawImage(source, SKRect.Create(visible.Width, visible.Height), paint);
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

            SKMatrix matrix = canvas.TotalMatrix;

            // 先算区域、再加圆角 clip：此时的 DeviceClipBounds 只反映祖先裁剪 + 失效矩形，
            // 用它判断"是否拿到了完整背景"才有意义。
            BackdropRegion region = ComputeRegion(canvas, matrix, rect);
            if (region.IsEmpty)
            {
                return;
            }

            canvas.Save();
            try
            {
                canvas.ClipRoundRect(clip, SKClipOperation.Intersect, antialias: true);

                SKSurface? surface = lease.SkSurface;
                if (surface == null)
                {
                    // 拿不到表面就无法自己截背景，退回 Skia 的 backdrop 图层。
                    DrawWithBackdropLayer(canvas, rect, sigma);
                    return;
                }

                (float deviceSigmaX, float deviceSigmaY) = ToDeviceSigma(matrix, sigma);
                if (_mode == LuminaBackdropBlurMode.Cached)
                {
                    _cachedBackdrop.Draw(canvas, surface, region, matrix, deviceSigmaX, deviceSigmaY, rect, _cacheVersion);
                }
                else
                {
                    DrawBlurredRegion(canvas, surface, region.Visible, deviceSigmaX, deviceSigmaY);
                }
            }
            finally
            {
                canvas.Restore();
            }
        }

        private static BackdropRegion ComputeRegion(SKCanvas canvas, SKMatrix matrix, SKRect rect)
        {
            // 外扩取整前先内缩一个极小量，吸收 49.99999 / 100.00001 这类浮点噪声，
            // 不然会平白多出 1px，导致"完整背景"判定永远不成立。
            const float epsilon = 0.01f;
            SKRect mapped = matrix.MapRect(rect);
            SKRectI full = new(
                (int)MathF.Floor(mapped.Left + epsilon),
                (int)MathF.Floor(mapped.Top + epsilon),
                (int)MathF.Ceiling(mapped.Right - epsilon),
                (int)MathF.Ceiling(mapped.Bottom - epsilon));
            SKRectI visible = SKRectI.Intersect(full, canvas.DeviceClipBounds);
            return new BackdropRegion(full, visible);
        }

        private static (float X, float Y) ToDeviceSigma(SKMatrix matrix, float sigma)
        {
            float scaleX = MathF.Sqrt((matrix.ScaleX * matrix.ScaleX) + (matrix.SkewY * matrix.SkewY));
            float scaleY = MathF.Sqrt((matrix.SkewX * matrix.SkewX) + (matrix.ScaleY * matrix.ScaleY));
            return (
                Math.Clamp(sigma * Math.Max(scaleX, 0.01f), 1f, 320f),
                Math.Clamp(sigma * Math.Max(scaleY, 0.01f), 1f, 320f));
        }

        private static void DrawWithBackdropLayer(SKCanvas canvas, SKRect rect, float sigma)
        {
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
            int saveCount = canvas.SaveLayer(in layer);
            canvas.DrawRect(rect, transparentPaint);
            canvas.RestoreToCount(saveCount);
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

    /// <summary>
    /// 只截本帧有效的那块背景（<paramref name="visible"/>），模糊后按设备坐标原位画回。
    /// </summary>
    /// <remarks>
    /// 不把局部快照拉伸到整个元素（旧实现的硬边色块就是这么来的），也不读 clip 之外的像素。
    /// 失效矩形边缘因 Clamp 会有轻微接缝，但内容是正确的局部模糊。
    /// 直接画在当前 canvas 上，GPU 后端不会产生回读。调用方需已设好圆角 clip（clip 本身在设备空间，ResetMatrix 不影响它）。
    /// </remarks>
    private static void DrawBlurredRegion(SKCanvas canvas, SKSurface surface, SKRectI visible, float sigmaX, float sigmaY)
    {
        using SKImage? source = SnapshotRegion(surface, visible);
        if (source == null)
        {
            return;
        }

        using SKImageFilter blur = SKImageFilter.CreateBlur(sigmaX, sigmaY, SKShaderTileMode.Clamp);
        using SKPaint paint = new() { ImageFilter = blur };
        canvas.Save();
        canvas.ResetMatrix();
        canvas.DrawImage(source, ToRect(visible), paint);
        canvas.Restore();
    }

    private static SKImage? SnapshotRegion(SKSurface surface, SKRectI region)
    {
        SKImage? image = surface.Snapshot(region);
        if (image == null)
        {
            return null;
        }

        // Snapshot 会把越界部分裁掉；尺寸对不上说明 canvas 和 surface 坐标系不一致，宁可不画也不错位。
        if (image.Width != region.Width || image.Height != region.Height)
        {
            image.Dispose();
            return null;
        }

        return image;
    }

    private static SKRect ToRect(SKRectI rect)
    {
        return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
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
