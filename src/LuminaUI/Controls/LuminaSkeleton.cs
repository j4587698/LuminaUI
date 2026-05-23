using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaSkeleton : TemplatedControl
{
	public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<LuminaSkeleton, IBrush?>("Fill");

	public static readonly StyledProperty<bool> IsAnimatedProperty = AvaloniaProperty.Register<LuminaSkeleton, bool>("IsAnimated", defaultValue: true);

	public IBrush? Fill
	{
		get
		{
			return GetValue(FillProperty);
		}
		set
		{
			SetValue(FillProperty, value);
		}
	}

	public bool IsAnimated
	{
		get
		{
			return GetValue(IsAnimatedProperty);
		}
		set
		{
			SetValue(IsAnimatedProperty, value);
		}
	}
}
