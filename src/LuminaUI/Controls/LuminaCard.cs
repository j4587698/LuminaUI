using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaCard : ContentControl
{
	public static readonly StyledProperty<bool> IsElevatedProperty = AvaloniaProperty.Register<LuminaCard, bool>("IsElevated", defaultValue: false);

	public static readonly StyledProperty<double> BackdropBlurRadiusProperty = AvaloniaProperty.Register<LuminaCard, double>("BackdropBlurRadius", 128.0);

	public static readonly StyledProperty<IBrush?> GlassTintBrushProperty = AvaloniaProperty.Register<LuminaCard, IBrush?>("GlassTintBrush");

	public static readonly StyledProperty<IBrush?> GlassEdgeBrushProperty = AvaloniaProperty.Register<LuminaCard, IBrush?>("GlassEdgeBrush");

	public bool IsElevated
	{
		get
		{
			return GetValue(IsElevatedProperty);
		}
		set
		{
			SetValue(IsElevatedProperty, value);
		}
	}

	public double BackdropBlurRadius
	{
		get
		{
			return GetValue(BackdropBlurRadiusProperty);
		}
		set
		{
			SetValue(BackdropBlurRadiusProperty, value);
		}
	}

	public IBrush? GlassTintBrush
	{
		get
		{
			return GetValue(GlassTintBrushProperty);
		}
		set
		{
			SetValue(GlassTintBrushProperty, value);
		}
	}

	public IBrush? GlassEdgeBrush
	{
		get
		{
			return GetValue(GlassEdgeBrushProperty);
		}
		set
		{
			SetValue(GlassEdgeBrushProperty, value);
		}
	}
}
