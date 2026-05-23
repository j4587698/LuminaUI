using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaLoading : ContentControl
{
	public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<LuminaLoading, bool>("IsActive", defaultValue: true);

	public static readonly StyledProperty<LuminaLoadingKind> KindProperty = AvaloniaProperty.Register<LuminaLoading, LuminaLoadingKind>("Kind", LuminaLoadingKind.Ring);

	public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<LuminaLoading, double>("Size", 22.0);

	public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<LuminaLoading, double>("StrokeThickness", 3.0);

	public bool IsActive
	{
		get
		{
			return GetValue(IsActiveProperty);
		}
		set
		{
			SetValue(IsActiveProperty, value);
		}
	}

	public LuminaLoadingKind Kind
	{
		get
		{
			return GetValue(KindProperty);
		}
		set
		{
			SetValue(KindProperty, value);
		}
	}

	public double Size
	{
		get
		{
			return GetValue(SizeProperty);
		}
		set
		{
			SetValue(SizeProperty, value);
		}
	}

	public double StrokeThickness
	{
		get
		{
			return GetValue(StrokeThicknessProperty);
		}
		set
		{
			SetValue(StrokeThicknessProperty, value);
		}
	}
}
