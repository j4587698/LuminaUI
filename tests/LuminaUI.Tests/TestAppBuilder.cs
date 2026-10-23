using Avalonia;
using Avalonia.Headless;
using LuminaUI;

[assembly: AvaloniaTestApplication(typeof(LuminaUI.Tests.TestAppBuilder))]

namespace LuminaUI.Tests;

/// <summary>
/// Hosts the headless application every <c>[AvaloniaFact]</c> runs inside.
/// </summary>
/// <remarks>
/// Only <see cref="LuminaTheme"/> is registered, on purpose. LuminaTheme embeds the Fluent base
/// theme it patches, and the whole suite is meant to fail loudly if that ever stops working —
/// adding a standalone <c>FluentTheme</c> here would hide exactly the regression we care about.
/// </remarks>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>The application shell used by the headless tests.</summary>
public sealed class TestApp : Application
{
    /// <inheritdoc />
    public override void Initialize() => Styles.Add(new LuminaTheme());
}
