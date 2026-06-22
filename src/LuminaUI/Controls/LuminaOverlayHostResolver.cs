using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

internal static class LuminaOverlayHostResolver
{
    public static ILuminaOverlayHost? FindDefault()
    {
        return (ILuminaOverlayHost?)LuminaShell.Current ?? LuminaTopView.Current ?? LuminaOverlayHost.Current;
    }

    public static ILuminaOverlayHost? FindFor(Control? owner)
    {
        if (owner == null)
        {
            return FindDefault();
        }

        if (owner is ILuminaOverlayHost directHost)
        {
            return directHost;
        }

        LuminaOverlayHost? overlayHost = owner.GetVisualAncestors().OfType<LuminaOverlayHost>().FirstOrDefault();
        if (overlayHost != null)
        {
            return overlayHost;
        }

        return (ILuminaOverlayHost?)LuminaShell.FindFor(owner) ?? FindDefault();
    }

    public static ILuminaOverlayHost? FindTopFor(Control? owner)
    {
        return (ILuminaOverlayHost?)FindOuterOverlayHostFor(owner)
            ?? (ILuminaOverlayHost?)FindOuterShellFor(owner)
            ?? (ILuminaOverlayHost?)LuminaTopView.Current
            ?? LuminaShell.Current;
    }

    private static LuminaOverlayHost? FindOuterOverlayHostFor(Control? owner)
    {
        if (owner == null)
        {
            return null;
        }

        LuminaOverlayHost? outerOverlayHost = owner.GetVisualAncestors().OfType<LuminaOverlayHost>().LastOrDefault();
        if (outerOverlayHost != null)
        {
            return outerOverlayHost;
        }

        return owner as LuminaOverlayHost;
    }

    private static LuminaShell? FindOuterShellFor(Control? owner)
    {
        if (owner == null)
        {
            return null;
        }

        LuminaShell? outerShell = owner.GetVisualAncestors().OfType<LuminaShell>().LastOrDefault();
        if (outerShell != null)
        {
            return outerShell;
        }

        return owner as LuminaShell;
    }
}
