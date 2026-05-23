using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaDialog : ContentControl
{
	public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<LuminaDialog, string?>("Title");

	public string? Title
	{
		get
		{
			return GetValue(TitleProperty);
		}
		set
		{
			SetValue(TitleProperty, value);
		}
	}
}
