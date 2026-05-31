using Avalonia;

namespace LuminaUI.Controls;

public class LuminaMultiSelectOption : AvaloniaObject
{
    public static readonly StyledProperty<object?> ItemProperty =
        AvaloniaProperty.Register<LuminaMultiSelectOption, object?>(nameof(Item));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<LuminaMultiSelectOption, bool>(nameof(IsSelected), defaultValue: false);

    public LuminaMultiSelectOption(object? item, bool isSelected)
    {
        Item = item;
        IsSelected = isSelected;
    }

    public object? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public string DisplayText => Item?.ToString() ?? string.Empty;
}
