using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using LuminaUI.Controls;

namespace LuminaUI.Extensions;

public static class LuminaAnimationExtensions
{
    public static LuminaAnimationBuilder<T> Animate<T>(this Animatable target, AvaloniaProperty<T> property)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);

        return new LuminaAnimationBuilder<T>(target, property);
    }

    public static Task AnimateAsync<T>(
        this Animatable target,
        AvaloniaProperty<T> property,
        T from,
        T to,
        TimeSpan? duration = null,
        Easing? easing = null,
        CancellationToken cancellationToken = default)
    {
        return target.Animate(property)
            .From(from)
            .To(to)
            .WithDuration(duration ?? TimeSpan.FromMilliseconds(240))
            .WithEasing(easing ?? new CubicEaseOut())
            .RunAsync(cancellationToken);
    }

    public static Task AnimateTranslateAsync(
        this Control target,
        double x,
        double y,
        TimeSpan? duration = null,
        Easing? easing = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        var current = LuminaMotion.GetCurrentTransform(target);
        var animationDuration = duration ?? TimeSpan.FromMilliseconds(280);
        var animationEasing = easing ?? new CubicEaseOut();

        return LuminaMotion.AnimateVisualTransformAsync(
            target,
            x,
            y,
            current.ScaleX,
            current.ScaleY,
            animationDuration,
            animationEasing,
            cancellationToken);
    }

    public static Task AnimateScaleAsync(
        this Control target,
        double scale,
        TimeSpan? duration = null,
        Easing? easing = null,
        CancellationToken cancellationToken = default)
    {
        return target.AnimateScaleAsync(scale, scale, duration, easing, cancellationToken);
    }

    public static Task AnimateScaleAsync(
        this Control target,
        double scaleX,
        double scaleY,
        TimeSpan? duration = null,
        Easing? easing = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        var current = LuminaMotion.GetCurrentTransform(target);
        var animationDuration = duration ?? TimeSpan.FromMilliseconds(220);
        var animationEasing = easing ?? new CubicEaseOut();

        return LuminaMotion.AnimateVisualTransformAsync(
            target,
            current.X,
            current.Y,
            scaleX,
            scaleY,
            animationDuration,
            animationEasing,
            cancellationToken);
    }

    public static Task AnimateTransformAsync(
        this Control target,
        double x,
        double y,
        double scale,
        TimeSpan? duration = null,
        Easing? easing = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        var animationDuration = duration ?? TimeSpan.FromMilliseconds(280);
        var animationEasing = easing ?? new CubicEaseOut();

        return LuminaMotion.AnimateVisualTransformAsync(
            target,
            x,
            y,
            scale,
            scale,
            animationDuration,
            animationEasing,
            cancellationToken);
    }

    public static void SetTranslate(this Control target, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(target);

        var current = LuminaMotion.GetCurrentTransform(target);
        LuminaMotion.SetVisualTransform(target, x, y, current.ScaleX, current.ScaleY);
    }

    public static void SetScale(this Control target, double scale)
    {
        target.SetScale(scale, scale);
    }

    public static void SetScale(this Control target, double scaleX, double scaleY)
    {
        ArgumentNullException.ThrowIfNull(target);

        var current = LuminaMotion.GetCurrentTransform(target);
        LuminaMotion.SetVisualTransform(target, current.X, current.Y, scaleX, scaleY);
    }
}

public sealed class LuminaAnimationBuilder<T>
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(240);
    private static readonly Easing DefaultEasing = new CubicEaseOut();

    private readonly Animatable _target;
    private readonly AvaloniaProperty<T> _property;
    private T? _from;
    private T? _to;
    private bool _hasFrom;
    private bool _hasTo;
    private TimeSpan _duration = DefaultDuration;
    private Easing _easing = DefaultEasing;
    private FillMode _fillMode = FillMode.Forward;

    internal LuminaAnimationBuilder(Animatable target, AvaloniaProperty<T> property)
    {
        _target = target;
        _property = property;
    }

    public LuminaAnimationBuilder<T> From(T value)
    {
        _from = value;
        _hasFrom = true;
        return this;
    }

    public LuminaAnimationBuilder<T> To(T value)
    {
        _to = value;
        _hasTo = true;
        return this;
    }

    public LuminaAnimationBuilder<T> WithDuration(TimeSpan duration)
    {
        _duration = duration <= TimeSpan.Zero ? DefaultDuration : duration;
        return this;
    }

    public LuminaAnimationBuilder<T> WithEasing(Easing easing)
    {
        _easing = easing;
        return this;
    }

    public LuminaAnimationBuilder<T> WithFillMode(FillMode fillMode)
    {
        _fillMode = fillMode;
        return this;
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        _ = RunAsync(cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_hasTo)
        {
            throw new InvalidOperationException("Animation target value must be set before starting.");
        }

        var animation = new Animation
        {
            Duration = _duration,
            Easing = _easing,
            FillMode = _fillMode,
            IterationCount = new IterationCount(1)
        };

        if (_hasFrom)
        {
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0),
                Setters =
                {
                    new Setter { Property = _property, Value = _from }
                }
            });
        }

        animation.Children.Add(new KeyFrame
        {
            Cue = new Cue(1),
            Setters =
            {
                new Setter { Property = _property, Value = _to }
            }
        });

        return animation.RunAsync(_target, cancellationToken);
    }
}
