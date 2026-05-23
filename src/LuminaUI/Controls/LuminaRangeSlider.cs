using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaRangeSlider : TemplatedControl
{
	private enum ActiveThumb
	{
		Lower,
		Upper
	}

	private bool _isDragging;

	private bool _isPointerOver;

	private bool _isCoercing;

	private ActiveThumb _activeThumb = ActiveThumb.Lower;

	public static readonly StyledProperty<double> MinimumProperty;

	public static readonly StyledProperty<double> MaximumProperty;

	public static readonly StyledProperty<double> LowerValueProperty;

	public static readonly StyledProperty<double> UpperValueProperty;

	public static readonly StyledProperty<double> StepProperty;

	public static readonly StyledProperty<bool> IsSnapToStepProperty;

	public static readonly StyledProperty<double> TrackHeightProperty;

	public static readonly StyledProperty<double> ThumbSizeProperty;

	public static readonly StyledProperty<IBrush?> TrackBrushProperty;

	public static readonly StyledProperty<IBrush?> SelectionBrushProperty;

	public static readonly StyledProperty<IBrush?> ThumbBrushProperty;

	public static readonly StyledProperty<IBrush?> ThumbBorderBrushProperty;

	public static readonly StyledProperty<ICommand?> RangeChangedCommandProperty;

	public double Minimum
	{
		get
		{
			return GetValue(MinimumProperty);
		}
		set
		{
			SetValue(MinimumProperty, value);
		}
	}

	public double Maximum
	{
		get
		{
			return GetValue(MaximumProperty);
		}
		set
		{
			SetValue(MaximumProperty, value);
		}
	}

	public double LowerValue
	{
		get
		{
			return GetValue(LowerValueProperty);
		}
		set
		{
			SetValue(LowerValueProperty, value);
		}
	}

	public double UpperValue
	{
		get
		{
			return GetValue(UpperValueProperty);
		}
		set
		{
			SetValue(UpperValueProperty, value);
		}
	}

	public double Step
	{
		get
		{
			return GetValue(StepProperty);
		}
		set
		{
			SetValue(StepProperty, value);
		}
	}

	public bool IsSnapToStep
	{
		get
		{
			return GetValue(IsSnapToStepProperty);
		}
		set
		{
			SetValue(IsSnapToStepProperty, value);
		}
	}

	public double TrackHeight
	{
		get
		{
			return GetValue(TrackHeightProperty);
		}
		set
		{
			SetValue(TrackHeightProperty, value);
		}
	}

	public double ThumbSize
	{
		get
		{
			return GetValue(ThumbSizeProperty);
		}
		set
		{
			SetValue(ThumbSizeProperty, value);
		}
	}

	public IBrush? TrackBrush
	{
		get
		{
			return GetValue(TrackBrushProperty);
		}
		set
		{
			SetValue(TrackBrushProperty, value);
		}
	}

	public IBrush? SelectionBrush
	{
		get
		{
			return GetValue(SelectionBrushProperty);
		}
		set
		{
			SetValue(SelectionBrushProperty, value);
		}
	}

	public IBrush? ThumbBrush
	{
		get
		{
			return GetValue(ThumbBrushProperty);
		}
		set
		{
			SetValue(ThumbBrushProperty, value);
		}
	}

	public IBrush? ThumbBorderBrush
	{
		get
		{
			return GetValue(ThumbBorderBrushProperty);
		}
		set
		{
			SetValue(ThumbBorderBrushProperty, value);
		}
	}

	public ICommand? RangeChangedCommand
	{
		get
		{
			return GetValue(RangeChangedCommandProperty);
		}
		set
		{
			SetValue(RangeChangedCommandProperty, value);
		}
	}

	static LuminaRangeSlider()
	{
		MinimumProperty = AvaloniaProperty.Register<LuminaRangeSlider, double>("Minimum", 0.0);
		MaximumProperty = AvaloniaProperty.Register<LuminaRangeSlider, double>("Maximum", 100.0);
		LowerValueProperty = AvaloniaProperty.Register<LuminaRangeSlider, double>("LowerValue", 0.0, inherits: false, BindingMode.TwoWay);
		UpperValueProperty = AvaloniaProperty.Register<LuminaRangeSlider, double>("UpperValue", 100.0, inherits: false, BindingMode.TwoWay);
		StepProperty = AvaloniaProperty.Register<LuminaRangeSlider, double>("Step", 1.0);
		IsSnapToStepProperty = AvaloniaProperty.Register<LuminaRangeSlider, bool>("IsSnapToStep", defaultValue: false);
		TrackHeightProperty = AvaloniaProperty.Register<LuminaRangeSlider, double>("TrackHeight", 8.0);
		ThumbSizeProperty = AvaloniaProperty.Register<LuminaRangeSlider, double>("ThumbSize", 18.0);
		TrackBrushProperty = AvaloniaProperty.Register<LuminaRangeSlider, IBrush?>("TrackBrush");
		SelectionBrushProperty = AvaloniaProperty.Register<LuminaRangeSlider, IBrush?>("SelectionBrush");
		ThumbBrushProperty = AvaloniaProperty.Register<LuminaRangeSlider, IBrush?>("ThumbBrush");
		ThumbBorderBrushProperty = AvaloniaProperty.Register<LuminaRangeSlider, IBrush?>("ThumbBorderBrush");
		RangeChangedCommandProperty = AvaloniaProperty.Register<LuminaRangeSlider, ICommand?>("RangeChangedCommand");
		Visual.AffectsRender<LuminaRangeSlider>(new AvaloniaProperty[10] { MinimumProperty, MaximumProperty, LowerValueProperty, UpperValueProperty, TrackHeightProperty, ThumbSizeProperty, TrackBrushProperty, SelectionBrushProperty, ThumbBrushProperty, ThumbBorderBrushProperty });
		Layoutable.AffectsMeasure<LuminaRangeSlider>(new AvaloniaProperty[2] { TrackHeightProperty, ThumbSizeProperty });
		InputElement.FocusableProperty.OverrideDefaultValue<LuminaRangeSlider>(defaultValue: true);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(0.0, Math.Max(ThumbSize, TrackHeight));
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == MinimumProperty || change.Property == MaximumProperty || change.Property == LowerValueProperty || change.Property == UpperValueProperty || change.Property == StepProperty || change.Property == IsSnapToStepProperty)
		{
			CoerceRange();
			InvalidateVisual();
		}
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		Rect trackRect = GetTrackRect();
		if (!(trackRect.Width <= 0.0) && !(trackRect.Height <= 0.0))
		{
			double radius = trackRect.Height / 2.0;
			double lowerRatio = ToRatio(LowerValue);
			double upperRatio = ToRatio(UpperValue);
			double selectionX = trackRect.X + trackRect.Width * lowerRatio;
			double selectionWidth = Math.Max(trackRect.Height, trackRect.Width * (upperRatio - lowerRatio));
			selectionWidth = Math.Min(selectionWidth, trackRect.Right - selectionX);
			context.DrawRectangle(TrackBrush ?? Brushes.Gray, null, trackRect, radius, radius);
			context.DrawRectangle(SelectionBrush ?? Brushes.DodgerBlue, null, new Rect(selectionX, trackRect.Y, selectionWidth, trackRect.Height), radius, radius);
			DrawThumb(context, trackRect, lowerRatio, _activeThumb == ActiveThumb.Lower);
			DrawThumb(context, trackRect, upperRatio, _activeThumb == ActiveThumb.Upper);
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
		if (base.IsEffectivelyEnabled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			Focus();
			_activeThumb = PickThumb(e.GetPosition(this));
			_isDragging = true;
			e.Pointer.Capture(this);
			SetActiveThumbFromPoint(e.GetPosition(this));
			e.Handled = true;
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		if (_isDragging && base.IsEffectivelyEnabled)
		{
			SetActiveThumbFromPoint(e.GetPosition(this));
			e.Handled = true;
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (_isDragging)
		{
			SetActiveThumbFromPoint(e.GetPosition(this));
			ExecuteRangeChangedCommand();
		}
		_isDragging = false;
		e.Pointer.Capture(null);
		InvalidateVisual();
		e.Handled = true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (base.IsEffectivelyEnabled)
		{
			double step = ((Step > 0.0) ? Step : Math.Max(1.0, (Maximum - Minimum) / 100.0));
			bool handled = true;
			switch (e.Key)
			{
			case Key.Left:
			case Key.Down:
				MoveActiveThumb(0.0 - step);
				break;
			case Key.Up:
			case Key.Right:
				MoveActiveThumb(step);
				break;
			case Key.Home:
				SetThumbValue(_activeThumb, Minimum);
				break;
			case Key.End:
				SetThumbValue(_activeThumb, Maximum);
				break;
			case Key.Tab:
				handled = false;
				break;
			default:
				handled = false;
				break;
			}
			if (handled)
			{
				ExecuteRangeChangedCommand();
				e.Handled = true;
			}
		}
	}

	private Rect GetTrackRect()
	{
		double thumbSize = Math.Max(0.0, ThumbSize);
		double trackHeight = Math.Max(1.0, TrackHeight);
		double left = thumbSize / 2.0;
		double width = Math.Max(0.0, base.Bounds.Width - thumbSize);
		double y = Math.Max(0.0, (base.Bounds.Height - trackHeight) / 2.0);
		return new Rect(left, y, width, trackHeight);
	}

	private void SetActiveThumbFromPoint(Point point)
	{
		Rect trackRect = GetTrackRect();
		if (!(trackRect.Width <= 0.0))
		{
			double ratio = Math.Clamp((point.X - trackRect.X) / trackRect.Width, 0.0, 1.0);
			double value = Minimum + (Maximum - Minimum) * ratio;
			SetThumbValue(_activeThumb, value);
		}
	}

	private void MoveActiveThumb(double delta)
	{
		double current = ((_activeThumb == ActiveThumb.Lower) ? LowerValue : UpperValue);
		SetThumbValue(_activeThumb, current + delta);
	}

	private void SetThumbValue(ActiveThumb thumb, double value)
	{
		value = NormalizeValue(value);
		if (thumb == ActiveThumb.Lower)
		{
			SetCurrentValue(LowerValueProperty, Math.Min(value, UpperValue));
		}
		else
		{
			SetCurrentValue(UpperValueProperty, Math.Max(value, LowerValue));
		}
	}

	private ActiveThumb PickThumb(Point point)
	{
		Rect trackRect = GetTrackRect();
		if (trackRect.Width <= 0.0)
		{
			return ActiveThumb.Lower;
		}
		double pointerRatio = Math.Clamp((point.X - trackRect.X) / trackRect.Width, 0.0, 1.0);
		double lowerDistance = Math.Abs(pointerRatio - ToRatio(LowerValue));
		double upperDistance = Math.Abs(pointerRatio - ToRatio(UpperValue));
		return (!(lowerDistance <= upperDistance)) ? ActiveThumb.Upper : ActiveThumb.Lower;
	}

	private double NormalizeValue(double value)
	{
		double min = Math.Min(Minimum, Maximum);
		double max = Math.Max(Minimum, Maximum);
		double clamped = (double.IsNaN(value) ? min : Math.Clamp(value, min, max));
		if (!IsSnapToStep || Step <= 0.0)
		{
			return clamped;
		}
		double steps = Math.Round((clamped - min) / Step);
		return Math.Clamp(min + steps * Step, min, max);
	}

	private void CoerceRange()
	{
		if (_isCoercing)
		{
			return;
		}
		_isCoercing = true;
		try
		{
			double lower = NormalizeValue(LowerValue);
			double upper = NormalizeValue(UpperValue);
			if (lower > upper)
			{
				double num = upper;
				upper = lower;
				lower = num;
			}
			if (!AreClose(lower, LowerValue))
			{
				SetCurrentValue(LowerValueProperty, lower);
			}
			if (!AreClose(upper, UpperValue))
			{
				SetCurrentValue(UpperValueProperty, upper);
			}
		}
		finally
		{
			_isCoercing = false;
		}
	}

	private double ToRatio(double value)
	{
		double min = Math.Min(Minimum, Maximum);
		double max = Math.Max(Minimum, Maximum);
		double range = max - min;
		if (range <= 0.0 || double.IsNaN(value))
		{
			return 0.0;
		}
		return Math.Clamp((value - min) / range, 0.0, 1.0);
	}

	private void DrawThumb(DrawingContext context, Rect trackRect, double ratio, bool isActive)
	{
		double thumbSize = Math.Max(0.0, ThumbSize);
		if (!(thumbSize <= 0.0))
		{
			Point center = new Point(trackRect.X + trackRect.Width * ratio, base.Bounds.Height / 2.0);
			double radius = thumbSize / 2.0;
			bool active = _isPointerOver || _isDragging || (isActive && base.IsKeyboardFocusWithin);
			double visualRadius = (active ? radius : (radius * 0.86));
			if (active)
			{
				context.DrawEllipse(new SolidColorBrush(Color.FromArgb(44, 0, 0, 0)), null, center, radius + 4.0, radius + 4.0);
			}
			context.DrawEllipse(ThumbBrush ?? Brushes.White, new Pen(ThumbBorderBrush ?? SelectionBrush ?? Brushes.DodgerBlue, 1.5), center, visualRadius, visualRadius);
		}
	}

	private void ExecuteRangeChangedCommand()
	{
		LuminaRangeValue value = new LuminaRangeValue(LowerValue, UpperValue);
		ICommand? rangeChangedCommand = RangeChangedCommand;
		if (rangeChangedCommand != null && rangeChangedCommand.CanExecute(value))
		{
			rangeChangedCommand.Execute(value);
		}
	}

	private static bool AreClose(double left, double right)
	{
		return Math.Abs(left - right) < 0.0001;
	}
}
