using Avalonia;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

public sealed class Skeleton
{
	public static readonly AttachedProperty<LuminaSkeletonShape> ShapeProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, LuminaSkeletonShape>("Shape", LuminaSkeletonShape.Auto);

	public static readonly AttachedProperty<IDataTemplate?> ItemTemplateProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, IDataTemplate?>("ItemTemplate");

	public static readonly AttachedProperty<int> PlaceholderCountProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, int>("PlaceholderCount", 0);

	public static readonly AttachedProperty<double> WidthProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, double>("Width", double.NaN);

	public static readonly AttachedProperty<double> HeightProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, double>("Height", double.NaN);

	public static readonly AttachedProperty<CornerRadius?> CornerRadiusProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, CornerRadius?>("CornerRadius");

	public static readonly AttachedProperty<LuminaSkeletonShape> ItemShapeProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, LuminaSkeletonShape>("ItemShape", LuminaSkeletonShape.AvatarText);

	public static readonly AttachedProperty<bool> IgnoreProperty = AvaloniaProperty.RegisterAttached<Skeleton, AvaloniaObject, bool>("Ignore", defaultValue: false);

	private Skeleton()
	{
	}

	public static LuminaSkeletonShape GetShape(AvaloniaObject element)
	{
		return element.GetValue(ShapeProperty);
	}

	public static void SetShape(AvaloniaObject element, LuminaSkeletonShape value)
	{
		element.SetValue(ShapeProperty, value);
	}

	public static IDataTemplate? GetItemTemplate(AvaloniaObject element)
	{
		return element.GetValue(ItemTemplateProperty);
	}

	public static void SetItemTemplate(AvaloniaObject element, IDataTemplate? value)
	{
		element.SetValue(ItemTemplateProperty, value);
	}

	public static int GetPlaceholderCount(AvaloniaObject element)
	{
		return element.GetValue(PlaceholderCountProperty);
	}

	public static void SetPlaceholderCount(AvaloniaObject element, int value)
	{
		element.SetValue(PlaceholderCountProperty, value);
	}

	public static double GetWidth(AvaloniaObject element)
	{
		return element.GetValue(WidthProperty);
	}

	public static void SetWidth(AvaloniaObject element, double value)
	{
		element.SetValue(WidthProperty, value);
	}

	public static double GetHeight(AvaloniaObject element)
	{
		return element.GetValue(HeightProperty);
	}

	public static void SetHeight(AvaloniaObject element, double value)
	{
		element.SetValue(HeightProperty, value);
	}

	public static CornerRadius? GetCornerRadius(AvaloniaObject element)
	{
		return element.GetValue(CornerRadiusProperty);
	}

	public static void SetCornerRadius(AvaloniaObject element, CornerRadius? value)
	{
		element.SetValue(CornerRadiusProperty, value);
	}

	public static LuminaSkeletonShape GetItemShape(AvaloniaObject element)
	{
		return element.GetValue(ItemShapeProperty);
	}

	public static void SetItemShape(AvaloniaObject element, LuminaSkeletonShape value)
	{
		element.SetValue(ItemShapeProperty, value);
	}

	public static bool GetIgnore(AvaloniaObject element)
	{
		return element.GetValue(IgnoreProperty);
	}

	public static void SetIgnore(AvaloniaObject element, bool value)
	{
		element.SetValue(IgnoreProperty, value);
	}
}
