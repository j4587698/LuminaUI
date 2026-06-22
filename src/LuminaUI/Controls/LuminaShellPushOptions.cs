using Avalonia.Animation;

namespace LuminaUI.Controls;

public sealed class LuminaShellPushOptions
{
    private IPageTransition? _pageTransition;

    public IPageTransition? PageTransition
    {
        get => _pageTransition;
        set
        {
            _pageTransition = value;
            HasPageTransitionOverride = true;
        }
    }

    public bool HasPageTransitionOverride { get; set; }

    public bool? ShowShellChrome { get; set; }

    public bool? ShowShellHeader { get; set; }

    public bool? ShowShellMenu { get; set; }

    public LuminaShellPushOptions()
    {
    }

    public LuminaShellPushOptions(IPageTransition? pageTransition)
    {
        PageTransition = pageTransition;
    }

    public static LuminaShellPushOptions WithoutPageTransition()
    {
        return new LuminaShellPushOptions(null);
    }

    public static LuminaShellPushOptions FullScreen()
    {
        return new LuminaShellPushOptions
        {
            ShowShellChrome = false
        };
    }
}
