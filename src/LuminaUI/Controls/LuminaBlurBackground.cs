using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaBlurBackground : ContentControl
{
	public static readonly StyledProperty<double> BlurRadiusProperty = AvaloniaProperty.Register<LuminaBlurBackground, double>("BlurRadius", 44.0);

	public static readonly StyledProperty<IBrush?> TintBrushProperty = AvaloniaProperty.Register<LuminaBlurBackground, IBrush?>("TintBrush");

	public static readonly StyledProperty<IBrush?> EdgeBrushProperty = AvaloniaProperty.Register<LuminaBlurBackground, IBrush?>("EdgeBrush");

	public static readonly StyledProperty<Thickness> EdgeThicknessProperty = AvaloniaProperty.Register<LuminaBlurBackground, Thickness>("EdgeThickness", new Thickness(1.0));

	public double BlurRadius
	{
		get
		{
			return GetValue(BlurRadiusProperty);
		}
		set
		{
			SetValue(BlurRadiusProperty, value);
		}
	}

	public IBrush? TintBrush
	{
		get
		{
			return GetValue(TintBrushProperty);
		}
		set
		{
			SetValue(TintBrushProperty, value);
		}
	}

	public IBrush? EdgeBrush
	{
		get
		{
			return GetValue(EdgeBrushProperty);
		}
		set
		{
			SetValue(EdgeBrushProperty, value);
		}
	}

	public Thickness EdgeThickness
	{
		get
		{
			return GetValue(EdgeThicknessProperty);
		}
		set
		{
			SetValue(EdgeThicknessProperty, value);
		}
	}
}
