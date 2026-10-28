using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Renders the third-party controls that the optional LuminaUI.ColorPicker, LuminaUI.DataGrid and
/// LuminaUI.TreeDataGrid packages restyle, with real data, in both theme variants.
/// </summary>
/// <remarks>
/// These packages only ship themes, so a broken template override or a resource that no longer
/// exists shows up when the restyled control is rendered, not when the package is built.
/// </remarks>
public class ExtensionSmokeTests
{
    private const int SurfaceWidth = 480;
    private const int SurfaceHeight = 360;

    public sealed record Row(string Name, int Value);

    private static readonly Row[] Rows = [new("Alpha", 1), new("Beta", 2), new("Gamma", 3)];

    public static TheoryData<string> Variants => ["Light", "Dark"];

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void ExtensionControlsRenderWithoutErrors(string variant)
    {
        var factories = new (string Name, Func<Control> Create)[]
        {
            ("ColorPicker", () => new global::Avalonia.Controls.ColorPicker()),
            ("ColorView", () => new ColorView()),
            ("DataGrid", () => new global::Avalonia.Controls.DataGrid { AutoGenerateColumns = true, ItemsSource = Rows }),
            ("TreeDataGrid", () => new global::Avalonia.Controls.TreeDataGrid
            {
                Source = new FlatTreeDataGridSource<Row>(Rows)
                {
                    Columns =
                    {
                        new TextColumn<Row, string>("Name", row => row.Name),
                        new TextColumn<Row, int>("Value", row => row.Value),
                    },
                },
            }),
        };

        List<string> failures = [];
        using LogCapture log = LogCapture.Start();
        var window = new Window
        {
            Width = SurfaceWidth,
            Height = SurfaceHeight,
            RequestedThemeVariant = variant == "Dark" ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light,
        };
        window.Show();

        try
        {
            foreach (var (name, create) in factories)
            {
                try
                {
                    Control control = create();
                    window.Content = control;
                    Dispatcher.UIThread.RunJobs();
                    window.CaptureRenderedFrame()?.Dispose();

                    if (control.Bounds.Width <= 0 || control.Bounds.Height <= 0)
                    {
                        failures.Add($"{name}: rendered with an empty size {control.Bounds.Size}.");
                    }
                }
                catch (Exception exception)
                {
                    failures.Add($"{name}: {exception.GetType().Name}: {exception.Message}");
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

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        Assert.True(
            log.Entries.Count == 0,
            $"Rendering the extension controls logged {log.Entries.Count} warnings/errors:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, log.Entries.Distinct().Take(50)));
    }
}
