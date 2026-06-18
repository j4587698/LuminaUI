using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace LuminaUI.Controls;

internal static class LuminaPickerResources
{
    public static void BindResource(AvaloniaObject target, AvaloniaProperty property, string key)
    {
        target.Bind(property, new DynamicResourceExtension(key));
    }

    public static void BindBrush(AvaloniaObject target, AvaloniaProperty property, string key)
    {
        BindResource(target, property, key);
    }

    public static IBrush Brush(string key, IBrush fallback)
    {
        object? resource;
        return (Application.Current != null && Application.Current.TryFindResource(key, out resource) && resource is IBrush brush) ? brush : fallback;
    }

    public static double Double(string key, double fallback)
    {
        object? resource;
        return Application.Current != null && Application.Current.TryFindResource(key, out resource) && resource is double value ? value : fallback;
    }

    public static Thickness Thickness(string key, Thickness fallback)
    {
        object? resource;
        return Application.Current != null && Application.Current.TryFindResource(key, out resource) && resource is Thickness value ? value : fallback;
    }

    public static CornerRadius CornerRadius(string key, CornerRadius fallback)
    {
        object? resource;
        return Application.Current != null && Application.Current.TryFindResource(key, out resource) && resource is CornerRadius value ? value : fallback;
    }
}
