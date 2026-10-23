using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Instantiates and renders every public control in the library once.
/// </summary>
/// <remarks>
/// This is the cheapest guard against the failure mode that hurts most in a control library: a
/// template that references a resource or a named part which no longer exists. Nothing throws when
/// the XAML is loaded — the control simply renders wrong — so the check has to actually push the
/// control through measure, arrange and draw.
/// </remarks>
public class ControlSmokeTests
{
    private const int SurfaceWidth = 320;
    private const int SurfaceHeight = 240;

    private readonly ITestOutputHelper _output;

    public ControlSmokeTests(ITestOutputHelper output) => _output = output;

    [AvaloniaFact]
    public void EveryControlCanBeCreatedAndRendered()
    {
        Type[] candidates = DiscoverControlTypes();
        string[] names = [.. candidates.Select(type => type.Name)];

        _output.WriteLine($"Rendered {candidates.Length} control types.");

        // Sanity-check the discovery itself, so a reflection regression cannot turn this test into
        // a no-op that always passes.
        string[] expected =
        [
            nameof(global::LuminaUI.Controls.LuminaAvatar),
            nameof(global::LuminaUI.Controls.LuminaCard),
            nameof(global::LuminaUI.Controls.LuminaBreadcrumb),
            nameof(global::LuminaUI.Controls.LuminaNavigationView),
            nameof(global::LuminaUI.Controls.LuminaShell),
        ];

        string[] notDiscovered = [.. expected.Where(name => !names.Contains(name))];

        Assert.True(
            notDiscovered.Length == 0,
            "Reflection over the LuminaUI assembly did not find these known controls, so the smoke "
            + "test would not actually exercise anything: "
            + string.Join(", ", notDiscovered));

        List<string> failures = [];
        Window window = new() { Width = SurfaceWidth, Height = SurfaceHeight };
        window.Show();

        try
        {
            foreach (Type type in candidates)
            {
                try
                {
                    Control control = (Control)Activator.CreateInstance(type)!;

                    window.Content = control;
                    window.Measure(new Size(SurfaceWidth, SurfaceHeight));
                    window.Arrange(new Rect(0, 0, SurfaceWidth, SurfaceHeight));
                    Dispatcher.UIThread.RunJobs();

                    window.CaptureRenderedFrame()?.Dispose();
                }
                catch (Exception exception)
                {
                    failures.Add($"{type.Name}: {exception.GetType().Name}: {exception.Message}");
                }
                finally
                {
                    window.Content = null;
                }
            }
        }
        finally
        {
            window.Close();
        }

        Assert.True(
            failures.Count == 0,
            $"{failures.Count} of {candidates.Length} controls failed to render:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, failures));
    }

    private static Type[] DiscoverControlTypes() =>
        [
            .. typeof(LuminaTheme)
                .Assembly
                .GetTypes()
                .Where(type => type is { IsAbstract: false, IsPublic: true })
                .Where(type => typeof(Control).IsAssignableFrom(type))
                // TopLevel-derived types (Window, EmbeddableControlRoot) cannot be nested as content.
                .Where(type => !typeof(TopLevel).IsAssignableFrom(type))
                .Where(type => type.GetConstructor(Type.EmptyTypes) is not null)
                .OrderBy(type => type.Name, StringComparer.Ordinal),
        ];
}
