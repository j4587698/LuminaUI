using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using LuminaUI.Controls;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Drives <see cref="LuminaShell"/>'s responsive state machine by resizing a real window and
/// asserting on the <c>Effective*</c> surface plus the pseudo-classes the theme selects on.
/// </summary>
/// <remarks>
/// <para>
/// This is the highest-churn area of the library: menu state was lost across a responsive
/// transition, the compact pane collapsed wrongly, the footer reserved space while empty, and the
/// header toggles appeared when there was no menu to toggle. Every one of those fails silently —
/// no exception, no log line, just a shell that looks wrong — which is exactly the class of defect
/// a static test cannot see and a pixel snapshot catches only after someone re-records the baseline.
/// </para>
/// <para>
/// Not covered here: the four safe-area partitions (<see cref="LuminaShell.MenuLayoutSafeAreaPadding"/>
/// and friends). <c>LayoutSafeAreaPadding</c> has a private setter on both the shell and
/// <c>LuminaOverlayHost</c> and is fed from the platform insets manager, so it is always zero
/// headless. Testing it would require a production seam, which is a separate decision.
/// </para>
/// </remarks>
public sealed class ShellResponsiveTests
{
    /// <summary>Mirrors <c>LuminaShell.SmallScreenBreakpoint</c>, which is a private const.</summary>
    private const double Breakpoint = 768.0;

    private const double WideWidth = 1200.0;

    private const double NarrowWidth = 600.0;

    private const double Height = 800.0;

    [AvaloniaFact]
    public void SmallScreenPseudoClassFlipsExactlyAtTheBreakpoint()
    {
        using ShellHarness harness = new();

        harness.Resize(Breakpoint - 1);
        Assert.Contains(":small-screen", harness.Shell.Classes);

        harness.Resize(Breakpoint);
        Assert.DoesNotContain(":small-screen", harness.Shell.Classes);
    }

    [AvaloniaFact]
    public void NarrowScreenClosesTheMenuAndWideScreenRestoresIt()
    {
        using ShellHarness harness = new(configure: shell => shell.IsMenuOpen = true);

        Assert.True(harness.Shell.IsMenuOpen, "wide screen keeps the menu open");

        harness.Resize(NarrowWidth);
        Assert.False(harness.Shell.IsMenuOpen, "narrow screen closes the menu");

        harness.Resize(WideWidth);
        Assert.True(harness.Shell.IsMenuOpen, "returning to wide screen restores the menu");
    }

    [AvaloniaFact]
    public void WideScreenMenuStateSurvivesAUserClosingItWhileNarrow()
    {
        using ShellHarness harness = new(configure: shell => shell.IsMenuOpen = true);

        harness.Resize(NarrowWidth);
        harness.Shell.IsMenuOpen = false; // user closes it by hand while narrow

        harness.Resize(WideWidth);

        Assert.True(harness.Shell.IsMenuOpen, "the pre-transition state wins over the narrow-screen edit");
    }

    [AvaloniaFact]
    public void CompactPaneStateIsRestoredTogetherWithMenuState()
    {
        using ShellHarness harness = new(configure: shell =>
        {
            shell.IsCompactMenuEnabled = true;
            shell.IsMenuOpen = false;
        });

        harness.Resize(NarrowWidth);
        harness.Shell.IsCompactMenuEnabled = false; // perturb both while narrow
        harness.Shell.IsMenuOpen = true;

        harness.Resize(WideWidth);

        Assert.True(harness.Shell.IsCompactMenuEnabled, "compact state is restored");
        Assert.False(harness.Shell.IsMenuOpen, "menu state is restored");
    }

    [AvaloniaFact]
    public void StartingOnANarrowScreenDoesNotFabricateAWideScreenState()
    {
        // The first layout has PreviousSize.Width == 0, so the shell has never seen a wide layout
        // and must not record one. Widening should therefore fall back to the pane-mode default
        // (a Left pane keeps the menu open) rather than "restoring" a state it never captured.
        using ShellHarness harness = new(NarrowWidth, configure: shell => shell.IsMenuOpen = false);

        Assert.False(harness.Shell.IsMenuOpen, "narrow first frame closes the menu");

        harness.Resize(WideWidth);

        Assert.True(harness.Shell.IsMenuOpen, "no stale state was captured, so the pane default applies");
    }

    [AvaloniaFact]
    public void AutoResponsiveCanBeTurnedOffWithoutLosingTheMenu()
    {
        using ShellHarness harness = new(configure: shell =>
        {
            shell.IsMenuAutoResponsive = false;
            shell.IsMenuOpen = true;
        });

        harness.Resize(NarrowWidth);

        Assert.True(harness.Shell.IsMenuOpen, "the menu is left alone when auto-responsive is off");
        Assert.Contains(":small-screen", harness.Shell.Classes);
    }

