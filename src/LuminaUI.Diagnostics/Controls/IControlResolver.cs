using Avalonia.Controls;

namespace LuminaUI.Diagnostics.Controls;

public interface IControlResolver
{
    ControlResolution Resolve(Control root, ControlIdentifier identifier);
}

public sealed record ControlResolution(
    ControlIdentifier Identifier,
    Control? Control,
    string? ErrorMessage)
{
    public bool Found => Control is not null;

    public static ControlResolution FoundControl(
        ControlIdentifier identifier,
        Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        return new ControlResolution(identifier, control, null);
    }

    public static ControlResolution NotFound(ControlIdentifier identifier) =>
        new(
            identifier,
            Control: null,
            ErrorMessage: $"Control '{identifier}' was not found.");
}
