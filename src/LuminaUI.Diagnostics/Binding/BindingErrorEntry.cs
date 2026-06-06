namespace LuminaUI.Diagnostics.Binding;

public sealed record BindingErrorEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Area,
    string SourceType,
    string Message);
