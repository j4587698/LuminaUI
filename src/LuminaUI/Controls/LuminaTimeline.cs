using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;

namespace LuminaUI.Controls;

public class LuminaTimeline : ItemsControl
{
	private static readonly FuncTemplate<Panel?> DefaultPanel;

	public static readonly StyledProperty<BindingBase?> HeaderMemberBindingProperty;

	public static readonly StyledProperty<BindingBase?> ContentMemberBindingProperty;

	public static readonly StyledProperty<BindingBase?> TimeMemberBindingProperty;

	public static readonly StyledProperty<BindingBase?> StatusMemberBindingProperty;

	public static readonly StyledProperty<BindingBase?> IconMemberBindingProperty;

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

	public static readonly StyledProperty<IDataTemplate?> DescriptionTemplateProperty;

	public static readonly StyledProperty<string?> TimeFormatProperty;

	public static readonly StyledProperty<LuminaTimelineMode> ModeProperty;

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? HeaderMemberBinding
	{
		get
		{
			return GetValue(HeaderMemberBindingProperty);
		}
		set
		{
			SetValue(HeaderMemberBindingProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? ContentMemberBinding
	{
		get
		{
			return GetValue(ContentMemberBindingProperty);
		}
		set
		{
			SetValue(ContentMemberBindingProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? TimeMemberBinding
	{
		get
		{
			return GetValue(TimeMemberBindingProperty);
		}
		set
		{
			SetValue(TimeMemberBindingProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? StatusMemberBinding
	{
		get
		{
			return GetValue(StatusMemberBindingProperty);
		}
		set
		{
			SetValue(StatusMemberBindingProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? IconMemberBinding
	{
		get
		{
			return GetValue(IconMemberBindingProperty);
		}
		set
		{
			SetValue(IconMemberBindingProperty, value);
		}
	}

	[InheritDataTypeFromItems("ItemsSource")]
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

	[InheritDataTypeFromItems("ItemsSource")]
	public IDataTemplate? DescriptionTemplate
	{
		get
		{
			return GetValue(DescriptionTemplateProperty);
		}
		set
		{
			SetValue(DescriptionTemplateProperty, value);
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

	public LuminaTimelineMode Mode
	{
		get
		{
			return GetValue(ModeProperty);
		}
		set
		{
			SetValue(ModeProperty, value);
		}
	}

	static LuminaTimeline()
	{
		DefaultPanel = new FuncTemplate<Panel?>(() => new StackPanel());
		HeaderMemberBindingProperty = AvaloniaProperty.Register<LuminaTimeline, BindingBase?>("HeaderMemberBinding");
		ContentMemberBindingProperty = AvaloniaProperty.Register<LuminaTimeline, BindingBase?>("ContentMemberBinding");
		TimeMemberBindingProperty = AvaloniaProperty.Register<LuminaTimeline, BindingBase?>("TimeMemberBinding");
		StatusMemberBindingProperty = AvaloniaProperty.Register<LuminaTimeline, BindingBase?>("StatusMemberBinding");
		IconMemberBindingProperty = AvaloniaProperty.Register<LuminaTimeline, BindingBase?>("IconMemberBinding");
		IconTemplateProperty = AvaloniaProperty.Register<LuminaTimeline, IDataTemplate?>("IconTemplate");
		DescriptionTemplateProperty = AvaloniaProperty.Register<LuminaTimeline, IDataTemplate?>("DescriptionTemplate");
		TimeFormatProperty = AvaloniaProperty.Register<LuminaTimeline, string?>("TimeFormat", "yyyy-MM-dd HH:mm");
		ModeProperty = AvaloniaProperty.Register<LuminaTimeline, LuminaTimelineMode>("Mode", LuminaTimelineMode.Left);
		ItemsControl.ItemsPanelProperty.OverrideDefaultValue<LuminaTimeline>(DefaultPanel);
		ModeProperty.Changed.AddClassHandler(delegate(LuminaTimeline timeline, AvaloniaPropertyChangedEventArgs<LuminaTimelineMode> _)
		{
			timeline.RefreshTimelineContainers();
		});
		TimeFormatProperty.Changed.AddClassHandler(delegate(LuminaTimeline timeline, AvaloniaPropertyChangedEventArgs<string?> _)
		{
			timeline.RefreshTimelineContainers();
		});
	}

	protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
	{
		recycleKey = null;
		return !(item is LuminaTimelineItem);
	}

	protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
	{
		return (item as LuminaTimelineItem) ?? new LuminaTimelineItem();
	}

	protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
	{
		base.PrepareContainerForItemOverride(container, item, index);
		if (container is LuminaTimelineItem timelineItem)
		{
			int lastIndex = Math.Max(0, base.ItemCount - 1);
			timelineItem.SetEdgeState(index == 0, index == lastIndex);
			timelineItem.SetIfUnset(LuminaTimelineItem.TimeFormatProperty, TimeFormat);
			timelineItem.SetIfUnset(LuminaTimelineItem.IconTemplateProperty, IconTemplate);
			timelineItem.SetIfUnset(HeaderedContentControl.HeaderTemplateProperty, base.ItemTemplate);
			timelineItem.SetIfUnset(ContentControl.ContentTemplateProperty, DescriptionTemplate);
			if (HeaderMemberBinding != null)
			{
				timelineItem.Bind(HeaderedContentControl.HeaderProperty, HeaderMemberBinding);
			}
			if (ContentMemberBinding != null)
			{
				timelineItem.Bind(ContentControl.ContentProperty, ContentMemberBinding);
			}
			if (TimeMemberBinding != null)
			{
				timelineItem.Bind(LuminaTimelineItem.TimeProperty, TimeMemberBinding);
			}
			if (StatusMemberBinding != null)
			{
				timelineItem.Bind(LuminaTimelineItem.StatusProperty, StatusMemberBinding);
			}
			if (IconMemberBinding != null)
			{
				timelineItem.Bind(LuminaTimelineItem.IconProperty, IconMemberBinding);
			}
			if (!(item is LuminaTimelineItem) && ContentMemberBinding == null && timelineItem.Content == null)
			{
				timelineItem.Content = item;
			}
			ApplyItemPosition(timelineItem, index);
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		RefreshTimelineContainers();
		return base.ArrangeOverride(finalSize);
	}

	private void RefreshTimelineContainers()
	{
		if (base.ItemsPanelRoot == null)
		{
			return;
		}
		LuminaTimelineItem[] items = base.ItemsPanelRoot.Children.OfType<LuminaTimelineItem>().ToArray();
		for (int i = 0; i < items.Length; i++)
		{
			items[i].SetEdgeState(i == 0, i == items.Length - 1);
			ApplyItemPosition(items[i], i);
			if (!items[i].IsSet(LuminaTimelineItem.TimeFormatProperty))
			{
				items[i].SetCurrentValue(LuminaTimelineItem.TimeFormatProperty, TimeFormat);
			}
		}
	}

	private void ApplyItemPosition(LuminaTimelineItem item, int index)
	{
		if (!item.IsSet(LuminaTimelineItem.PositionProperty))
		{
			LuminaTimelineMode mode = Mode;
			if (1 == 0)
			{
			}
			LuminaTimelineItemPosition luminaTimelineItemPosition = mode switch
			{
				LuminaTimelineMode.Right => LuminaTimelineItemPosition.Left, 
				LuminaTimelineMode.Center => LuminaTimelineItemPosition.Separate, 
				LuminaTimelineMode.Alternate => (index % 2 == 0) ? LuminaTimelineItemPosition.Right : LuminaTimelineItemPosition.Left, 
				_ => LuminaTimelineItemPosition.Right, 
			};
			if (1 == 0)
			{
			}
			LuminaTimelineItemPosition position = luminaTimelineItemPosition;
			item.SetCurrentValue(LuminaTimelineItem.PositionProperty, position);
		}
	}
}
