using Avalonia.Data.Converters;

namespace LuminaUI.Converters;

public static class LuminaShellHeaderConverters
{
    public static readonly IValueConverter IsText =
        new FuncValueConverter<object?, bool>(value => value is string text && !string.IsNullOrWhiteSpace(text));

    public static readonly IValueConverter IsCustomContent =
        new FuncValueConverter<object?, bool>(value => value != null && value is not string);

    public static readonly IValueConverter ToText =
        new FuncValueConverter<object?, string?>(value => value as string);
}
