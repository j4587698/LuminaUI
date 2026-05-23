using Avalonia;

namespace LuminaUI.Controls;

public class LuminaSettingsSelectOptionItem : AvaloniaObject
{
	public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<LuminaSettingsSelectOptionItem, bool>("IsSelected", defaultValue: false);

	public int Index { get; }

	public object? Item { get; }

	public string Text { get; }

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

	public LuminaSettingsSelectOptionItem(int index, object? item, string text)
	{
		Index = index;
		Item = item;
		Text = text;
	}
}
