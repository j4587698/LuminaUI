using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaLoadingVisual : Control
{
	private double _animationSeconds;

	private bool _isAttached;

	private bool _isFrameQueued;

	public static readonly StyledProperty<bool> IsActiveProperty;

	public static readonly StyledProperty<LuminaLoadingKind> KindProperty;

	public static readonly StyledProperty<double> SizeProperty;

	public static readonly StyledProperty<double> StrokeThicknessProperty;

	public static readonly StyledProperty<IBrush?> BrushProperty;

	public bool IsActive
	{
		get
		{
			return GetValue(IsActiveProperty);
		}
		set
		{
			SetValue(IsActiveProperty, value);
		}
	}

	public LuminaLoadingKind Kind
	{
		get
		{
			return GetValue(KindProperty);
		}
		set
		{
			SetValue(KindProperty, value);
		}
	}

	public double Size
	{
		get
		{
			return GetValue(SizeProperty);
		}
		set
		{
			SetValue(SizeProperty, value);
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

	public IBrush? Brush
	{
		get
		{
			return GetValue(BrushProperty);
		}
		set
		{
			SetValue(BrushProperty, value);
		}
	}

	static LuminaLoadingVisual()
	{
		IsActiveProperty = AvaloniaProperty.Register<LuminaLoadingVisual, bool>("IsActive", defaultValue: true);
		KindProperty = AvaloniaProperty.Register<LuminaLoadingVisual, LuminaLoadingKind>("Kind", LuminaLoadingKind.Ring);
		SizeProperty = AvaloniaProperty.Register<LuminaLoadingVisual, double>("Size", 22.0);
		StrokeThicknessProperty = AvaloniaProperty.Register<LuminaLoadingVisual, double>("StrokeThickness", 3.0);
		BrushProperty = AvaloniaProperty.Register<LuminaLoadingVisual, IBrush?>("Brush");
		Visual.AffectsRender<LuminaLoadingVisual>(new AvaloniaProperty[5] { IsActiveProperty, KindProperty, SizeProperty, StrokeThicknessProperty, BrushProperty });
		Layoutable.AffectsMeasure<LuminaLoadingVisual>(new AvaloniaProperty[3] { KindProperty, SizeProperty, StrokeThicknessProperty });
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		if (IsActive && !(base.Bounds.Width <= 0.0) && !(base.Bounds.Height <= 0.0))
		{
			IBrush brush = Brush ?? Brushes.DodgerBlue;
			switch (Kind)
			{
			case LuminaLoadingKind.Bar:
				RenderBar(context, brush);
				break;
			case LuminaLoadingKind.Dots:
				RenderDots(context, brush);
				break;
			default:
				RenderRing(context, brush);
				break;
			}
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		LuminaLoadingKind kind = Kind;
		if (1 == 0)
		{
		}
		Size result = kind switch
		{
			LuminaLoadingKind.Bar => new Size(GetBarWidth(), Math.Max(8.0, StrokeThickness * 2.8)), 
			LuminaLoadingKind.Dots => new Size(Math.Max(36.0, Size * 1.8), Math.Max(10.0, Size * 0.55)), 
			_ => new Size(Size, Size), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		_isAttached = true;
		QueueFrame();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_isAttached = false;
		_isFrameQueued = false;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IsActiveProperty)
		{
			if (IsActive)
			{
				QueueFrame();
			}
			else
			{
				InvalidateVisual();
			}
		}
	}

	private void QueueFrame()
	{
		if (!_isFrameQueued && _isAttached && IsActive)
		{
			TopLevel? topLevel = TopLevel.GetTopLevel(this);
			if (topLevel != null)
			{
				_isFrameQueued = true;
				topLevel.RequestAnimationFrame(OnFrame);
			}
		}
	}

	private void OnFrame(TimeSpan timestamp)
	{
		_isFrameQueued = false;
		if (_isAttached && IsActive)
		{
			_animationSeconds = timestamp.TotalSeconds;
			InvalidateVisual();
			QueueFrame();
		}
	}

	private void RenderRing(DrawingContext context, IBrush brush)
	{
		double thickness = Math.Max(1.0, StrokeThickness);
		double size = Math.Min(base.Bounds.Width, base.Bounds.Height);
		Point center = new Point(base.Bounds.Width / 2.0, base.Bounds.Height / 2.0);
		double radius = Math.Max(0.0, (size - thickness * 1.35) / 2.0);
		if (radius <= 0.0)
		{
			return;
		}
		using (context.PushOpacity(0.14))
		{
			context.DrawEllipse(null, CreatePen(brush, thickness * 0.64), center, radius, radius);
		}
		double headAngle = (_animationSeconds * 300.0 + Math.Sin(_animationSeconds * Math.PI * 2.0 * 0.82) * 26.0) % 360.0;
		double sweep = 216.0;
		for (int i = 0; i < 8; i++)
		{
			double t0 = (double)i / 8.0;
			double t1 = (double)(i + 1) / 8.0;
			double segmentStart = headAngle - sweep + sweep * t0;
			double segmentSweep = sweep / 8.0 + 1.2;
			double weight = EaseInOut(t1);
			double segmentThickness = thickness * (0.36 + weight * 0.88);
			double opacity = 0.18 + weight * 0.82;
			using (context.PushOpacity(opacity))
			{
				context.DrawGeometry(null, CreatePen(brush, segmentThickness), CreateArcGeometry(center, radius, segmentStart, segmentSweep));
			}
		}
	}

	private void RenderBar(DrawingContext context, IBrush brush)
	{
		double width = base.Bounds.Width;
		double height = Math.Max(8.0, base.Bounds.Height);
		double trackHeight = Math.Max(2.0, StrokeThickness);
		double radius = trackHeight / 2.0;
		Rect track = new Rect(0.0, (height - trackHeight) / 2.0, width, trackHeight);
		using (context.PushOpacity(0.12))
		{
			context.DrawRectangle(brush, null, track, radius, radius);
		}
		double cycle = _animationSeconds * 0.78 % 1.0;
		double pingPong = ((cycle < 0.5) ? (cycle * 2.0) : (2.0 - cycle * 2.0));
		double progress = EaseInOut(pingPong);
		double pulse = (Math.Sin(_animationSeconds * Math.PI * 2.0 * 0.78) + 1.0) / 2.0;
		double thumbWidth = Math.Clamp(width * (0.28 + pulse * 0.16), 24.0, width * 0.56);
		double x = (width - thumbWidth) * progress;
		Rect thumb = new Rect(x, (height - trackHeight * 1.5) / 2.0, thumbWidth, trackHeight * 1.5);
		context.DrawRectangle(brush, null, thumb, trackHeight * 0.75, trackHeight * 0.75);
	}

	private void RenderDots(DrawingContext context, IBrush brush)
	{
		double width = base.Bounds.Width;
		double centerY = base.Bounds.Height / 2.0;
		double spacing = width / 5.0;
		double baseRadius = Math.Max(2.2, Math.Min(base.Bounds.Height, spacing) * 0.22);
		for (int i = 0; i < 4; i++)
		{
			double phase = (_animationSeconds * 1.35 + (double)i * 0.17) % 1.0;
			double wave = (Math.Sin(phase * Math.PI * 2.0) + 1.0) / 2.0;
			double radius = baseRadius * (0.72 + EaseInOut(wave) * 0.62);
			double opacity = 0.32 + wave * 0.62;
			Point center = new Point(spacing * (double)(i + 1), centerY);
			using (context.PushOpacity(opacity))
			{
				context.DrawEllipse(brush, null, center, radius, radius);
			}
		}
	}

	private double GetBarWidth()
	{
		if (Size >= 48.0)
		{
			return Size;
		}
		return Math.Clamp(Size * 4.2, 54.0, 112.0);
	}

	private static Pen CreatePen(IBrush brush, double thickness)
	{
		return new Pen(brush, thickness, null, PenLineCap.Round, PenLineJoin.Round);
	}

	private static StreamGeometry CreateArcGeometry(Point center, double radius, double startAngle, double sweepAngle)
	{
		Point start = PointOnArc(center, radius, startAngle);
		Point end = PointOnArc(center, radius, startAngle + sweepAngle);
		StreamGeometry geometry = new StreamGeometry();
		using StreamGeometryContext context = geometry.Open();
		context.BeginFigure(start, isFilled: false);
		context.ArcTo(end, new Size(radius, radius), 0.0, Math.Abs(sweepAngle) > 180.0, (sweepAngle >= 0.0) ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
		return geometry;
	}

	private static Point PointOnArc(Point center, double radius, double angle)
	{
		double radians = Math.PI * angle / 180.0;
		return new Point(center.X + Math.Cos(radians) * radius, center.Y + Math.Sin(radians) * radius);
	}

	private static double EaseInOut(double value)
	{
		double clamped = Math.Clamp(value, 0.0, 1.0);
		return clamped * clamped * (3.0 - 2.0 * clamped);
	}
}
