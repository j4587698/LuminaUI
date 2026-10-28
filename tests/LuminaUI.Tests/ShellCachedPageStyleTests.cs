using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Controls;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Pages the shell re-shows must come back fully styled.
/// </summary>
/// <remarks>
/// NavigationPage measures the outgoing page during its transition after it has already left the
/// logical tree; Avalonia then marks it as styled without applying anything and skips styling on
/// the next attach. Control themes recover, plain styles (class styles, app styles) did not.
/// </remarks>
public class ShellCachedPageStyleTests
{
    [AvaloniaFact]
    public void CachedRoutePagesKeepTheirStylesAcrossNavigation()
    {
        // TextBlock.Helper is a plain class Style in LuminaTheme (not part of a ControlTheme).
        var helper = new TextBlock { Classes = { "Helper" }, Text = "helper" };
        var shell = new LuminaShell();
        shell.RegisterRoute("a", () => new StackPanel { Children = { helper } });
        shell.RegisterRoute("b", () => new Border());

        var window = new Window { Width = 800, Height = 600, Content = shell };
        window.Show();
        try
        {
            shell.NavigateTo("a");
            Settle(window);
            Assert.Equal(TextWrapping.Wrap, helper.TextWrapping);
            object? styled = helper.Foreground;

            for (int round = 1; round <= 2; round++)
            {
                shell.NavigateTo("b");
                Settle(window);
                shell.NavigateTo("a");
                Settle(window);

                Assert.True(
                    helper.TextWrapping == TextWrapping.Wrap && Equals(styled, helper.Foreground),
                    $"Round {round}: the cached page came back without its Style setters "
                    + $"(TextWrapping={helper.TextWrapping}, Foreground={helper.Foreground}).");
            }
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Lets the asynchronous page replacement and its transition finish.</summary>
    private static void Settle(Window window)
    {
        for (int i = 0; i < 25; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame()?.Dispose();
            Thread.Sleep(20);
        }
    }
}
