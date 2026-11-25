using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using LuminaUI.Enums;

namespace LuminaUI.Controls;

/// <summary>
/// Blurs whatever is drawn behind this element.
/// </summary>
/// <remarks>The compositor updates the backdrop automatically; no manual refresh is needed.</remarks>
public sealed class LuminaBackdropBlur : Control
{
    /// <summary>
    /// Avalonia converts <see cref="BlurEffect.Radius"/> to a Skia blur sigma with
    /// <c>sigma = 0.288675 * radius + 0.5</c>. <see cref="BlurRadius"/> keeps the historical meaning
    /// of that sigma, so existing themes and styles keep their exact look.
    /// </summary>
    private const double RadiusToSigmaSlope = 0.288675;

    private const double RadiusToSigmaIntercept = 0.5;

    private const double MinSigma = 1.0;

    private const double MaxSigma = 320.0;

    public static readonly StyledProperty<double> BlurRadiusProperty;

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty;

    public static readonly StyledProperty<LuminaBackdropBlurMode> ModeProperty;

    private readonly VolatileBackdropEffectCache _volatileCache = new();

    private Size _clipSize;

    private CornerRadius _clipCornerRadius;

    private Geometry? _clip;

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
    }

    /// <summary>Retained for compatibility. Backdrops update automatically.</summary>
    [Obsolete("Backdrop blur updates automatically; this method is no longer needed.")]
    public void Refresh()
    {
    }

    /// <summary>Retained for compatibility. Backdrops update automatically.</summary>
    [Obsolete("Backdrop blur updates automatically; this method is no longer needed.")]
    public void RefreshBackdrop()
    {
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateBackdrop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BlurRadiusProperty ||
            change.Property == CornerRadiusProperty ||
            change.Property == ModeProperty)
        {
            UpdateBackdrop();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateBackdrop();
    }

    private void UpdateBackdrop()
    {
        Size size = Bounds.Size;
        if (Mode == LuminaBackdropBlurMode.Off || BlurRadius <= 0.0 || size.Width <= 0.0 || size.Height <= 0.0)
        {
            SetCurrentValue(BackdropEffectProperty, null);
            SetCurrentValue(ClipProperty, null);
            SetCurrentValue(BackdropEffectCacheProperty, null);
            _clip = null;
            _clipSize = default;
            return;
        }

        SetCurrentValue(ClipProperty, GetClip(size, CornerRadius));

        SetCurrentValue(BackdropEffectCacheProperty, _volatileCache);

        double sigma = Math.Clamp(BlurRadius, MinSigma, MaxSigma);
        SetCurrentValue(
            BackdropEffectProperty,
            new BlurEffect { Radius = ((sigma - RadiusToSigmaIntercept) / RadiusToSigmaSlope) });
    }

    private Geometry GetClip(Size size, CornerRadius cornerRadius)
    {
        if (_clip != null && _clipSize == size && _clipCornerRadius == cornerRadius)
        {
            return _clip;
        }

        _clip = CreateClip(new Rect(size), cornerRadius);
        _clipSize = size;
        _clipCornerRadius = cornerRadius;
        return _clip;
    }

    private static Geometry CreateClip(Rect rect, CornerRadius cornerRadius)
    {
        double limit = Math.Min(rect.Width, rect.Height) / 2.0;
        double topLeft = Math.Clamp(cornerRadius.TopLeft, 0.0, limit);
        double topRight = Math.Clamp(cornerRadius.TopRight, 0.0, limit);
        double bottomRight = Math.Clamp(cornerRadius.BottomRight, 0.0, limit);
        double bottomLeft = Math.Clamp(cornerRadius.BottomLeft, 0.0, limit);

        // The backdrop effect is clipped to the visual's layout rect. A geometry clip on top of that
        // is what makes the blur follow CornerRadius; the compositor applies it before it samples.
        if (topLeft == topRight && topRight == bottomRight && bottomRight == bottomLeft)
        {
            return new RectangleGeometry(rect, topLeft, topLeft);
        }

        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(rect.Left + topLeft, rect.Top), isFilled: true);
            context.LineTo(new Point(rect.Right - topRight, rect.Top));
            AddCorner(context, new Point(rect.Right, rect.Top + topRight), topRight);
            context.LineTo(new Point(rect.Right, rect.Bottom - bottomRight));
            AddCorner(context, new Point(rect.Right - bottomRight, rect.Bottom), bottomRight);
            context.LineTo(new Point(rect.Left + bottomLeft, rect.Bottom));
            AddCorner(context, new Point(rect.Left, rect.Bottom - bottomLeft), bottomLeft);
            context.LineTo(new Point(rect.Left, rect.Top + topLeft));
            AddCorner(context, new Point(rect.Left + topLeft, rect.Top), topLeft);
            context.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static void AddCorner(StreamGeometryContext context, Point destination, double radius)
    {
        if (radius <= 0.0)
        {
            context.LineTo(destination);
            return;
        }

        context.ArcTo(destination, new Size(radius, radius), 0.0, isLargeArc: false, SweepDirection.Clockwise);
    }
}
