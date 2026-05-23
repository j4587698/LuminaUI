using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

public class LuminaPropertyItem : ContentControl
{
	public static readonly StyledProperty<object?> PropertyNameProperty = AvaloniaProperty.Register<LuminaPropertyItem, object?>("PropertyName");

	public static readonly StyledProperty<IDataTemplate?> PropertyNameTemplateProperty = AvaloniaProperty.Register<LuminaPropertyItem, IDataTemplate?>("PropertyNameTemplate");

	public static readonly StyledProperty<double> LabelWidthProperty = AvaloniaProperty.Register<LuminaPropertyItem, double>("LabelWidth", 150.0);

	public static readonly StyledProperty<bool> ShowBorderLinesProperty = AvaloniaProperty.Register<LuminaPropertyItem, bool>("ShowBorderLines", defaultValue: true);

	public object? PropertyName
	{
		get
		{
			return GetValue(PropertyNameProperty);
		}
		set
		{
			SetValue(PropertyNameProperty, value);
		}
	}

	public IDataTemplate? PropertyNameTemplate
	{
		get
		{
			return GetValue(PropertyNameTemplateProperty);
		}
		set
		{
			SetValue(PropertyNameTemplateProperty, value);
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

	internal void SetIfUnset<T>(AvaloniaProperty<T> property, T value)
	{
		if (!IsSet(property))
		{
			SetCurrentValue(property, value);
		}
	}
}
