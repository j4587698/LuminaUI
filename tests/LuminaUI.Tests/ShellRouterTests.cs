using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using LuminaUI.Controls;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Pins the routing contract of <see cref="LuminaShell"/>: registering, navigating, and the
/// page-caching rules that decide whether a route hands back a live page or a fresh one.
/// </summary>
/// <remarks>
/// <para>
/// Routing is the library's primary public API — the demo is built entirely on it — and it fails
/// quietly. <see cref="LuminaShell.NavigateTo(string)"/> answers a mistyped key with
/// <see langword="false"/> and otherwise does nothing, so a caller that ignores the return value
/// sees a click that simply never changes the page. The caching rules are the same kind of trap in
/// the other direction: they decide whether navigating back to a route restores the page you left
/// (same instance) or resets it (new instance), and getting that wrong looks like a data-loss bug
/// rather than a routing bug.
/// </para>
/// <para>
/// These drive an unhosted shell on purpose. <c>NavigateTo</c> only takes the async
/// <c>NavigationPage</c> reset path when the template has supplied <c>PART_NavigationHost</c>, so
/// leaving the shell out of a window keeps navigation synchronous and the assertions deterministic.
/// </para>
/// </remarks>
public sealed class ShellRouterTests
{
    [AvaloniaFact]
    public void NavigateToAnswersFalseForAnUnregisteredKeyAndChangesNothing()
    {
        LuminaShell shell = new();

        Assert.False(shell.NavigateTo("nope"));
        Assert.Null(shell.ActiveRouteContent);
        Assert.Null(shell.ActiveNavigationKey);
    }

    [AvaloniaFact]
    public void RegisterRouteRejectsBlankKeysAndMissingFactories()
    {
        LuminaShell shell = new();

        Assert.Throws<ArgumentException>(() => shell.RegisterRoute("   ", () => new Border()));
        Assert.Throws<ArgumentNullException>(() => shell.RegisterRoute("home", null!));
    }

    [AvaloniaFact]
    public void NavigateToActivatesTheRegisteredRoute()
    {
        LuminaShell shell = new();
        Border content = new();
        shell.RegisterRoute("home", () => content);

        Assert.True(shell.NavigateTo("home"));

        Assert.NotNull(shell.ActiveRouteContent);
        Assert.Equal("home", shell.ActiveNavigationKey);
        Assert.Same(content, Assert.IsType<ContentPage>(shell.ActiveRouteContent).Content);
    }

    [AvaloniaFact]
    public void CachedRoutesHandBackTheSamePageInstance()
    {
        LuminaShell shell = new();
        int factoryCalls = 0;
        shell.RegisterRoute("home", () =>
        {
            factoryCalls++;
            return new Border();
        });

        shell.NavigateTo("home");
        object? first = shell.ActiveRouteContent;

        shell.NavigateTo("home");

        Assert.Same(first, shell.ActiveRouteContent);
        Assert.Equal(1, factoryCalls);
    }

    [AvaloniaFact]
    public void DisablingCachePagesBuildsAFreshPageEveryTime()
    {
        LuminaShell shell = new() { CachePages = false };
        int factoryCalls = 0;
        shell.RegisterRoute("home", () =>
        {
            factoryCalls++;
            return new Border();
        });

        shell.NavigateTo("home");
        object? first = shell.ActiveRouteContent;

        shell.NavigateTo("home");

        Assert.NotSame(first, shell.ActiveRouteContent);
        Assert.Equal(2, factoryCalls);
    }

    [AvaloniaFact]
    public void RegisteringTheSameKeyAgainInvalidatesTheCachedPage()
    {
        LuminaShell shell = new();
        int factoryCalls = 0;
        shell.RegisterRoute("home", () =>
        {
            factoryCalls++;
            return new Border();
        });

        shell.NavigateTo("home");
        object? first = shell.ActiveRouteContent;

        shell.RegisterRoute("home", () =>
        {
            factoryCalls++;
            return new Border();
        });
        shell.NavigateTo("home");

        Assert.NotSame(first, shell.ActiveRouteContent);
        Assert.Equal(2, factoryCalls);
    }

    [AvaloniaFact]
    public void UnregisterRouteReportsWhetherItRemovedAnything()
    {
        LuminaShell shell = new();
        shell.RegisterRoute("home", () => new Border());

        Assert.True(shell.UnregisterRoute("home"));
        Assert.False(shell.UnregisterRoute("home"));
        Assert.False(shell.NavigateTo("home"));
    }

    [AvaloniaFact]
    public void ClearRoutesDropsEveryRoute()
    {
        LuminaShell shell = new();
        shell.RegisterRoute("home", () => new Border());
        shell.RegisterRoute("settings", () => new Border());

        shell.ClearRoutes();

        Assert.False(shell.NavigateTo("home"));
        Assert.False(shell.NavigateTo("settings"));
    }

    [AvaloniaFact]
    public void AFactoryThatReturnsNullIsReportedWithTheRouteKey()
    {
        LuminaShell shell = new();
        shell.RegisterRoute("broken", () => null!);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => shell.NavigateTo("broken"));

        Assert.Contains("broken", exception.Message, StringComparison.Ordinal);
    }
}
