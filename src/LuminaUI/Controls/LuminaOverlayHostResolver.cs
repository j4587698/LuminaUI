using Avalonia.Controls;

namespace LuminaUI.Controls;

internal static class LuminaOverlayHostResolver
{
    public static ILuminaOverlayHost? FindDefault()
    {
        LuminaTopView? topView = LuminaTopView.Current;
        if (topView?.UseSafeArea == true)
        {
            return topView;
        }

        return (ILuminaOverlayHost?)LuminaShell.Current ?? topView;
    }

    public static ILuminaOverlayHost? FindFor(Control? owner)
    {
        if (owner == null)
        {
            return FindDefault();
        }

        LuminaTopView? topView = LuminaTopView.FindOuterFor(owner);
        if (topView?.UseSafeArea == true)
        {
            return topView;
        }

        return (ILuminaOverlayHost?)LuminaShell.FindFor(owner) ?? topView ?? LuminaTopView.FindFor(owner);
    }

    public static ILuminaOverlayHost? FindTopFor(Control? owner)
    {
        return LuminaTopView.FindOuterFor(owner);
    }
}
