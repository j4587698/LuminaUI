using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace LuminaUI.Controls;

public class LuminaSteps : ItemsControl
{
	public static readonly StyledProperty<int> CurrentProperty;

	public static readonly StyledProperty<LuminaStepsDirection> DirectionProperty;

	public static readonly StyledProperty<LuminaStepsSize> SizeProperty;

	public static readonly StyledProperty<bool> IsProgressDotProperty;

	public static readonly StyledProperty<BindingBase?> TitleMemberBindingProperty;

	public static readonly StyledProperty<BindingBase?> DescriptionMemberBindingProperty;

	public static readonly StyledProperty<BindingBase?> StatusMemberBindingProperty;

	public static readonly StyledProperty<BindingBase?> IconMemberBindingProperty;

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

	public int Current
	{
		get
		{
			return GetValue(CurrentProperty);
		}
		set
		{
			SetValue(CurrentProperty, value);
		}
	}

	public LuminaStepsDirection Direction
	{
		get
		{
			return GetValue(DirectionProperty);
		}
		set
		{
			SetValue(DirectionProperty, value);
		}
	}

	public LuminaStepsSize Size
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

	public bool IsProgressDot
	{
		get
		{
			return GetValue(IsProgressDotProperty);
		}
		set
		{
			SetValue(IsProgressDotProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? TitleMemberBinding
	{
		get
		{
			return GetValue(TitleMemberBindingProperty);
		}
		set
		{
			SetValue(TitleMemberBindingProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? DescriptionMemberBinding
	{
		get
		{
			return GetValue(DescriptionMemberBindingProperty);
		}
		set
		{
			SetValue(DescriptionMemberBindingProperty, value);
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

	static LuminaSteps()
	{
		CurrentProperty = AvaloniaProperty.Register<LuminaSteps, int>("Current", 0);
		DirectionProperty = AvaloniaProperty.Register<LuminaSteps, LuminaStepsDirection>("Direction", LuminaStepsDirection.Horizontal);
		SizeProperty = AvaloniaProperty.Register<LuminaSteps, LuminaStepsSize>("Size", LuminaStepsSize.Default);
		IsProgressDotProperty = AvaloniaProperty.Register<LuminaSteps, bool>("IsProgressDot", defaultValue: false);
		TitleMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>("TitleMemberBinding");
		DescriptionMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>("DescriptionMemberBinding");
		StatusMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>("StatusMemberBinding");
		IconMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>("IconMemberBinding");
		IconTemplateProperty = AvaloniaProperty.Register<LuminaSteps, IDataTemplate?>("IconTemplate");
		ItemsControl.ItemsPanelProperty.OverrideDefaultValue<LuminaSteps>(new FuncTemplate<Panel?>(() => new StackPanel
		{
			Orientation = Orientation.Horizontal
		}));
		CurrentProperty.Changed.AddClassHandler(delegate(LuminaSteps steps, AvaloniaPropertyChangedEventArgs _)
		{
			steps.RefreshStepContainers();
		});
		DirectionProperty.Changed.AddClassHandler(delegate(LuminaSteps steps, AvaloniaPropertyChangedEventArgs<LuminaStepsDirection> _)
		{
			steps.ItemsPanelProperty_Changed();
			steps.RefreshStepContainers();
		});
		IsProgressDotProperty.Changed.AddClassHandler(delegate(LuminaSteps steps, AvaloniaPropertyChangedEventArgs _)
		{
			steps.RefreshStepContainers();
		});
	}

	protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
	{
		recycleKey = null;
		return !(item is LuminaStepItem);
	}

	protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
	{
		return (item as LuminaStepItem) ?? new LuminaStepItem();
	}

	protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
	{
		base.PrepareContainerForItemOverride(container, item, index);
		if (container is LuminaStepItem stepItem)
		{
			stepItem.StepNumber = index + 1;
			if (TitleMemberBinding != null)
			{
				stepItem.Bind(HeaderedContentControl.HeaderProperty, TitleMemberBinding);
			}
			if (DescriptionMemberBinding != null)
			{
				stepItem.Bind(ContentControl.ContentProperty, DescriptionMemberBinding);
			}
			if (StatusMemberBinding != null)
			{
				stepItem.Bind(LuminaStepItem.StatusProperty, StatusMemberBinding);
			}
			if (IconMemberBinding != null)
			{
				stepItem.Bind(LuminaStepItem.IconProperty, IconMemberBinding);
			}
			stepItem.SetIfUnset(LuminaStepItem.IconTemplateProperty, IconTemplate);
			stepItem.SetIfUnset(HeaderedContentControl.HeaderTemplateProperty, base.ItemTemplate);
			if (!(item is LuminaStepItem) && DescriptionMemberBinding == null && stepItem.Content == null)
			{
				stepItem.Content = item;
			}
			ApplyItemState(stepItem, index);
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		RefreshStepContainers();
		return base.ArrangeOverride(finalSize);
	}

	private void RefreshStepContainers()
	{
		if (base.ItemsPanelRoot != null)
		{
			LuminaStepItem[] items = base.ItemsPanelRoot.Children.OfType<LuminaStepItem>().ToArray();
			for (int i = 0; i < items.Length; i++)
			{
				items[i].SetEdgeState(i == 0, i == items.Length - 1);
				items[i].SetDirectionState(Direction);
				ApplyItemState(items[i], i);
			}
		}
	}

	private void ApplyItemState(LuminaStepItem item, int index)
	{
		if (StatusMemberBinding == null)
		{
			LuminaStepStatus status = ((index < Current) ? LuminaStepStatus.Finish : ((index == Current) ? LuminaStepStatus.Process : LuminaStepStatus.Wait));
			if (item.Status != LuminaStepStatus.Error || index != Current)
			{
				item.SetCurrentValue(LuminaStepItem.StatusProperty, status);
			}
		}
		item.SetCurrentState(index == Current);
	}

	private void ItemsPanelProperty_Changed()
	{
		FuncTemplate<Panel?> panel = ((Direction == LuminaStepsDirection.Horizontal) ? new FuncTemplate<Panel?>(() => new UniformGrid
		{
			Rows = 1
		}) : new FuncTemplate<Panel?>(() => new StackPanel
		{
			Orientation = Orientation.Vertical
		}));
		SetCurrentValue(ItemsControl.ItemsPanelProperty, panel);
	}
}
