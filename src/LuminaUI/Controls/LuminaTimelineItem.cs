using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

[PseudoClasses(new string[] { ":first", ":last", ":empty-icon", ":left", ":right", ":separate" })]
public class LuminaTimelineItem : HeaderedContentControl
{
	public const string PC_First = ":first";

	public const string PC_Last = ":last";

	public const string PC_EmptyIcon = ":empty-icon";

	public const string PC_Left = ":left";

	public const string PC_Right = ":right";

	public const string PC_Separate = ":separate";

	public static readonly StyledProperty<object?> IconProperty;

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

	public static readonly StyledProperty<LuminaTimelineItemStatus> StatusProperty;

	public static readonly StyledProperty<LuminaTimelineItemPosition> PositionProperty;

	public static readonly StyledProperty<object?> TimeProperty;

	public static readonly StyledProperty<string?> TimeFormatProperty;

	public static readonly DirectProperty<LuminaTimelineItem, string> DisplayTimeProperty;

	private string _displayTime = string.Empty;

	public object? Icon
	{
		get
		{
			return GetValue(IconProperty);
		}
		set
		{
			SetValue(IconProperty, value);
		}
	}

	public IDataTemplate? IconTemplate
	{
		get
		{
			return GetValue(IconTemplateProperty);
		}
		set
		{
			SetValue(IconTemplateProperty, value);
		}
	}

	public LuminaTimelineItemStatus Status
	{
		get
		{
			return GetValue(StatusProperty);
		}
		set
		{
			SetValue(StatusProperty, value);
		}
	}

	public LuminaTimelineItemPosition Position
	{
		get
		{
			return GetValue(PositionProperty);
		}
		set
		{
			SetValue(PositionProperty, value);
		}
	}

	public object? Time
	{
		get
		{
			return GetValue(TimeProperty);
		}
		set
		{
			SetValue(TimeProperty, value);
		}
	}

	public string? TimeFormat
	{
		get
		{
			return GetValue(TimeFormatProperty);
		}
		set
		{
			SetValue(TimeFormatProperty, value);
		}
	}

	public string DisplayTime
	{
		get
		{
			return _displayTime;
		}
		private set
		{
			SetAndRaise(DisplayTimeProperty, ref _displayTime, value);
		}
	}

	static LuminaTimelineItem()
	{
		IconProperty = AvaloniaProperty.Register<LuminaTimelineItem, object?>("Icon");
		IconTemplateProperty = AvaloniaProperty.Register<LuminaTimelineItem, IDataTemplate?>("IconTemplate");
		StatusProperty = AvaloniaProperty.Register<LuminaTimelineItem, LuminaTimelineItemStatus>("Status", LuminaTimelineItemStatus.Default);
		PositionProperty = AvaloniaProperty.Register<LuminaTimelineItem, LuminaTimelineItemPosition>("Position", LuminaTimelineItemPosition.Right);
		TimeProperty = AvaloniaProperty.Register<LuminaTimelineItem, object?>("Time");
		TimeFormatProperty = AvaloniaProperty.Register<LuminaTimelineItem, string?>("TimeFormat", "yyyy-MM-dd HH:mm");
		DisplayTimeProperty = AvaloniaProperty.RegisterDirect<LuminaTimelineItem, string>("DisplayTime", (LuminaTimelineItem item) => item.DisplayTime);
		IconProperty.Changed.AddClassHandler(delegate(LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<object?> _)
		{
			item.UpdateIconState();
		});
		PositionProperty.Changed.AddClassHandler(delegate(LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<LuminaTimelineItemPosition> _)
		{
			item.UpdatePositionState();
		});
		TimeProperty.Changed.AddClassHandler(delegate(LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<object?> _)
		{
			item.UpdateDisplayTime();
		});
		TimeFormatProperty.Changed.AddClassHandler(delegate(LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<string?> _)
		{
			item.UpdateDisplayTime();
		});
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		UpdateIconState();
		UpdatePositionState();
		UpdateDisplayTime();
	}

	internal void SetEdgeState(bool isFirst, bool isLast)
	{
		base.PseudoClasses.Set(":first", isFirst);
		base.PseudoClasses.Set(":last", isLast);
	}

	internal void SetIfUnset<T>(AvaloniaProperty<T> property, T value)
	{
		if (!IsSet(property))
		{
			SetCurrentValue(property, value);
		}
	}

	private void UpdateIconState()
	{
		base.PseudoClasses.Set(":empty-icon", Icon == null);
	}

	private void UpdatePositionState()
	{
		base.PseudoClasses.Set(":left", Position == LuminaTimelineItemPosition.Left);
		base.PseudoClasses.Set(":right", Position == LuminaTimelineItemPosition.Right);
		base.PseudoClasses.Set(":separate", Position == LuminaTimelineItemPosition.Separate);
	}

	private void UpdateDisplayTime()
	{
		object? time = Time;
		if (1 == 0)
		{
		}
		IFormattable? formattable;
		string displayTime;
		if (time != null)
		{
			if (!(time is DateTime dateTime))
			{
				if (!(time is DateTimeOffset dateTimeOffset))
				{
					formattable = time as IFormattable;
					if (formattable != null)
					{
						goto IL_0094;
					}
					goto IL_00b3;
				}
				if (string.IsNullOrWhiteSpace(TimeFormat))
				{
					goto IL_0044;
				}
				displayTime = dateTimeOffset.ToString(TimeFormat);
			}
			else
			{
				if (string.IsNullOrWhiteSpace(TimeFormat))
				{
					goto IL_0044;
				}
				displayTime = dateTime.ToString(TimeFormat);
			}
		}
		else
		{
			displayTime = string.Empty;
		}
		goto IL_00ca;
		IL_0044:
		formattable = (IFormattable)time;
		goto IL_0094;
		IL_00ca:
		if (1 == 0)
		{
		}
		DisplayTime = displayTime;
		return;
		IL_0094:
		if (string.IsNullOrWhiteSpace(TimeFormat))
		{
			goto IL_00b3;
		}
		displayTime = formattable.ToString(TimeFormat, null);
		goto IL_00ca;
		IL_00b3:
		displayTime = time?.ToString() ?? string.Empty;
		goto IL_00ca;
	}
}
