using System.Collections;
using System.Globalization;
using System.Text.Json.Nodes;

namespace LuminaUI.Diagnostics.Serialization;

public sealed class ValueFormatter
{
    private const int DefaultMaxStringLength = 200;
    private const int DefaultMaxEnumerableItems = 20;
    private const int DefaultMaxDepth = 1;

    public JsonObject Format(
        object? value,
        ValueFormatOptions? options = null)
    {
        var normalizedOptions = Normalize(options);
        return FormatCore(value, normalizedOptions);
    }

    private static ValueFormatOptions Normalize(ValueFormatOptions? options)
    {
        var source = options ?? new ValueFormatOptions();

        return new ValueFormatOptions
        {
            MaxStringLength = source.MaxStringLength > 0 ? source.MaxStringLength : DefaultMaxStringLength,
            MaxEnumerableItems = source.MaxEnumerableItems > 0 ? source.MaxEnumerableItems : DefaultMaxEnumerableItems,
            MaxDepth = source.MaxDepth >= 0 ? source.MaxDepth : DefaultMaxDepth
        };
    }

    private static JsonObject FormatCore(
        object? value,
        ValueFormatOptions options)
    {
        if (value is null)
            return Create("null", type: null, value: null);

        return value switch
        {
            string text => FormatString(text, options),
            char character => Create("string", typeof(char).FullName, character.ToString()),
            bool boolean => Create("boolean", typeof(bool).FullName, boolean),
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                Create("number", value.GetType().FullName, value),
            Enum enumValue => Create("enum", value.GetType().FullName, enumValue.ToString()),
            DateTime dateTime => Create("dateTime", typeof(DateTime).FullName, dateTime.ToString("O", CultureInfo.InvariantCulture)),
            DateTimeOffset dateTimeOffset => Create("dateTimeOffset", typeof(DateTimeOffset).FullName, dateTimeOffset.ToString("O", CultureInfo.InvariantCulture)),
            DateOnly dateOnly => Create("date", typeof(DateOnly).FullName, dateOnly.ToString("O", CultureInfo.InvariantCulture)),
            TimeOnly timeOnly => Create("time", typeof(TimeOnly).FullName, timeOnly.ToString("O", CultureInfo.InvariantCulture)),
            TimeSpan timeSpan => Create("timeSpan", typeof(TimeSpan).FullName, timeSpan.ToString("c", CultureInfo.InvariantCulture)),
            Guid guid => Create("guid", typeof(Guid).FullName, guid.ToString("D")),
            Type type => Create("type", typeof(Type).FullName, type.FullName ?? type.Name),
            IEnumerable enumerable when value is not string => FormatEnumerable(enumerable, value.GetType(), options),
            _ => FormatObject(value, options)
        };
    }

    private static JsonObject FormatString(
        string value,
        ValueFormatOptions options)
    {
        var truncated = value.Length > options.MaxStringLength;
        var formatted = truncated
            ? value[..options.MaxStringLength]
            : value;

        var json = Create("string", typeof(string).FullName, formatted);
        json["truncated"] = truncated;
        return json;
    }

    private static JsonObject FormatEnumerable(
        IEnumerable enumerable,
        Type type,
        ValueFormatOptions options)
    {
        var json = Create("sequence", type.FullName, value: null);
        var items = new JsonArray();
        var truncated = false;

        if (options.MaxDepth == 0)
        {
            json["items"] = items;
            json["truncated"] = true;
            return json;
        }

        var childOptions = options with { MaxDepth = options.MaxDepth - 1 };

        try
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (index >= options.MaxEnumerableItems)
                {
                    truncated = true;
                    break;
                }

                items.Add(FormatCore(item, childOptions));
                index++;
            }
        }
        catch (Exception ex)
        {
            json["error"] = $"Enumeration failed: {ex.Message}";
        }

        json["items"] = items;
        json["truncated"] = truncated;
        return json;
    }

    private static JsonObject FormatObject(
        object value,
        ValueFormatOptions options)
    {
        var text = TryFormatToString(value);
        var json = Create("object", value.GetType().FullName, Truncate(text, options.MaxStringLength, out var truncated));
        json["truncated"] = truncated;
        return json;
    }

    private static string TryFormatToString(object value)
    {
        try
        {
            return value.ToString() ?? "";
        }
        catch (Exception ex)
        {
            return $"ToString failed: {ex.Message}";
        }
    }

    private static string Truncate(
        string value,
        int maxLength,
        out bool truncated)
    {
        truncated = value.Length > maxLength;
        return truncated ? value[..maxLength] : value;
    }

    private static JsonObject Create(
        string kind,
        string? type,
        object? value)
    {
        var json = new JsonObject
        {
            ["kind"] = kind
        };

        if (type is not null)
            json["type"] = type;

        json["value"] = value switch
        {
            null => null,
            bool boolean => boolean,
            byte number => number,
            sbyte number => number,
            short number => number,
            ushort number => number,
            int number => number,
            uint number => number,
            long number => number,
            ulong number => number,
            float number => number,
            double number => number,
            decimal number => number,
            string text => text,
            _ => value.ToString()
        };

        return json;
    }
}

public sealed record ValueFormatOptions
{
    public int MaxStringLength { get; init; } = 200;

    public int MaxEnumerableItems { get; init; } = 20;

    public int MaxDepth { get; init; } = 1;
}
