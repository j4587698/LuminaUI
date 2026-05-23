using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaForm : ItemsControl
{
	public static readonly AttachedProperty<object?> LabelProperty = AvaloniaProperty.RegisterAttached<LuminaForm, Control, object?>("Label");

	public static readonly AttachedProperty<string?> DescriptionProperty = AvaloniaProperty.RegisterAttached<LuminaForm, Control, string?>("Description");

	public static readonly AttachedProperty<bool> IsRequiredProperty = AvaloniaProperty.RegisterAttached<LuminaForm, Control, bool>("IsRequired", defaultValue: false);

	public static readonly AttachedProperty<bool> NoLabelProperty = AvaloniaProperty.RegisterAttached<LuminaForm, Control, bool>("NoLabel", defaultValue: false);

	public static readonly StyledProperty<LuminaFormLabelPosition> LabelPositionProperty = AvaloniaProperty.Register<LuminaForm, LuminaFormLabelPosition>("LabelPosition", LuminaFormLabelPosition.Top);

	public static readonly StyledProperty<GridLength> LabelWidthProperty = AvaloniaProperty.Register<LuminaForm, GridLength>("LabelWidth", GridLength.Auto);

	public static readonly StyledProperty<double> ItemSpacingProperty = AvaloniaProperty.Register<LuminaForm, double>("ItemSpacing", 14.0);

	public LuminaFormLabelPosition LabelPosition
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

	public GridLength LabelWidth
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

	public double ItemSpacing
	{
		get
		{
			return GetValue(ItemSpacingProperty);
		}
		set
		{
			SetValue(ItemSpacingProperty, value);
		}
	}

	public static object? GetLabel(Control element)
	{
		return element.GetValue(LabelProperty);
	}

	public static void SetLabel(Control element, object? value)
	{
		element.SetValue(LabelProperty, value);
	}

	public static string? GetDescription(Control element)
	{
		return element.GetValue(DescriptionProperty);
	}

	public static void SetDescription(Control element, string? value)
	{
		element.SetValue(DescriptionProperty, value);
	}

	public static bool GetIsRequired(Control element)
	{
		return element.GetValue(IsRequiredProperty);
	}

	public static void SetIsRequired(Control element, bool value)
	{
		element.SetValue(IsRequiredProperty, value);
	}

	public static bool GetNoLabel(Control element)
	{
		return element.GetValue(NoLabelProperty);
	}

	public static void SetNoLabel(Control element, bool value)
	{
		element.SetValue(NoLabelProperty, value);
	}

	protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
	{
		recycleKey = null;
		return !(item is LuminaFormItem) && !(item is LuminaFormGroup);
	}

	protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
	{
		LuminaFormItem formItem = new LuminaFormItem();
		if (item is Control control)
		{
			formItem.Content = control;
			formItem.Label = GetLabel(control);
			formItem.Description = GetDescription(control);
			formItem.IsRequired = GetIsRequired(control);
			formItem.NoLabel = GetNoLabel(control);
		}
		else
		{
			formItem.Content = item;
		}
		return formItem;
	}

	protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
	{
		base.PrepareContainerForItemOverride(container, item, index);
		if (!(container is LuminaFormItem formItem))
		{
			if (container is LuminaFormGroup formGroup)
			{
				formGroup.SetCurrentValue(LuminaFormGroup.LabelPositionProperty, LabelPosition);
				formGroup.SetCurrentValue(LuminaFormGroup.LabelWidthProperty, LabelWidth);
				formGroup.SetCurrentValue(LuminaFormGroup.ItemSpacingProperty, ItemSpacing);
			}
		}
		else
		{
			formItem.SetCurrentValue(LuminaFormItem.LabelPositionProperty, LabelPosition);
			formItem.SetCurrentValue(LuminaFormItem.LabelWidthProperty, LabelWidth);
		}
	}
}
