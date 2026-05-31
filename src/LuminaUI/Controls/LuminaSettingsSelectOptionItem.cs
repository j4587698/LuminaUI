using Avalonia;

namespace LuminaUI.Controls;

public class LuminaSettingsSelectOptionItem : AvaloniaObject
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<LuminaSettingsSelectOptionItem, bool>(nameof(IsSelected), defaultValue: false);

    public LuminaSettingsSelectOptionItem(int index, object? item, string text)
    {
        Index = index;
        Item = item;
        Text = text;
    }

    public int Index { get; }

    public object? Item { get; }

    public string Text { get; }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}
