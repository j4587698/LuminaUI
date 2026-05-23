using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

[PseudoClasses(new string[] { ":left", ":top" })]
public class LuminaDescriptionsItem : ContentControl
{
	public const string PC_Left = ":left";

	public const string PC_Top = ":top";

	public static readonly StyledProperty<object?> LabelProperty;

	public static readonly StyledProperty<IDataTemplate?> LabelTemplateProperty;

	public static readonly StyledProperty<LuminaDescriptionLabelPosition> LabelPositionProperty;

	public static readonly StyledProperty<double> LabelWidthProperty;

	public static readonly StyledProperty<LuminaDescriptionItemAlignment> ItemAlignmentProperty;

	public static readonly StyledProperty<bool> ShowBorderLinesProperty;

	public object? Label
	{
		get
		{
			return GetValue(LabelProperty);
		}
		set
		{
			SetValue(LabelProperty, value);
		}
	}

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

	static LuminaDescriptionsItem()
	{
		LabelProperty = AvaloniaProperty.Register<LuminaDescriptionsItem, object?>("Label");
		LabelTemplateProperty = AvaloniaProperty.Register<LuminaDescriptionsItem, IDataTemplate?>("LabelTemplate");
		LabelPositionProperty = AvaloniaProperty.Register<LuminaDescriptionsItem, LuminaDescriptionLabelPosition>("LabelPosition", LuminaDescriptionLabelPosition.Left);
		LabelWidthProperty = AvaloniaProperty.Register<LuminaDescriptionsItem, double>("LabelWidth", double.NaN);
		ItemAlignmentProperty = AvaloniaProperty.Register<LuminaDescriptionsItem, LuminaDescriptionItemAlignment>("ItemAlignment", LuminaDescriptionItemAlignment.Plain);
		ShowBorderLinesProperty = AvaloniaProperty.Register<LuminaDescriptionsItem, bool>("ShowBorderLines", defaultValue: false);
		LabelPositionProperty.Changed.AddClassHandler(delegate(LuminaDescriptionsItem item, AvaloniaPropertyChangedEventArgs<LuminaDescriptionLabelPosition> _)
		{
			item.UpdateLabelPosition();
		});
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		UpdateLabelPosition();
	}

	internal void SetIfUnset<T>(AvaloniaProperty<T> property, T value)
	{
		if (!IsSet(property))
		{
			SetCurrentValue(property, value);
		}
	}

	private void UpdateLabelPosition()
	{
		base.PseudoClasses.Set(":left", LabelPosition == LuminaDescriptionLabelPosition.Left);
		base.PseudoClasses.Set(":top", LabelPosition == LuminaDescriptionLabelPosition.Top);
	}
}
