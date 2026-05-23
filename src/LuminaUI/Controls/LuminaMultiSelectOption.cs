using Avalonia;

namespace LuminaUI.Controls;

public class LuminaMultiSelectOption : AvaloniaObject
{
	public static readonly StyledProperty<object?> ItemProperty = AvaloniaProperty.Register<LuminaMultiSelectOption, object?>("Item");

	public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<LuminaMultiSelectOption, bool>("IsSelected", defaultValue: false);

	public object? Item
	{
		get
		{
			return GetValue(ItemProperty);
		}
		set
		{
			SetValue(ItemProperty, value);
		}
	}

	public bool IsSelected
	{
		get
		{
			return GetValue(IsSelectedProperty);
		}
		set
		{
			SetValue(IsSelectedProperty, value);
		}
	}

	public string DisplayText => Item?.ToString() ?? string.Empty;

	public LuminaMultiSelectOption(object? item, bool isSelected)
	{
		Item = item;
		IsSelected = isSelected;
	}
}
