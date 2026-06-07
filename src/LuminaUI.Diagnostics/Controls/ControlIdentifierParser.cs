namespace LuminaUI.Diagnostics.Controls;

public static class ControlIdentifierParser
{
    public static ControlIdentifier Parse(string controlId)
    {
        if (TryParse(controlId, out var identifier, out var error))
            return identifier;

        throw new FormatException(error);
    }

    public static bool TryParse(
        string? controlId,
        out ControlIdentifier identifier,
        out string? error)
    {
        identifier = default;

        var text = controlId?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            error = "Control identifier is required.";
            return false;
        }

        if (text.StartsWith('#'))
            return TryParseName(text, out identifier, out error);

        return TryParseType(text, out identifier, out error);
    }

    private static bool TryParseName(
        string text,
        out ControlIdentifier identifier,
        out string? error)
    {
        var name = text[1..].Trim();
        if (name.Length == 0)
        {
            identifier = default;
            error = "Control name identifier must include a name after '#'.";
            return false;
        }

        identifier = new ControlIdentifier(ControlIdentifierKind.Name, name);
        error = null;
        return true;
    }

    private static bool TryParseType(
        string text,
        out ControlIdentifier identifier,
        out string? error)
    {
        var bracketIndex = text.LastIndexOf('[');
        if (text.Contains(']') && bracketIndex < 0)
        {
            identifier = default;
            error = "Indexed type identifier must include '[' before ']'.";
            return false;
        }

        if (text.Contains(']') && !text.EndsWith(']'))
        {
            identifier = default;
            error = "Indexed type identifier must end with ']'.";
            return false;
        }

        if (bracketIndex < 0)
        {
            identifier = new ControlIdentifier(ControlIdentifierKind.Type, text);
            error = null;
            return true;
        }

        if (!text.EndsWith(']'))
        {
            identifier = default;
            error = "Indexed type identifier must end with ']'.";
            return false;
        }

        var typeName = text[..bracketIndex].Trim();
        var indexText = text[(bracketIndex + 1)..^1].Trim();

        if (typeName.Length == 0)
        {
            identifier = default;
            error = "Indexed type identifier must include a type name.";
            return false;
        }

        if (typeName.Contains('[') || typeName.Contains(']'))
        {
            identifier = default;
            error = "Indexed type identifier can only include one index segment.";
            return false;
        }

        if (!int.TryParse(indexText, out var index) || index < 0)
        {
            identifier = default;
            error = "Indexed type identifier must include a non-negative integer index.";
            return false;
        }

        identifier = new ControlIdentifier(ControlIdentifierKind.IndexedType, typeName, index);
        error = null;
        return true;
    }
}
