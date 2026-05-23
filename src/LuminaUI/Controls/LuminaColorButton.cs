using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaColorButton : Button
{
	public static readonly StyledProperty<IBrush?> SwatchBrushProperty = AvaloniaProperty.Register<LuminaColorButton, IBrush?>("SwatchBrush");

	public IBrush? SwatchBrush
	{
		get
		{
			return GetValue(SwatchBrushProperty);
		}
		set
		{
			SetValue(SwatchBrushProperty, value);
		}
	}
}