    [AvaloniaFact]
    public void ExplicitPaneDisplayModeOverridesTheCompactFlag()
    {
        using ShellHarness harness = new(configure: shell =>
        {
            shell.PaneDisplayMode = LuminaShellPaneDisplayMode.LeftCompact;
            shell.IsCompactMenuEnabled = false;
        });

        Assert.Equal(LuminaShellPaneDisplayMode.LeftCompact, harness.Shell.EffectivePaneDisplayMode);
        Assert.Contains(":pane-left-compact", harness.Shell.Classes);
        Assert.DoesNotContain(":pane-left", harness.Shell.Classes);
    }

    [AvaloniaFact]
    public void CompactPaneIsCoercedToLeftWhenCompactIsNotAllowed()
    {
        using ShellHarness harness = new(configure: shell =>
        {
            shell.CanCompactMenu = false;
            shell.IsCompactMenuEnabled = true;
        });

        Assert.Equal(LuminaShellPaneDisplayMode.Left, harness.Shell.EffectivePaneDisplayMode);
        Assert.Contains(":pane-left", harness.Shell.Classes);
        Assert.DoesNotContain(":pane-left-compact", harness.Shell.Classes);
    }

    [AvaloniaFact]
    public void HidingChromeAndHeaderDrivesThePseudoClasses()
    {
        using ShellHarness harness = new(configure: shell => shell.IsShellChromeVisible = false);

        Assert.False(harness.Shell.EffectiveIsShellChromeVisible);
        Assert.Contains(":chromeless", harness.Shell.Classes);

        harness.Shell.IsShellChromeVisible = true;
        harness.Shell.IsShellHeaderVisible = false;

        Assert.False(harness.Shell.EffectiveIsShellHeaderVisible);
        Assert.Contains(":headerless", harness.Shell.Classes);
        Assert.DoesNotContain(":chromeless", harness.Shell.Classes);
    }

    [AvaloniaFact]
    public void HidingChromeCollapsesBothPaneLengthsToZero()
    {
        using ShellHarness harness = new(configure: shell =>
        {
            shell.OpenPaneLength = 300;
            shell.CompactPaneLength = 48;
        });

        Assert.Equal(300, harness.Shell.EffectiveOpenPaneLength);
        Assert.Equal(48, harness.Shell.EffectiveCompactPaneLength);

        harness.Shell.IsShellChromeVisible = false;

        Assert.Equal(0, harness.Shell.EffectiveOpenPaneLength);
        Assert.Equal(0, harness.Shell.EffectiveCompactPaneLength);
    }

    [AvaloniaFact]
    public void PaneToggleOnlyAppearsWhenTheMenuHasContent()
    {
        using ShellHarness harness = new();

        Assert.False(harness.Shell.EffectiveIsPaneToggleVisible, "no menu content means nothing to toggle");

        harness.Shell.MenuContent = new Border();

        Assert.True(harness.Shell.EffectiveIsPaneToggleVisible, "menu content makes the toggle reachable");
    }

    [AvaloniaFact]
    public void HeaderTitleFallsBackToThePageTitle()
    {
        // ActivePageTitle is derived, never an input: attaching the shell re-runs the page-metadata
        // sync (SetActivePage -> ApplyDefaultPageMetadata) and resets it from DefaultPageTitle, even
        // when AutoApplyPageMetadata is off. DefaultPageTitle is the knob that actually feeds it.
        using ShellHarness harness = new(configure: shell => shell.DefaultPageTitle = "Page A");

        Assert.Equal("Page A", harness.Shell.EffectiveHeaderTitle as string);

        harness.Shell.Title = "   ";
        Assert.Equal("Page A", harness.Shell.EffectiveHeaderTitle as string);

        harness.Shell.Title = "Shell";
        Assert.Equal("Shell", harness.Shell.EffectiveHeaderTitle as string);
    }

    /// <summary>
    /// Hosts a shell in a shown window so the theme template (and its <c>PART_OverlayHost</c> part)
    /// resolves, then resizes the window to drive <c>OnSizeChanged</c>.
    /// </summary>
    private sealed class ShellHarness : IDisposable
    {
        private readonly Window _window;

        public ShellHarness(double width = WideWidth, Action<LuminaShell>? configure = null)
        {
            Shell = new LuminaShell();
            configure?.Invoke(Shell);

            _window = new Window { Width = width, Height = Height, Content = Shell };
            _window.Show();

            Resize(width);
        }

        public LuminaShell Shell { get; }

        public void Resize(double width)
        {
            _window.Width = width;
            _window.Height = Height;
            _window.Measure(new Size(width, Height));
            _window.Arrange(new Rect(0, 0, width, Height));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(width, Shell.Bounds.Width);
        }

        public void Dispose() => _window.Close();
    }
}
