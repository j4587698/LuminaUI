using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

internal static class LuminaPickerResources
{
    public static IBrush Brush(string key, IBrush fallback)
    {
        object? resource;
        return (Application.Current != null && Application.Current.TryFindResource(key, out resource) && resource is IBrush brush) ? brush : fallback;
    }
}
