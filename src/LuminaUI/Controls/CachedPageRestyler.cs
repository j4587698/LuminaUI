using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace LuminaUI.Controls;

/// <summary>
/// Works around lost <see cref="Style"/> setters on pages that the shell re-shows (cached route
/// pages, pages navigated back to).
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="NavigationPage"/> navigates away it removes the old page from the logical tree
/// immediately (which invalidates its styles) but keeps it in the visual tree for the page
/// transition and measures it there. <c>Layoutable.MeasureCore</c> calls
/// <see cref="StyledElement.ApplyStyling"/>, and at that point the page's styling chain no longer
/// reaches <c>Application.Styles</c> (its inheritance parent was cleared with the logical
/// parent). Nothing is applied, but every element in the page is marked as styled, so when the
/// page is attached again Avalonia skips styling it. Control themes recover (they are resolved
/// through resources), but every plain style setter - class styles such as
/// <c>TextBlock.Helper</c>, the chart brush styles, application styles - stays missing.
/// </para>
/// <para>
/// Changing an element's <see cref="StyledElement.Styles"/> is the public way to make Avalonia
/// invalidate the styles of the element and its whole subtree. Doing that when a previously
/// detached page is attached again - before its children are re-attached - lets them re-apply
/// their styles against the now complete chain.
/// </para>
/// </remarks>
internal static class CachedPageRestyler
{
    private const string NeverMatchingClass = "lumina-restyle-trigger";

    private static readonly ConditionalWeakTable<StyledElement, object> s_tracked = new();
    private static readonly ConditionalWeakTable<StyledElement, object> s_detached = new();

    // A style needs at least one setter for Styles.Add to invalidate the subtree; its selector
    // never matches anything, so the setter itself is never applied.
    private static readonly Style s_trigger = new(selector => selector.Class(NeverMatchingClass))
    {
        Setters = { new Setter(Visual.OpacityProperty, 1.0) }
    };

    public static void Track(StyledElement page)
    {
        if (s_tracked.TryGetValue(page, out _))
        {
            return;
        }

        s_tracked.Add(page, page);
        page.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
        page.AttachedToLogicalTree += OnAttachedToLogicalTree;
    }

    private static void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (sender is StyledElement page)
        {
            s_detached.AddOrUpdate(page, page);
        }
    }

    private static void OnAttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        // Raised after the page itself was (possibly wrongly) skipped by ApplyStyling but before
        // its logical children are attached, so invalidating here lets every child restyle
        // during its own attach.
        if (sender is not StyledElement page || !s_detached.TryGetValue(page, out _))
        {
            return;
        }

        s_detached.Remove(page);
        page.Styles.Add(s_trigger);
        page.Styles.Remove(s_trigger);
        page.ApplyStyling();
    }
}
