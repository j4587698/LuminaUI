using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public abstract class LuminaSectionBase : ItemsControl
{
	public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<LuminaSectionBase, string?>("Header");

	public static readonly StyledProperty<string?> KeyProperty = AvaloniaProperty.Register<LuminaSectionBase, string?>("Key");

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

	public string? Key
	{
		get
		{
			return GetValue(KeyProperty);
		}
		set
		{
			SetValue(KeyProperty, value);
		}
	}

	internal string GetNavigationKey()
	{
		if (!string.IsNullOrWhiteSpace(Key))
		{
			return Key;
		}
		if (!string.IsNullOrWhiteSpace(Header))
		{
			return Header.Substring(0, 1).ToUpperInvariant();
		}
		return "#";
	}
}
