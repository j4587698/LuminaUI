namespace LuminaUI.Diagnostics.Controls;

public enum ControlIdentifierKind
{
    Name,
    Type,
    IndexedType
}

public readonly record struct ControlIdentifier
{
    public ControlIdentifier(
        ControlIdentifierKind kind,
        string value,
        int index = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Control identifier value is required.", nameof(value));

        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Control identifier index cannot be negative.");

        Kind = kind;
        Value = value.Trim();
        Index = index;
    }

    public ControlIdentifierKind Kind { get; }

    public string Value { get; }

    public int Index { get; }

    public override string ToString() =>
        Kind switch
        {
            ControlIdentifierKind.Name => $"#{Value}",
            ControlIdentifierKind.IndexedType => $"{Value}[{Index}]",
            _ => Value
        };
}
