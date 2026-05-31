using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public sealed class LuminaMotion
{
    private sealed class MotionState
    {
        public bool IsHooked { get; set; }

        public bool IsPointerHooked { get; set; }

        public bool IsSquishHooked { get; set; }

        public bool HasRunFadeIn { get; set; }

        public ScaleTransform? Scale { get; set; }

        public RotateTransform? Rotate { get; set; }

        public SkewTransform? Skew { get; set; }

        public TranslateTransform? Translate { get; set; }

        public double TranslateX { get; set; }

        public double TranslateY { get; set; }

        public double ScaleX { get; set; } = 1.0;

        public double ScaleY { get; set; } = 1.0;

        public double Rotation { get; set; }

        public double? PointerScale { get; set; }

        public CancellationTokenSource? TransformAnimation { get; set; }

        public CancellationTokenSource? FadeAnimation { get; set; }

        public CancellationTokenSource? VisibilityAnimation { get; set; }

        public CancellationTokenSource? DisabledAnimation { get; set; }

        public List<ITransition>? InstalledTransitions { get; set; }
    }

    internal readonly record struct MotionTransforms(ScaleTransform Scale, RotateTransform Rotate, SkewTransform Skew, TranslateTransform Translate);

    private static readonly AttachedProperty<MotionState?> StateProperty;

    public static readonly AttachedProperty<bool> AnimateLayoutProperty;

    public static readonly AttachedProperty<bool> AnimateSizeProperty;

    public static readonly AttachedProperty<bool> AnimateOpacityProperty;

    public static readonly AttachedProperty<double> DurationProperty;

    public static readonly AttachedProperty<LuminaMotionEasingKind> EasingProperty;

    public static readonly AttachedProperty<double> TranslateXProperty;

    public static readonly AttachedProperty<double> TranslateYProperty;

    public static readonly AttachedProperty<double> ScaleProperty;

    public static readonly AttachedProperty<double> RotationProperty;

    public static readonly AttachedProperty<double> HoverScaleProperty;

    public static readonly AttachedProperty<double> PressedScaleProperty;

    public static readonly AttachedProperty<bool> FadeInProperty;

    public static readonly AttachedProperty<double> FadeInScaleProperty;

    public static readonly AttachedProperty<bool> VisibleProperty;

    public static readonly AttachedProperty<double> HiddenScaleProperty;

    public static readonly AttachedProperty<double> DisabledOpacityProperty;

    public static readonly AttachedProperty<double> DisabledScaleProperty;

    public static readonly AttachedProperty<bool> SquishProperty;

    public static readonly AttachedProperty<double> SquishIntensityProperty;

    public static readonly AttachedProperty<double> SquishDepthProperty;

    public static readonly AttachedProperty<bool> SquishTiltProperty;

    private LuminaMotion()
    {
    }

    public static bool GetAnimateLayout(Control control)
    {
        return control.GetValue(AnimateLayoutProperty);
    }

    public static void SetAnimateLayout(Control control, bool value)
    {
        control.SetValue(AnimateLayoutProperty, value);
    }

    public static bool GetAnimateSize(Control control)
    {
        return control.GetValue(AnimateSizeProperty);
    }

    public static void SetAnimateSize(Control control, bool value)
    {
        control.SetValue(AnimateSizeProperty, value);
    }

    public static bool GetAnimateOpacity(Control control)
    {
        return control.GetValue(AnimateOpacityProperty);
    }

    public static void SetAnimateOpacity(Control control, bool value)
    {
        control.SetValue(AnimateOpacityProperty, value);
    }

    public static double GetDuration(Control control)
    {
        return control.GetValue(DurationProperty);
    }

    public static void SetDuration(Control control, double value)
    {
        control.SetValue(DurationProperty, value);
    }

    public static LuminaMotionEasingKind GetEasing(Control control)
    {
        return control.GetValue(EasingProperty);
    }

    public static void SetEasing(Control control, LuminaMotionEasingKind value)
    {
        control.SetValue(EasingProperty, value);
    }

    public static double GetTranslateX(Control control)
    {
        return control.GetValue(TranslateXProperty);
    }

    public static void SetTranslateX(Control control, double value)
    {
        control.SetValue(TranslateXProperty, value);
    }

    public static double GetTranslateY(Control control)
    {
        return control.GetValue(TranslateYProperty);
    }

    public static void SetTranslateY(Control control, double value)
    {
        control.SetValue(TranslateYProperty, value);
    }

    public static double GetScale(Control control)
    {
        return control.GetValue(ScaleProperty);
    }

    public static void SetScale(Control control, double value)
    {
        control.SetValue(ScaleProperty, value);
    }

    public static double GetRotation(Control control)
    {
        return control.GetValue(RotationProperty);
    }

    public static void SetRotation(Control control, double value)
    {
        control.SetValue(RotationProperty, value);
    }

    public static double GetHoverScale(Control control)
    {
        return control.GetValue(HoverScaleProperty);
    }

    public static void SetHoverScale(Control control, double value)
    {
        control.SetValue(HoverScaleProperty, value);
    }

    public static double GetPressedScale(Control control)
    {
        return control.GetValue(PressedScaleProperty);
    }

    public static void SetPressedScale(Control control, double value)
    {
        control.SetValue(PressedScaleProperty, value);
    }

    public static bool GetFadeIn(Control control)
    {
        return control.GetValue(FadeInProperty);
    }

    public static void SetFadeIn(Control control, bool value)
    {
        control.SetValue(FadeInProperty, value);
    }

    public static double GetFadeInScale(Control control)
    {
        return control.GetValue(FadeInScaleProperty);
    }

    public static void SetFadeInScale(Control control, double value)
    {
        control.SetValue(FadeInScaleProperty, value);
    }

    public static bool GetVisible(Control control)
    {
        return control.GetValue(VisibleProperty);
    }

    public static void SetVisible(Control control, bool value)
    {
        control.SetValue(VisibleProperty, value);
    }

    public static double GetHiddenScale(Control control)
    {
        return control.GetValue(HiddenScaleProperty);
    }

    public static void SetHiddenScale(Control control, double value)
    {
        control.SetValue(HiddenScaleProperty, value);
    }

    public static double GetDisabledOpacity(Control control)
    {
        return control.GetValue(DisabledOpacityProperty);
    }

    public static void SetDisabledOpacity(Control control, double value)
    {
        control.SetValue(DisabledOpacityProperty, value);
    }

    public static double GetDisabledScale(Control control)
    {
        return control.GetValue(DisabledScaleProperty);
    }

    public static void SetDisabledScale(Control control, double value)
    {
        control.SetValue(DisabledScaleProperty, value);
    }

    public static bool GetSquish(Control control)
    {
        return control.GetValue(SquishProperty);
    }

    public static void SetSquish(Control control, bool value)
    {
        control.SetValue(SquishProperty, value);
    }

    public static double GetSquishIntensity(Control control)
    {
        return control.GetValue(SquishIntensityProperty);
    }

    public static void SetSquishIntensity(Control control, double value)
    {
        control.SetValue(SquishIntensityProperty, value);
    }

    public static double GetSquishDepth(Control control)
    {
        return control.GetValue(SquishDepthProperty);
    }

    public static void SetSquishDepth(Control control, double value)
    {
        control.SetValue(SquishDepthProperty, value);
    }

    public static bool GetSquishTilt(Control control)
    {
        return control.GetValue(SquishTiltProperty);
    }

    public static void SetSquishTilt(Control control, bool value)
    {
        control.SetValue(SquishTiltProperty, value);
    }

    static LuminaMotion()
    {
        StateProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, MotionState?>("State");
        AnimateLayoutProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, bool>("AnimateLayout", defaultValue: false);
        AnimateSizeProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, bool>("AnimateSize", defaultValue: false);
        AnimateOpacityProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, bool>("AnimateOpacity", defaultValue: false);
        DurationProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("Duration", 240.0);
        EasingProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, LuminaMotionEasingKind>("Easing", LuminaMotionEasingKind.CubicOut);
        TranslateXProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("TranslateX", 0.0);
        TranslateYProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("TranslateY", 0.0);
        ScaleProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("Scale", 1.0);
        RotationProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("Rotation", 0.0);
        HoverScaleProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("HoverScale", 1.0);
        PressedScaleProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("PressedScale", 1.0);
        FadeInProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, bool>("FadeIn", defaultValue: false);
        FadeInScaleProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("FadeInScale", 0.94);
        VisibleProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, bool>("Visible", defaultValue: true);
        HiddenScaleProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("HiddenScale", 0.96);
        DisabledOpacityProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("DisabledOpacity", 1.0);
        DisabledScaleProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("DisabledScale", 1.0);
        SquishProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, bool>("Squish", defaultValue: false);
        SquishIntensityProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("SquishIntensity", 1.0);
        SquishDepthProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, double>("SquishDepth", 1.0);
        SquishTiltProperty = AvaloniaProperty.RegisterAttached<LuminaMotion, Control, bool>("SquishTilt", defaultValue: true);
        AnimateLayoutProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyOrClear(control);
        });
        AnimateSizeProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyOrClear(control);
        });
        AnimateOpacityProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyOrClear(control);
        });
        DurationProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyOrClear(control);
        });
        EasingProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyOrClear(control);
        });
        TranslateXProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyTransformTarget(control);
        });
        TranslateYProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyTransformTarget(control);
        });
        ScaleProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyTransformTarget(control);
        });
        RotationProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyTransformTarget(control);
        });
        HoverScaleProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyPointerHooks(control);
        });
        PressedScaleProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyPointerHooks(control);
        });
        FadeInProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyFadeIn(control);
        });
        FadeInScaleProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyFadeIn(control);
        });
        VisibleProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyVisibility(control);
        });
        HiddenScaleProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyVisibility(control);
        });
        DisabledOpacityProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyDisabledState(control);
        });
        DisabledScaleProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyDisabledState(control);
        });
        InputElement.IsEnabledProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplyDisabledState(control);
        });
        SquishProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplySquishHooks(control);
        });
        SquishIntensityProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplySquishHooks(control);
        });
        SquishDepthProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplySquishHooks(control);
        });
        SquishTiltProperty.Changed.AddClassHandler((Control control, AvaloniaPropertyChangedEventArgs _) =>
        {
            ApplySquishHooks(control);
        });
    }

    internal static (double X, double Y, double ScaleX, double ScaleY) GetCurrentTransform(Control control)
    {
        ArgumentNullException.ThrowIfNull(control, "control");
        MotionState state = GetOrCreateState(control);
        return (X: state.TranslateX, Y: state.TranslateY, ScaleX: state.ScaleX, ScaleY: state.ScaleY);
    }

    internal static MotionTransforms EnsureTransforms(Control control)
    {
        ArgumentNullException.ThrowIfNull(control, "control");
        MotionState state = GetOrCreateState(control);
        if (state.Scale != null && state.Rotate != null && state.Skew != null && state.Translate != null)
        {
            return new MotionTransforms(state.Scale, state.Rotate, state.Skew, state.Translate);
        }
        ScaleTransform scale = state.Scale ?? new ScaleTransform(state.ScaleX, state.ScaleY);
        RotateTransform rotate = state.Rotate ?? new RotateTransform(state.Rotation);
        SkewTransform skew = state.Skew ?? new SkewTransform();
        TranslateTransform translate = state.Translate ?? new TranslateTransform(state.TranslateX, state.TranslateY);
        if (control.RenderTransform is TransformGroup group)
        {
            if (!group.Children.Contains(scale))
            {
                group.Children.Add(scale);
            }
            if (!group.Children.Contains(rotate))
            {
                InsertBefore(group, rotate, skew);
            }
            if (!group.Children.Contains(skew))
            {
                InsertBefore(group, skew, translate);
            }
            if (!group.Children.Contains(translate))
            {
                group.Children.Add(translate);
            }
        }
        else
        {
            TransformGroup group2 = new TransformGroup();
            if (control.RenderTransform is Transform existingTransform)
            {
                group2.Children.Add(existingTransform);
            }
            group2.Children.Add(scale);
            group2.Children.Add(rotate);
            group2.Children.Add(skew);
            group2.Children.Add(translate);
            control.RenderTransform = group2;
        }
        control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        state.Scale = scale;
        state.Rotate = rotate;
        state.Skew = skew;
        state.Translate = translate;
        return new MotionTransforms(scale, rotate, skew, translate);
    }

    private static void InsertBefore(TransformGroup group, Transform transform, Transform before)
    {
        int index = group.Children.IndexOf(before);
        if (index >= 0)
        {
            group.Children.Insert(index, transform);
        }
        else
        {
            group.Children.Add(transform);
        }
    }

    internal static void SetVisualTransform(Control control, double x, double y, double scaleX, double scaleY)
    {
        ArgumentNullException.ThrowIfNull(control, "control");
        MotionState state = GetOrCreateState(control);
        SetVisualTransform(control, x, y, scaleX, scaleY, state.Rotation);
    }

    internal static void SetVisualTransform(Control control, double x, double y, double scaleX, double scaleY, double rotation)
    {
        ArgumentNullException.ThrowIfNull(control, "control");
        SetVisualTransform(control, x, y, scaleX, scaleY, rotation, 0.0, 0.0);
    }

    private static void SetVisualTransform(Control control, double x, double y, double scaleX, double scaleY, double skewX, double skewY)
    {
        MotionState state = GetOrCreateState(control);
        SetVisualTransform(control, x, y, scaleX, scaleY, state.Rotation, skewX, skewY);
    }

    private static void SetVisualTransform(Control control, double x, double y, double scaleX, double scaleY, double rotation, double skewX, double skewY)
    {
        ArgumentNullException.ThrowIfNull(control, "control");
        MotionState state = GetOrCreateState(control);
        MotionTransforms transforms = EnsureTransforms(control);
        transforms.Translate.X = x;
        transforms.Translate.Y = y;
        transforms.Scale.ScaleX = scaleX;
        transforms.Scale.ScaleY = scaleY;
        transforms.Rotate.Angle = rotation;
        transforms.Skew.AngleX = skewX;
        transforms.Skew.AngleY = skewY;
        state.TranslateX = x;
        state.TranslateY = y;
        state.ScaleX = scaleX;
        state.ScaleY = scaleY;
        state.Rotation = rotation;
    }

    internal static async Task AnimateVisualTransformAsync(Control control, double x, double y, double scaleX, double scaleY, TimeSpan? duration = null, Easing? easing = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(control, "control");
        MotionState state = GetOrCreateState(control);
        await AnimateVisualTransformAsync(control, x, y, scaleX, scaleY, state.Rotation, duration, easing, cancellationToken);
    }

    internal static async Task AnimateVisualTransformAsync(Control control, double x, double y, double scaleX, double scaleY, double rotation, TimeSpan? duration = null, Easing? easing = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(control, "control");
        TopLevel? topLevel = null;
        if (control.IsAttachedToVisualTree())
        {
            topLevel = TopLevel.GetTopLevel(control);
        }
        if (topLevel == null)
        {
            SetVisualTransform(control, x, y, scaleX, scaleY, rotation);
            return;
        }
        MotionState state = GetOrCreateState(control);
        MotionTransforms transforms = EnsureTransforms(control);
        double fromX = state.TranslateX;
        double fromY = state.TranslateY;
        double fromScaleX = state.ScaleX;
        double fromScaleY = state.ScaleY;
        double fromRotation = state.Rotation;
        TimeSpan animationDuration = duration ?? TimeSpan.FromMilliseconds(280);
        Easing animationEasing = easing ?? CreateEasing(control);
        TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool hasStarted = false;
        TimeSpan start = TimeSpan.Zero;
        using (cancellationToken.Register(() => {
            completion.TrySetCanceled(cancellationToken);
        }))
        {
            topLevel.RequestAnimationFrame(Step);
            await completion.Task;
        }
        void Step(TimeSpan timestamp)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            else if (!control.IsAttachedToVisualTree())
            {
                completion.TrySetCanceled(cancellationToken);
            }
            else
            {
                if (!hasStarted)
                {
                    hasStarted = true;
                    start = timestamp;
                }
                TimeSpan elapsed = timestamp - start;
                double rawProgress = (animationDuration <= TimeSpan.Zero) ? 1.0 : Math.Clamp(elapsed.TotalMilliseconds / animationDuration.TotalMilliseconds, 0.0, 1.0);
                double progress = animationEasing.Ease(rawProgress);
                double currentX = Lerp(fromX, x, progress);
                double currentY = Lerp(fromY, y, progress);
                double currentScaleX = Lerp(fromScaleX, scaleX, progress);
                double currentScaleY = Lerp(fromScaleY, scaleY, progress);
                double currentRotation = Lerp(fromRotation, rotation, progress);
                ApplyTransformValues(state, transforms, currentX, currentY, currentScaleX, currentScaleY, currentRotation, 0.0, 0.0);
                if (rawProgress >= 1.0)
                {
                    ApplyTransformValues(state, transforms, x, y, scaleX, scaleY, rotation, 0.0, 0.0);
                    completion.TrySetResult();
                }
                else
                {
                    topLevel.RequestAnimationFrame(Step);
                }
            }
        }
    }

    private static void ApplyOrClear(Control control)
    {
        if (ShouldAnimate(control))
        {
            EnsureHooked(control);
            Apply(control);
        }
        else
        {
            Clear(control);
        }
    }

    private static bool ShouldAnimate(Control control)
    {
        return GetAnimateLayout(control) || GetAnimateSize(control) || GetAnimateOpacity(control);
    }

    private static void EnsureHooked(Control control)
    {
        MotionState state = GetOrCreateState(control);
        if (!state.IsHooked)
        {
            state.IsHooked = true;
            control.AttachedToVisualTree += OnAttachedToVisualTree;
            control.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }
    }

    private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control control)
        {
            if (ShouldAnimate(control))
            {
                Apply(control);
            }
            ApplyFadeIn(control);
            ApplyVisibility(control);
            ApplyDisabledState(control);
            ApplyTransformTarget(control);
            ApplyPointerHooks(control);
            ApplySquishHooks(control);
        }
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control control)
        {
            CancelTransformAnimation(control);
            CancelFadeAnimation(control);
            CancelVisibilityAnimation(control);
            CancelDisabledAnimation(control);
            Clear(control);
        }
    }

    private static void Apply(Control control)
    {
        ApplyPropertyTransitions(control);
        CompositionVisual? visual = ElementComposition.GetElementVisual(control);
        if (visual != null)
        {
            Compositor compositor = visual.Compositor;
            ImplicitAnimationCollection animations = compositor.CreateImplicitAnimationCollection();
            TimeSpan duration = TimeSpan.FromMilliseconds(Math.Clamp(GetDuration(control), 1.0, 10000.0));
            if (GetAnimateLayout(control))
            {
                AddOffsetAnimation(compositor, animations, duration);
            }
            if (GetAnimateSize(control))
            {
                AddSizeAnimation(compositor, animations, duration);
            }
            if (GetAnimateOpacity(control))
            {
                AddOpacityAnimation(compositor, animations, duration);
            }
            visual.ImplicitAnimations = animations;
        }
    }

    private static void Clear(Control control)
    {
        RemovePropertyTransitions(control);
        CompositionVisual? visual = ElementComposition.GetElementVisual(control);
        if (visual != null)
        {
            visual.ImplicitAnimations = null;
        }
    }

    private static void AddOffsetAnimation(Compositor compositor, ImplicitAnimationCollection animations, TimeSpan duration)
    {
        CompositionAnimationGroup group = compositor.CreateAnimationGroup();
        Vector3KeyFrameAnimation animation = compositor.CreateVector3KeyFrameAnimation();
        animation.Target = "Offset";
        animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        animation.Duration = duration;
        group.Add(animation);
        animations["Offset"] = group;
    }

    private static void AddSizeAnimation(Compositor compositor, ImplicitAnimationCollection animations, TimeSpan duration)
    {
        CompositionAnimationGroup group = compositor.CreateAnimationGroup();
        Vector2KeyFrameAnimation animation = compositor.CreateVector2KeyFrameAnimation();
        animation.Target = "Size";
        animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        animation.Duration = duration;
        group.Add(animation);
        animations["Size"] = group;
    }

    private static void AddOpacityAnimation(Compositor compositor, ImplicitAnimationCollection animations, TimeSpan duration)
    {
        CompositionAnimationGroup group = compositor.CreateAnimationGroup();
        ScalarKeyFrameAnimation animation = compositor.CreateScalarKeyFrameAnimation();
        animation.Target = "Opacity";
        animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        animation.Duration = duration;
        group.Add(animation);
        animations["Opacity"] = group;
    }

    private static MotionState GetOrCreateState(Control control)
    {
        MotionState? state = control.GetValue(StateProperty);
        if (state != null)
        {
            return state;
        }
        state = new MotionState();
        control.SetValue(StateProperty, state);
        return state;
    }

    private static double Lerp(double from, double to, double progress)
    {
        return from + (to - from) * progress;
    }

    private static void ApplyTransformValues(MotionState state, MotionTransforms transforms, double x, double y, double scaleX, double scaleY, double rotation, double skewX, double skewY)
    {
        transforms.Translate.X = x;
        transforms.Translate.Y = y;
        transforms.Scale.ScaleX = scaleX;
        transforms.Scale.ScaleY = scaleY;
        transforms.Rotate.Angle = rotation;
        transforms.Skew.AngleX = skewX;
        transforms.Skew.AngleY = skewY;
        state.TranslateX = x;
        state.TranslateY = y;
        state.ScaleX = scaleX;
        state.ScaleY = scaleY;
        state.Rotation = rotation;
    }

    private static void ApplyTransformTarget(Control control)
    {
        if (HasTransformTarget(control) || control.GetValue(StateProperty) != null)
        {
            EnsureHooked(control);
            if (!control.IsAttachedToVisualTree())
            {
                SetVisualTransform(control, GetTranslateX(control), GetTranslateY(control), GetScale(control), GetScale(control), GetRotation(control));
            }
            else
            {
                _ = AnimateTransformTargetAsync(control);
            }
        }
    }

    private static async Task AnimateTransformTargetAsync(Control control, double? pointerScale = null)
    {
        MotionState state = GetOrCreateState(control);
        state.PointerScale = pointerScale;
        state.TransformAnimation?.Cancel();
        state.TransformAnimation?.Dispose();
        state.TransformAnimation = new CancellationTokenSource();
        try
        {
            double targetScale = pointerScale ?? GetScale(control);
            await AnimateVisualTransformAsync(control, GetTranslateX(control), GetTranslateY(control), targetScale, targetScale, GetRotation(control), GetMotionDuration(control), CreateEasing(control), state.TransformAnimation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void ApplyPointerHooks(Control control)
    {
        if (HasPointerScale(control))
        {
            EnsureHooked(control);
            MotionState state = GetOrCreateState(control);
            if (!state.IsPointerHooked)
            {
                state.IsPointerHooked = true;
                control.AddHandler(InputElement.PointerEnteredEvent, OnPointerEntered, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
                control.AddHandler(InputElement.PointerExitedEvent, OnPointerExited, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
                control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
                control.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            }
        }
    }

    private static void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control control && GetHoverScale(control) > 0.0)
        {
            _ = AnimateTransformTargetAsync(control, GetHoverScale(control));
        }
    }

    private static void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Control control)
        {
            _ = AnimateTransformTargetAsync(control);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && GetPressedScale(control) > 0.0)
        {
            _ = AnimateTransformTargetAsync(control, GetPressedScale(control));
        }
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control control)
        {
            double targetScale = (control.IsPointerOver && GetHoverScale(control) > 0.0) ? GetHoverScale(control) : GetScale(control);
            _ = AnimateTransformTargetAsync(control, targetScale);
        }
    }

    private static bool HasTransformTarget(Control control)
    {
        return Math.Abs(GetTranslateX(control)) > 0.001 || Math.Abs(GetTranslateY(control)) > 0.001 || Math.Abs(GetScale(control) - 1.0) > 0.001 || Math.Abs(GetRotation(control)) > 0.001;
    }

    private static bool HasPointerScale(Control control)
    {
        return Math.Abs(GetHoverScale(control) - 1.0) > 0.001 || Math.Abs(GetPressedScale(control) - 1.0) > 0.001;
    }

    private static TimeSpan GetMotionDuration(Control control)
    {
        return TimeSpan.FromMilliseconds(Math.Clamp(GetDuration(control), 1.0, 10000.0));
    }

    private static void CancelTransformAnimation(Control control)
    {
        MotionState? state = control.GetValue(StateProperty);
        state?.TransformAnimation?.Cancel();
        state?.TransformAnimation?.Dispose();
        if (state != null)
        {
            state.TransformAnimation = null;
        }
    }

    private static void CancelFadeAnimation(Control control)
    {
        MotionState? state = control.GetValue(StateProperty);
        state?.FadeAnimation?.Cancel();
        state?.FadeAnimation?.Dispose();
        if (state != null)
        {
            state.FadeAnimation = null;
        }
    }

    private static void CancelVisibilityAnimation(Control control)
    {
        MotionState? state = control.GetValue(StateProperty);
        state?.VisibilityAnimation?.Cancel();
        state?.VisibilityAnimation?.Dispose();
        if (state != null)
        {
            state.VisibilityAnimation = null;
        }
    }

    private static void CancelDisabledAnimation(Control control)
    {
        MotionState? state = control.GetValue(StateProperty);
        state?.DisabledAnimation?.Cancel();
        state?.DisabledAnimation?.Dispose();
        if (state != null)
        {
            state.DisabledAnimation = null;
        }
    }

    private static void ApplyFadeIn(Control control)
    {
        if (!GetFadeIn(control))
        {
            return;
        }
        EnsureHooked(control);
        if (control.IsAttachedToVisualTree())
        {
            MotionState state = GetOrCreateState(control);
            if (!state.HasRunFadeIn)
            {
                state.HasRunFadeIn = true;
                _ = RunFadeInAsync(control);
            }
        }
    }

    private static async Task RunFadeInAsync(Control control)
    {
        MotionState state = GetOrCreateState(control);
        state.FadeAnimation?.Cancel();
        state.FadeAnimation?.Dispose();
        state.FadeAnimation = new CancellationTokenSource();
        CancellationToken token = state.FadeAnimation.Token;
        (double X, double Y, double ScaleX, double ScaleY) current = GetCurrentTransform(control);
        double targetScale = GetScale(control);
        double fadeScale = Math.Clamp(GetFadeInScale(control), 0.0, Math.Max(1.0, targetScale));
        try
        {
            control.Opacity = 0.0;
            SetVisualTransform(control, current.X, current.Y, fadeScale, fadeScale);
            await Task.WhenAll(
                control.AnimateAsync(Visual.OpacityProperty, 0.0, 1.0, GetMotionDuration(control), CreateEasing(control), token),
                AnimateVisualTransformAsync(control, current.X, current.Y, targetScale, targetScale, GetMotionDuration(control), CreateEasing(control), token));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void ApplyVisibility(Control control)
    {
        EnsureHooked(control);
        if (!control.IsAttachedToVisualTree())
        {
            ApplyVisibilityInstant(control);
        }
        else
        {
            _ = AnimateVisibilityAsync(control, GetVisible(control));
        }
    }

    private static void ApplyVisibilityInstant(Control control)
    {
        bool visible = GetVisible(control);
        double scale = visible ? GetScale(control) : Math.Clamp(GetHiddenScale(control), 0.0, GetScale(control));
        control.IsVisible = visible;
        control.Opacity = visible ? 1 : 0;
        SetVisualTransform(control, GetTranslateX(control), GetTranslateY(control), scale, scale);
    }

    private static async Task AnimateVisibilityAsync(Control control, bool visible)
    {
        MotionState state = GetOrCreateState(control);
        state.VisibilityAnimation?.Cancel();
        state.VisibilityAnimation?.Dispose();
        state.VisibilityAnimation = new CancellationTokenSource();
        CancellationToken token = state.VisibilityAnimation.Token;
        double hiddenScale = Math.Clamp(GetHiddenScale(control), 0.0, GetScale(control));
        double targetScale = visible ? GetScale(control) : hiddenScale;
        double targetOpacity = visible ? 1.0 : 0.0;
        try
        {
            if (visible)
            {
                control.IsVisible = true;
                if (control.Opacity <= 0.001)
                {
                    SetVisualTransform(control, GetTranslateX(control), GetTranslateY(control), hiddenScale, hiddenScale);
                }
            }
            await Task.WhenAll(
                control.AnimateAsync(Visual.OpacityProperty, control.Opacity, targetOpacity, GetMotionDuration(control), CreateEasing(control), token),
                AnimateVisualTransformAsync(control, GetTranslateX(control), GetTranslateY(control), targetScale, targetScale, GetMotionDuration(control), CreateEasing(control), token));
            if (!visible)
            {
                control.IsVisible = false;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void ApplyDisabledState(Control control)
    {
        if (HasDisabledMotion(control))
        {
            EnsureHooked(control);
            if (!control.IsAttachedToVisualTree())
            {
                bool disabled = !control.IsEnabled;
                control.Opacity = disabled ? GetDisabledOpacity(control) : 1.0;
                double scale = disabled ? GetDisabledScale(control) : GetScale(control);
                SetVisualTransform(control, GetTranslateX(control), GetTranslateY(control), scale, scale);
            }
            else
            {
                _ = AnimateDisabledStateAsync(control);
            }
        }
    }

    private static async Task AnimateDisabledStateAsync(Control control)
    {
        MotionState state = GetOrCreateState(control);
        state.DisabledAnimation?.Cancel();
        state.DisabledAnimation?.Dispose();
        state.DisabledAnimation = new CancellationTokenSource();
        CancellationToken token = state.DisabledAnimation.Token;
        bool disabled = !control.IsEnabled;
        double targetOpacity = disabled ? GetDisabledOpacity(control) : 1.0;
        double targetScale = disabled ? GetDisabledScale(control) : GetScale(control);
        try
        {
            await Task.WhenAll(
                control.AnimateAsync(Visual.OpacityProperty, control.Opacity, targetOpacity, GetMotionDuration(control), CreateEasing(control), token),
                AnimateVisualTransformAsync(control, GetTranslateX(control), GetTranslateY(control), targetScale, targetScale, GetMotionDuration(control), CreateEasing(control), token));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool HasDisabledMotion(Control control)
    {
        return Math.Abs(GetDisabledOpacity(control) - 1.0) > 0.001 || Math.Abs(GetDisabledScale(control) - 1.0) > 0.001;
    }

    private static void ApplySquishHooks(Control control)
    {
        if (GetSquish(control))
        {
            EnsureHooked(control);
            EnsureTransforms(control);
            MotionState state = GetOrCreateState(control);
            if (!state.IsSquishHooked)
            {
                state.IsSquishHooked = true;
                control.AddHandler(InputElement.PointerMovedEvent, OnSquishPointerMoved, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
                control.AddHandler(InputElement.PointerExitedEvent, OnSquishPointerExited, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            }
        }
    }

    private static void OnSquishPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is Control control && GetSquish(control) && !(control.Bounds.Width <= 0.0) && !(control.Bounds.Height <= 0.0))
        {
            Point position = e.GetPosition(control);
            double centerX = control.Bounds.Width / 2.0;
            double centerY = control.Bounds.Height / 2.0;
            double dx = position.X - centerX;
            double dy = position.Y - centerY;
            double normalizedX = (centerX <= 0.0) ? 0.0 : Math.Clamp(dx / centerX, -1.0, 1.0);
            double normalizedY = (centerY <= 0.0) ? 0.0 : Math.Clamp(dy / centerY, -1.0, 1.0);
            double intensity = Math.Clamp(GetSquishIntensity(control), 0.0, 4.0);
            double depth = Math.Clamp(GetSquishDepth(control), 0.0, 4.0);
            double translateX = normalizedX * 5.0 * intensity;
            double translateY = normalizedY * 5.0 * intensity;
            double scaleX = GetScale(control) * (1.0 - Math.Abs(normalizedY) * 0.035 * intensity);
            double scaleY = GetScale(control) * (1.0 - Math.Abs(normalizedX) * 0.035 * intensity);
            double skewX = GetSquishTilt(control) ? ((0.0 - normalizedY) * 2.5 * depth) : 0.0;
            double skewY = GetSquishTilt(control) ? (normalizedX * 2.5 * depth) : 0.0;
            SetVisualTransform(control, GetTranslateX(control) + translateX, GetTranslateY(control) + translateY, scaleX, scaleY, skewX, skewY);
        }
    }

    private static void OnSquishPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Control control && GetSquish(control))
        {
            _ = AnimateSquishResetAsync(control);
        }
    }

    private static async Task AnimateSquishResetAsync(Control control)
    {
        MotionState state = GetOrCreateState(control);
        state.TransformAnimation?.Cancel();
        state.TransformAnimation?.Dispose();
        state.TransformAnimation = new CancellationTokenSource();
        try
        {
            await AnimateVisualTransformAsync(control, GetTranslateX(control), GetTranslateY(control), GetScale(control), GetScale(control), GetMotionDuration(control) * 1.8, new LuminaSpringEase
            {
                Damping = 7.0,
                Stiffness = 44.0
            }, state.TransformAnimation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void ApplyPropertyTransitions(Control control)
    {
        MotionState state = GetOrCreateState(control);
        RemovePropertyTransitions(control);
        TimeSpan duration = GetMotionDuration(control);
        Transitions transitions = control.Transitions ?? new Transitions();
        List<ITransition> installed = new List<ITransition>();
        if (GetAnimateSize(control))
        {
            installed.Add(new DoubleTransition
            {
                Property = Layoutable.WidthProperty,
                Duration = duration,
                Easing = new CubicEaseOut()
            });
            installed.Add(new DoubleTransition
            {
                Property = Layoutable.HeightProperty,
                Duration = duration,
                Easing = new CubicEaseOut()
            });
        }
        if (GetAnimateOpacity(control))
        {
            installed.Add(new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = new CubicEaseOut()
            });
        }
        foreach (ITransition transition in installed)
        {
            transitions.Add(transition);
        }
        if (installed.Count > 0)
        {
            control.Transitions = transitions;
            state.InstalledTransitions = installed;
        }
    }

    private static void RemovePropertyTransitions(Control control)
    {
        MotionState? state = control.GetValue(StateProperty);
        if (state == null || state.InstalledTransitions == null || state.InstalledTransitions.Count <= 0)
        {
            return;
        }
        List<ITransition> installed = state.InstalledTransitions;
        Transitions? transitions = control.Transitions;
        if (transitions == null)
        {
            return;
        }
        foreach (ITransition transition in installed)
        {
            transitions.Remove(transition);
        }
        state.InstalledTransitions = null;
    }

    private static Easing CreateEasing(Control control)
    {
        LuminaMotionEasingKind easing = GetEasing(control);
        Easing result = easing switch
        {
            LuminaMotionEasingKind.CubicInOut => new CubicEaseInOut(), 
            LuminaMotionEasingKind.BackOutSoft => new LuminaBackEaseOut
            {
                Intensity = LuminaMotionEasingIntensity.Soft
            }, 
            LuminaMotionEasingKind.BackOut => new LuminaBackEaseOut(), 
            LuminaMotionEasingKind.BackOutStrong => new LuminaBackEaseOut
            {
                Intensity = LuminaMotionEasingIntensity.Strong
            }, 
            LuminaMotionEasingKind.SpringSoft => new LuminaSpringEase
            {
                Damping = 10.0,
                Stiffness = 32.0
            }, 
            LuminaMotionEasingKind.Spring => new LuminaSpringEase
            {
                Damping = 8.0,
                Stiffness = 44.0
            }, 
            LuminaMotionEasingKind.SpringSnappy => new LuminaSpringEase
            {
                Damping = 6.0,
                Stiffness = 58.0
            }, 
            _ => new CubicEaseOut(), 
        };
        return result;
    }
}
