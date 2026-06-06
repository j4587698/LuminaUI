using System.ComponentModel;
using System.Globalization;
using Avalonia;

namespace LuminaUI.Diagnostics.Interaction;

public sealed class PropertyValueConverter
{
    public bool TryConvert(
        string? value,
        Type targetType,
        out object? converted,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        converted = null;
        error = null;

        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is null)
        {
            if (Nullable.GetUnderlyingType(targetType) is not null || !targetType.IsValueType)
                return true;

            error = $"Cannot assign null to {targetType.Name}.";
            return false;
        }

        try
        {
            converted = ConvertCore(value, nonNullableType);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static object? ConvertCore(
        string value,
        Type targetType)
    {
        if (targetType == typeof(string))
            return value;

        if (targetType == typeof(bool))
            return bool.Parse(value);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, ignoreCase: true);

        if (targetType == typeof(Thickness))
            return Thickness.Parse(value);

        if (targetType == typeof(Guid))
            return Guid.Parse(value);

        if (targetType == typeof(TimeSpan))
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
            return converter.ConvertFromInvariantString(value);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
