using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

[TemplatePart("PART_Viewport", typeof(Control))]
[TemplatePart("PART_Track", typeof(Control))]
[TemplatePart("PART_PrimaryText", typeof(TextBlock))]
[TemplatePart("PART_SecondaryText", typeof(TextBlock))]
public class LuminaAutoScrollText : TemplatedControl
{
	private readonly DispatcherTimer _timer;

	private readonly TranslateTransform _translate = new TranslateTransform();

	private Control? _viewport;

	private Control? _track;

	private TextBlock? _primaryText;

	private TextBlock? _secondaryText;

	private DateTime _lastTick = DateTime.UtcNow;

	private DateTime _resumeAt = DateTime.UtcNow;

	private double _offset;

	private double _cycleWidth;

	private bool _isLoaded;

	public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<LuminaAutoScrollText>();

	public static readonly StyledProperty<double> SpeedProperty = AvaloniaProperty.Register<LuminaAutoScrollText, double>("Speed", 36.0);

	public static readonly StyledProperty<double> GapProperty = AvaloniaProperty.Register<LuminaAutoScrollText, double>("Gap", 40.0);

	public static readonly StyledProperty<TimeSpan> RepeatDelayProperty = AvaloniaProperty.Register<LuminaAutoScrollText, TimeSpan>("RepeatDelay", TimeSpan.FromSeconds(1.2));

	public static readonly StyledProperty<bool> IsRunningProperty = AvaloniaProperty.Register<LuminaAutoScrollText, bool>("IsRunning", defaultValue: true);

	public string? Text
	{
		get
		{
			return GetValue(TextProperty);
		}
		set
		{
			SetValue(TextProperty, value);
		}
	}

	public double Speed
	{
		get
		{
			return GetValue(SpeedProperty);
		}
		set
		{
			SetValue(SpeedProperty, value);
		}
	}

	public double Gap
	{
		get
		{
			return GetValue(GapProperty);
		}
		set
		{
			SetValue(GapProperty, value);
		}
	}

	public TimeSpan RepeatDelay
	{
		get
		{
			return GetValue(RepeatDelayProperty);
		}
		set
		{
			SetValue(RepeatDelayProperty, value);
		}
	}

	public bool IsRunning
	{
		get
		{
			return GetValue(IsRunningProperty);
		}
		set
		{
			SetValue(IsRunningProperty, value);
		}
	}

	public LuminaAutoScrollText()
	{
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(16L)
		};
		_timer.Tick += OnTimerTick;
		base.Loaded += delegate
		{
			_isLoaded = true;
			QueueRecalculate();
		};
		base.Unloaded += delegate
		{
			_isLoaded = false;
			_timer.Stop();
		};
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		if (_viewport != null)
		{
			_viewport.SizeChanged -= OnViewportSizeChanged;
		}
		_viewport = e.NameScope.FindRequired<Control>("PART_Viewport");
		_track = e.NameScope.FindRequired<Control>("PART_Track");
		_primaryText = e.NameScope.FindRequired<TextBlock>("PART_PrimaryText");
		_secondaryText = e.NameScope.FindRequired<TextBlock>("PART_SecondaryText");
		if (_track != null)
		{
			_track.RenderTransform = _translate;
		}
		if (_viewport != null)
		{
			_viewport.SizeChanged += OnViewportSizeChanged;
		}
		QueueRecalculate();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == TextProperty || change.Property == SpeedProperty || change.Property == GapProperty || change.Property == RepeatDelayProperty || change.Property == IsRunningProperty || change.Property == TemplatedControl.FontSizeProperty || change.Property == TemplatedControl.FontWeightProperty)
		{
			QueueRecalculate();
		}
	}

	private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		QueueRecalculate();
	}

	private void QueueRecalculate()
	{
		Avalonia.Threading.Dispatcher.UIThread.Post(Recalculate, DispatcherPriority.Loaded);
	}

	private void Recalculate()
	{
		if (_viewport != null && _track != null && _primaryText != null && _secondaryText != null)
		{
			_primaryText.Measure(Size.Infinity);
			_secondaryText.Measure(Size.Infinity);
			double viewportWidth = _viewport.Bounds.Width;
			double textWidth = _primaryText.DesiredSize.Width;
			bool shouldScroll = _isLoaded && IsRunning && Speed > 0.0 && textWidth > viewportWidth + 1.0;
			_cycleWidth = Math.Max(1.0, textWidth + Math.Max(0.0, Gap));
			_secondaryText.IsVisible = shouldScroll;
			_offset = 0.0;
			_translate.X = 0.0;
			_lastTick = DateTime.UtcNow;
			_resumeAt = _lastTick + RepeatDelay;
			base.PseudoClasses.Set(":scrolling", shouldScroll);
			if (shouldScroll)
			{
				_timer.Start();
			}
			else
			{
				_timer.Stop();
			}
		}
	}

	private void OnTimerTick(object? sender, EventArgs e)
	{
		DateTime now = DateTime.UtcNow;
		TimeSpan elapsed = now - _lastTick;
		_lastTick = now;
		if (!(now < _resumeAt))
		{
			_offset += Math.Max(0.0, Speed) * elapsed.TotalSeconds;
			if (_offset >= _cycleWidth)
			{
				_offset = 0.0;
				_resumeAt = now + RepeatDelay;
			}
			_translate.X = 0.0 - _offset;
		}
	}
}
