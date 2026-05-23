using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaSettingItem : ContentControl
{
	public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<LuminaSettingItem, string?>("Header");

	public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaSettingItem, string?>("Description");

	public string? Header
	{
		get
		{
			return GetValue(HeaderProperty);
		}
		set
		{
			SetValue(HeaderProperty, value);
		}
	}

	public string? Description
	{
		get
		{
			return GetValue(DescriptionProperty);
		}
		set
		{
			SetValue(DescriptionProperty, value);
		}
	}
}
