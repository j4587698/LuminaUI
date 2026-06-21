using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

internal static class LuminaOverlayHostResolver
{
    public static ILuminaOverlayHost? FindDefault()
    {
        return (ILuminaOverlayHost?)LuminaShell.Current ?? LuminaTopView.Current;
    }

    public static ILuminaOverlayHost? FindFor(Control? owner)
    {
        if (owner == null)
        {
            return FindDefault();
        }

        LuminaShell? ancestorShell = owner as LuminaShell ?? owner.GetVisualAncestors().OfType<LuminaShell>().FirstOrDefault();
        if (ancestorShell != null)
        {
            return ancestorShell;
        }

        return (ILuminaOverlayHost?)LuminaTopView.FindFor(owner) ?? FindDefault();
    }

    public static ILuminaOverlayHost? FindTopFor(Control? owner)
    {
        return LuminaTopView.FindOuterFor(owner) ?? LuminaTopView.Current;
    }
}
