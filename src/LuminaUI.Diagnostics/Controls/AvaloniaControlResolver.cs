using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace LuminaUI.Diagnostics.Controls;

public sealed class AvaloniaControlResolver : IControlResolver
{
    public ControlResolution Resolve(
        Control root,
        ControlIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(root);

        return identifier.Kind switch
        {
            ControlIdentifierKind.Name => ResolveByName(root, identifier),
            ControlIdentifierKind.Type => ResolveByType(root, identifier, index: 0),
            ControlIdentifierKind.IndexedType => ResolveByType(root, identifier, identifier.Index),
            _ => ControlResolution.NotFound(identifier)
        };
    }

    private static ControlResolution ResolveByName(
        Control root,
        ControlIdentifier identifier)
    {
        foreach (var control in EnumerateControls(root))
        {
            if (string.Equals(control.Name, identifier.Value, StringComparison.Ordinal))
                return ControlResolution.FoundControl(identifier, control);
        }

        return ControlResolution.NotFound(identifier);
    }

    private static ControlResolution ResolveByType(
        Control root,
        ControlIdentifier identifier,
        int index)
    {
        var currentIndex = 0;
        foreach (var control in EnumerateControls(root))
        {
            if (!IsTypeMatch(control, identifier.Value))
                continue;

            if (currentIndex == index)
                return ControlResolution.FoundControl(identifier, control);

            currentIndex++;
        }

        return ControlResolution.NotFound(identifier);
    }

    private static bool IsTypeMatch(
        Control control,
        string typeName)
    {
        var type = control.GetType();
        return string.Equals(type.Name, typeName, StringComparison.Ordinal)
            || string.Equals(type.FullName, typeName, StringComparison.Ordinal);
    }

    internal static IEnumerable<Control> EnumerateControls(Control root)
    {
        var seen = new HashSet<Control>(ReferenceEqualityComparer.Instance);

        foreach (var control in root.GetSelfAndVisualDescendants().OfType<Control>())
        {
            if (seen.Add(control))
                yield return control;
        }

        foreach (var control in root.GetSelfAndLogicalDescendants().OfType<Control>())
        {
            if (seen.Add(control))
                yield return control;
        }
    }
}
