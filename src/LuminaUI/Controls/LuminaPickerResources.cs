using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace LuminaUI.Controls;

internal static class LuminaPickerResources
{
    public static void BindBrush(AvaloniaObject target, AvaloniaProperty property, string key)
    {
        target.Bind(property, new DynamicResourceExtension(key));
    }

    public static IBrush Brush(string key, IBrush fallback)
    {
        object? resource;
        return (Application.Current != null && Application.Current.TryFindResource(key, out resource) && resource is IBrush brush) ? brush : fallback;
    }
}
