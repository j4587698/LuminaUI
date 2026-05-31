using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaPlaybackSlider : RangeBase
{
    private bool _isDragging;

    private bool _isPointerOver;

    public static readonly StyledProperty<double> BufferedValueProperty;

    public static readonly StyledProperty<double> TrackHeightProperty;

    public static readonly StyledProperty<double> ThumbSizeProperty;

    public static readonly StyledProperty<double> StepProperty;

    public static readonly StyledProperty<IBrush?> TrackBrushProperty;

    public static readonly StyledProperty<IBrush?> BufferedBrushProperty;

    public static readonly StyledProperty<IBrush?> ProgressBrushProperty;

    public static readonly StyledProperty<IBrush?> ThumbBrushProperty;

    public static readonly StyledProperty<IBrush?> ThumbBorderBrushProperty;

    public double BufferedValue
    {
        get => GetValue(BufferedValueProperty);
        set => SetValue(BufferedValueProperty, value);
    }

    public double TrackHeight
    {
        get => GetValue(TrackHeightProperty);
        set => SetValue(TrackHeightProperty, value);
    }

    public double ThumbSize
    {
        get => GetValue(ThumbSizeProperty);
        set => SetValue(ThumbSizeProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? BufferedBrush
    {
        get => GetValue(BufferedBrushProperty);
        set => SetValue(BufferedBrushProperty, value);
    }

    public IBrush? ProgressBrush
    {
        get => GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }

    public IBrush? ThumbBrush
    {
        get => GetValue(ThumbBrushProperty);
        set => SetValue(ThumbBrushProperty, value);
    }

    public IBrush? ThumbBorderBrush
    {
        get => GetValue(ThumbBorderBrushProperty);
        set => SetValue(ThumbBorderBrushProperty, value);
    }

    static LuminaPlaybackSlider()
    {
        BufferedValueProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, double>(nameof(BufferedValue), 0.0);
        TrackHeightProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, double>(nameof(TrackHeight), 8.0);
        ThumbSizeProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, double>(nameof(ThumbSize), 18.0);
        StepProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, double>(nameof(Step), 1.0);
        TrackBrushProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, IBrush?>(nameof(TrackBrush));
        BufferedBrushProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, IBrush?>(nameof(BufferedBrush));
        ProgressBrushProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, IBrush?>(nameof(ProgressBrush));
        ThumbBrushProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, IBrush?>(nameof(ThumbBrush));
        ThumbBorderBrushProperty = AvaloniaProperty.Register<LuminaPlaybackSlider, IBrush?>(nameof(ThumbBorderBrush));
        Visual.AffectsRender<LuminaPlaybackSlider>(new AvaloniaProperty[11]
        {
            RangeBase.MinimumProperty,
            RangeBase.MaximumProperty,
            RangeBase.ValueProperty,
            BufferedValueProperty,
            TrackHeightProperty,
            ThumbSizeProperty,
            TrackBrushProperty,
            BufferedBrushProperty,
            ProgressBrushProperty,
            ThumbBrushProperty,
            ThumbBorderBrushProperty
        });
        Layoutable.AffectsMeasure<LuminaPlaybackSlider>(new AvaloniaProperty[2] { TrackHeightProperty, ThumbSizeProperty });
        InputElement.FocusableProperty.OverrideDefaultValue<LuminaPlaybackSlider>(defaultValue: true);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double height = Math.Max(ThumbSize, TrackHeight);
        return new Size(0.0, height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        Rect trackRect = GetTrackRect();
        if (!(trackRect.Width <= 0.0) && !(trackRect.Height <= 0.0))
        {
            double radius = trackRect.Height / 2.0;
            DrawRoundedLayer(context, trackRect, 1.0, TrackBrush ?? Brushes.Gray, radius);
            DrawRoundedLayer(context, trackRect, ToRatio(BufferedValue), BufferedBrush ?? Brushes.DarkGray, radius);
            DrawRoundedLayer(context, trackRect, ToRatio(Value), ProgressBrush ?? Brushes.DodgerBlue, radius);
            DrawThumb(context, trackRect);
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (IsEffectivelyEnabled)
        {
            Focus();
            _isDragging = true;
            e.Pointer.Capture(this);
            SetValueFromPoint(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging && IsEffectivelyEnabled)
        {
            SetValueFromPoint(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            SetValueFromPoint(e.GetPosition(this));
        }
        _isDragging = false;
        e.Pointer.Capture(null);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IsEffectivelyEnabled)
        {
            double step = (Step > 0.0) ? Step : Math.Max(1.0, (Maximum - Minimum) / 100.0);
            switch (e.Key)
            {
            case Key.Left:
            case Key.Down:
                Value = Clamp(Value - step);
                e.Handled = true;
                break;
            case Key.Up:
            case Key.Right:
                Value = Clamp(Value + step);
                e.Handled = true;
                break;
            case Key.Home:
                Value = Minimum;
                e.Handled = true;
                break;
            case Key.End:
                Value = Maximum;
                e.Handled = true;
                break;
            }
        }
    }

    private Rect GetTrackRect()
    {
        double thumbSize = Math.Max(0.0, ThumbSize);
        double trackHeight = Math.Max(1.0, TrackHeight);
        double left = thumbSize / 2.0;
        double width = Math.Max(0.0, Bounds.Width - thumbSize);
        double y = Math.Max(0.0, (Bounds.Height - trackHeight) / 2.0);
        return new Rect(left, y, width, trackHeight);
    }

    private void SetValueFromPoint(Point point)
    {
        Rect trackRect = GetTrackRect();
        if (!(trackRect.Width <= 0.0))
        {
            double ratio = Math.Clamp((point.X - trackRect.X) / trackRect.Width, 0.0, 1.0);
            Value = Clamp(Minimum + (Maximum - Minimum) * ratio);
        }
    }

    private double ToRatio(double value)
    {
        double range = Maximum - Minimum;
        if (range <= 0.0 || double.IsNaN(value))
        {
            return 0.0;
        }
        return Math.Clamp((value - Minimum) / range, 0.0, 1.0);
    }

    private double Clamp(double value)
    {
        if (double.IsNaN(value))
        {
            return Minimum;
        }
        return Math.Clamp(value, Minimum, Maximum);
    }

    private static void DrawRoundedLayer(DrawingContext context, Rect trackRect, double ratio, IBrush brush, double radius)
    {
        if (!(ratio <= 0.0))
        {
            double width = Math.Max(trackRect.Height, trackRect.Width * Math.Clamp(ratio, 0.0, 1.0));
            width = Math.Min(width, trackRect.Width);
            Rect rect = new Rect(trackRect.X, trackRect.Y, width, trackRect.Height);
            context.DrawRectangle(brush, null, rect, radius, radius);
        }
    }

    private void DrawThumb(DrawingContext context, Rect trackRect)
    {
        double thumbSize = Math.Max(0.0, ThumbSize);
        if (!(thumbSize <= 0.0))
        {
            double ratio = ToRatio(Value);
            Point center = new Point(trackRect.X + trackRect.Width * ratio, Bounds.Height / 2.0);
            double radius = thumbSize / 2.0;
            bool active = _isPointerOver || _isDragging || IsKeyboardFocusWithin;
            double visualRadius = active ? radius : (radius * 0.78);
            if (active)
            {
                context.DrawEllipse(new SolidColorBrush(Color.FromArgb(46, 0, 0, 0)), null, center, radius + 4.0, radius + 4.0);
            }
            context.DrawEllipse(ThumbBrush ?? Brushes.White, new Pen(ThumbBorderBrush ?? ProgressBrush ?? Brushes.DodgerBlue, 1.5), center, visualRadius, visualRadius);
        }
    }
}
