using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public abstract class LuminaSectionBase : ItemsControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<LuminaSectionBase, string?>(nameof(Header));

    public static readonly StyledProperty<string?> KeyProperty =
        AvaloniaProperty.Register<LuminaSectionBase, string?>(nameof(Key));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Key
    {
        get => GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    internal string GetNavigationKey()
    {
        if (!string.IsNullOrWhiteSpace(Key))
        {
            return Key;
        }
        if (!string.IsNullOrWhiteSpace(Header))
        {
            return Header.Substring(0, 1).ToUpperInvariant();
        }
        return "#";
    }
}
