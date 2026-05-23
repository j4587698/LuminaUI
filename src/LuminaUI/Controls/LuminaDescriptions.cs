using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace LuminaUI.Controls;

public class LuminaDescriptions : ItemsControl
{
	public static readonly StyledProperty<BindingBase?> LabelMemberBindingProperty = AvaloniaProperty.Register<LuminaDescriptions, BindingBase?>("LabelMemberBinding");

	public static readonly StyledProperty<IDataTemplate?> LabelTemplateProperty = AvaloniaProperty.Register<LuminaDescriptions, IDataTemplate?>("LabelTemplate");

	public static readonly StyledProperty<LuminaDescriptionLabelPosition> LabelPositionProperty = AvaloniaProperty.Register<LuminaDescriptions, LuminaDescriptionLabelPosition>("LabelPosition", LuminaDescriptionLabelPosition.Left);

	public static readonly StyledProperty<double> LabelWidthProperty = AvaloniaProperty.Register<LuminaDescriptions, double>("LabelWidth", double.NaN);

	public static readonly StyledProperty<LuminaDescriptionItemAlignment> ItemAlignmentProperty = AvaloniaProperty.Register<LuminaDescriptions, LuminaDescriptionItemAlignment>("ItemAlignment", LuminaDescriptionItemAlignment.Plain);

	public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<LuminaDescriptions, Orientation>("Orientation", Orientation.Vertical);

	public static readonly StyledProperty<bool> ShowBorderLinesProperty = AvaloniaProperty.Register<LuminaDescriptions, bool>("ShowBorderLines", defaultValue: false);

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? LabelMemberBinding
	{
		get
		{
			return GetValue(LabelMemberBindingProperty);
		}
		set
		{
			SetValue(LabelMemberBindingProperty, value);
		}
	}

	[InheritDataTypeFromItems("ItemsSource")]
	public IDataTemplate? LabelTemplate
	{
		get
		{
			return GetValue(LabelTemplateProperty);
		}
		set
		{
			SetValue(LabelTemplateProperty, value);
		}
	}

	public LuminaDescriptionLabelPosition LabelPosition
	{
		get
		{
			return GetValue(LabelPositionProperty);
		}
		set
		{
			SetValue(LabelPositionProperty, value);
		}
	}

	public double LabelWidth
	{
		get
		{
			return GetValue(LabelWidthProperty);
		}
		set
		{
			SetValue(LabelWidthProperty, value);
		}
	}

	public LuminaDescriptionItemAlignment ItemAlignment
	{
		get
		{
			return GetValue(ItemAlignmentProperty);
		}
		set
		{
			SetValue(ItemAlignmentProperty, value);
		}
	}

	public Orientation Orientation
	{
		get
		{
			return GetValue(OrientationProperty);
		}
		set
		{
			SetValue(OrientationProperty, value);
		}
	}

	public bool ShowBorderLines
	{
		get
		{
			return GetValue(ShowBorderLinesProperty);
		}
		set
		{
			SetValue(ShowBorderLinesProperty, value);
		}
	}

	protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
	{
		recycleKey = null;
		return !(item is LuminaDescriptionsItem);
	}

	protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
	{
		return (item as LuminaDescriptionsItem) ?? new LuminaDescriptionsItem();
	}

	protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
	{
		base.PrepareContainerForItemOverride(container, item, index);
		if (!(container is LuminaDescriptionsItem descriptionItem))
		{
			return;
		}
		descriptionItem.SetIfUnset(LuminaDescriptionsItem.LabelPositionProperty, LabelPosition);
		descriptionItem.SetIfUnset(LuminaDescriptionsItem.LabelWidthProperty, LabelWidth);
		descriptionItem.SetIfUnset(LuminaDescriptionsItem.ItemAlignmentProperty, ItemAlignment);
		descriptionItem.SetIfUnset(LuminaDescriptionsItem.LabelTemplateProperty, LabelTemplate);
		descriptionItem.SetIfUnset(LuminaDescriptionsItem.ShowBorderLinesProperty, ShowBorderLines);
		if (!(item is LuminaDescriptionsItem))
		{
			if (!descriptionItem.IsSet(ContentControl.ContentTemplateProperty) && base.ItemTemplate != null)
			{
				descriptionItem.SetCurrentValue(ContentControl.ContentTemplateProperty, base.ItemTemplate);
			}
			if (LabelMemberBinding != null && !descriptionItem.IsSet(LuminaDescriptionsItem.LabelProperty))
			{
				descriptionItem.Bind(LuminaDescriptionsItem.LabelProperty, LabelMemberBinding);
			}
			else if (!descriptionItem.IsSet(LuminaDescriptionsItem.LabelProperty))
			{
				descriptionItem.SetCurrentValue(LuminaDescriptionsItem.LabelProperty, item);
			}
			if (base.DisplayMemberBinding != null && !descriptionItem.IsSet(ContentControl.ContentProperty))
			{
				descriptionItem.Bind(ContentControl.ContentProperty, base.DisplayMemberBinding);
			}
			else if (!descriptionItem.IsSet(ContentControl.ContentProperty))
			{
				descriptionItem.SetCurrentValue(ContentControl.ContentProperty, item);
			}
		}
	}
}
