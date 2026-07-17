using Avalonia;
using Avalonia.Controls.Presenters;
using Avalonia.Rendering;

namespace LuminaUI.Controls;

/// <summary>
/// Presents content without using the presenter itself as an input target.
/// Interactive descendants remain available for hit testing.
/// </summary>
public sealed class LuminaInputPassThroughContentPresenter : ContentPresenter, ICustomHitTest
{
    public bool HitTest(Point point)
    {
        return false;
    }
}
