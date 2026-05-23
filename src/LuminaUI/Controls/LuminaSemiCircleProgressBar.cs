using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaSemiCircleProgressBar : ContentControl
{
	private readonly record struct ArcMetrics(Point Center, double Radius);

	public static readonly StyledProperty<double> MinimumProperty;

	public static readonly StyledProperty<double> MaximumProperty;

	public static readonly StyledProperty<double> ValueProperty;

	public static readonly StyledProperty<double> StrokeThicknessProperty;

	public static readonly StyledProperty<IBrush?> TrackBrushProperty;

	public static readonly StyledProperty<IBrush?> ProgressBrushProperty;

	public static readonly StyledProperty<bool> AutoTextScaleProperty;

	public static readonly StyledProperty<object?> DisplayContentProperty;

	public static readonly StyledProperty<string> DisplayTextProperty;

	public static readonly StyledProperty<bool> HasDefaultContentProperty;

	public static readonly StyledProperty<bool> HasCustomContentProperty;

	public static readonly StyledProperty<double> DisplayFontSizeProperty;

	public static readonly StyledProperty<Thickness> DisplayContentMarginProperty;

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

	public double Value
	{
		get
		{
			return GetValue(ValueProperty);
		}
		set
		{
			SetValue(ValueProperty, value);
		}
	}

	public double StrokeThickness
	{
		get
		{
			return GetValue(StrokeThicknessProperty);
		}
		set
		{
			SetValue(StrokeThicknessProperty, value);
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

	public IBrush? ProgressBrush
	{
		get
		{
			return GetValue(ProgressBrushProperty);
		}
		set
		{
			SetValue(ProgressBrushProperty, value);
		}
	}

	public bool AutoTextScale
	{
		get
		{
			return GetValue(AutoTextScaleProperty);
		}
		set
		{
			SetValue(AutoTextScaleProperty, value);
		}
	}

	public object? DisplayContent
	{
		get
		{
			return GetValue(DisplayContentProperty);
		}
		private set
		{
			SetValue(DisplayContentProperty, value);
		}
	}

	public string DisplayText
	{
		get
		{
			return GetValue(DisplayTextProperty);
		}
		private set
		{
			SetValue(DisplayTextProperty, value);
		}
	}

	public bool HasDefaultContent
	{
		get
		{
			return GetValue(HasDefaultContentProperty);
		}
		private set
		{
			SetValue(HasDefaultContentProperty, value);
		}
	}

	public bool HasCustomContent
	{
		get
		{
			return GetValue(HasCustomContentProperty);
		}
		private set
		{
			SetValue(HasCustomContentProperty, value);
		}
	}

	public double DisplayFontSize
	{
		get
		{
			return GetValue(DisplayFontSizeProperty);
		}
		private set
		{
			SetValue(DisplayFontSizeProperty, value);
		}
	}

	public Thickness DisplayContentMargin
	{
		get
		{
			return GetValue(DisplayContentMarginProperty);
		}
		private set
		{
			SetValue(DisplayContentMarginProperty, value);
		}
	}

	static LuminaSemiCircleProgressBar()
	{
		MinimumProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, double>("Minimum", 0.0);
		MaximumProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, double>("Maximum", 100.0);
		ValueProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, double>("Value", 0.0);
		StrokeThicknessProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, double>("StrokeThickness", 12.0);
		TrackBrushProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, IBrush?>("TrackBrush");
		ProgressBrushProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, IBrush?>("ProgressBrush");
		AutoTextScaleProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, bool>("AutoTextScale", defaultValue: true);
		DisplayContentProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, object?>("DisplayContent");
		DisplayTextProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, string>("DisplayText", "0%");
		HasDefaultContentProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, bool>("HasDefaultContent", defaultValue: true);
		HasCustomContentProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, bool>("HasCustomContent", defaultValue: false);
		DisplayFontSizeProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, double>("DisplayFontSize", 32.0);
		DisplayContentMarginProperty = AvaloniaProperty.Register<LuminaSemiCircleProgressBar, Thickness>("DisplayContentMargin");
		Visual.AffectsRender<LuminaSemiCircleProgressBar>(new AvaloniaProperty[6] { MinimumProperty, MaximumProperty, ValueProperty, StrokeThicknessProperty, TrackBrushProperty, ProgressBrushProperty });
		Layoutable.AffectsMeasure<LuminaSemiCircleProgressBar>(new AvaloniaProperty[1] { StrokeThicknessProperty });
	}

	public LuminaSemiCircleProgressBar()
	{
		UpdateDisplayContent();
		UpdateDisplayMetrics();
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		ArcMetrics metrics = GetArcMetrics();
		if (!(metrics.Radius <= 0.0))
		{
			double thickness = Math.Max(1.0, StrokeThickness);
			Pen trackPen = CreatePen(TrackBrush ?? Brushes.LightGray, thickness);
			Pen progressPen = CreatePen(ProgressBrush ?? Brushes.DodgerBlue, thickness);
			context.DrawGeometry(null, trackPen, CreateArcGeometry(metrics.Center, metrics.Radius, 1.0));
			double progress = GetProgressRatio(Value);
			if (progress > 0.0)
			{
				context.DrawGeometry(null, progressPen, CreateArcGeometry(metrics.Center, metrics.Radius, progress));
			}
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == MinimumProperty || change.Property == MaximumProperty || change.Property == ValueProperty || change.Property == ContentControl.ContentProperty)
		{
			UpdateDisplayContent();
		}
		if (change.Property == Visual.BoundsProperty || change.Property == StrokeThicknessProperty || change.Property == AutoTextScaleProperty || change.Property == TemplatedControl.FontSizeProperty)
		{
			UpdateDisplayMetrics();
		}
	}

	private void UpdateDisplayContent()
	{
		string percentText = GetPercentText();
		bool hasCustomContent = base.Content != null;
		DisplayText = percentText;
		DisplayContent = (hasCustomContent ? base.Content : percentText);
		HasCustomContent = hasCustomContent;
		HasDefaultContent = !hasCustomContent;
	}

	private void UpdateDisplayMetrics()
	{
		ArcMetrics metrics = GetArcMetrics();
		if (metrics.Radius <= 0.0)
		{
			DisplayFontSize = base.FontSize;
			DisplayContentMargin = default(Thickness);
			return;
		}
		DisplayFontSize = (AutoTextScale ? Math.Clamp(metrics.Radius * 0.46, 20.0, 72.0) : base.FontSize);
		double visualCenterY = metrics.Center.Y - metrics.Radius * 0.24;
		double verticalOffset = visualCenterY - base.Bounds.Height / 2.0;
		DisplayContentMargin = new Thickness(0.0, verticalOffset, 0.0, 0.0 - verticalOffset);
	}

	private string GetPercentText()
	{
		return $"{Math.Round(GetProgressRatio(Value) * 100.0):0}%";
	}

	private double GetProgressRatio(double value)
	{
		double range = Maximum - Minimum;
		if (range <= 0.0 || double.IsNaN(value))
		{
			return 0.0;
		}
		return Math.Clamp((value - Minimum) / range, 0.0, 1.0);
	}

	private ArcMetrics GetArcMetrics()
	{
		double thickness = Math.Max(1.0, StrokeThickness);
		double radius = Math.Min(Math.Max(0.0, base.Bounds.Width - thickness) / 2.0, Math.Max(0.0, base.Bounds.Height - thickness));
		return new ArcMetrics(new Point(base.Bounds.Width / 2.0, thickness / 2.0 + radius), radius);
	}

	private static Pen CreatePen(IBrush brush, double thickness)
	{
		return new Pen(brush, thickness, null, PenLineCap.Round, PenLineJoin.Round);
	}

	private static StreamGeometry CreateArcGeometry(Point center, double radius, double ratio)
	{
		double clampedRatio = Math.Clamp(ratio, 0.0, 1.0);
		Point start = new Point(center.X - radius, center.Y);
		Point end = PointOnArc(center, radius, 180.0 + 180.0 * clampedRatio);
		StreamGeometry geometry = new StreamGeometry();
		using StreamGeometryContext context = geometry.Open();
		context.BeginFigure(start, isFilled: false);
		context.ArcTo(end, new Size(radius, radius), 0.0, isLargeArc: false, SweepDirection.Clockwise);
		return geometry;
	}

	private static Point PointOnArc(Point center, double radius, double angle)
	{
		double radians = Math.PI * angle / 180.0;
		return new Point(center.X + Math.Cos(radians) * radius, center.Y + Math.Sin(radians) * radius);
	}
}
