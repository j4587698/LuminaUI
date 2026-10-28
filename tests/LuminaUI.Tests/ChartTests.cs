using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using LuminaUI.Charts;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Regression tests for the custom-drawn chart controls in <c>LuminaUI.Charts</c>.
/// </summary>
public class ChartTests
{
    private const int Width = 400;
    private const int Height = 300;

    [AvaloniaFact]
    public void DegenerateAxisRangesRenderAndTerminate()
    {
        // Each case used to stall the tick loop (y += step stops advancing once step drops below
        // the precision of y) or to produce an unbounded number of tick labels.
        var cases = new (string Name, Action<LuminaLineChart> Configure)[]
        {
            ("huge magnitude, tiny range", chart => chart.Series = Line((0, 1e16), (1, 1e16 + 2))),
            ("span overflows double", chart => chart.Series = Line((0, -1e308), (1, 1e308))),
            ("infinite fixed bounds", chart =>
            {
                chart.Series = Line((0, 1), (1, 2));
                chart.YAxis.Minimum = double.NegativeInfinity;
                chart.YAxis.Maximum = double.PositiveInfinity;
                chart.XAxis.Minimum = double.NaN;
            }),
            ("inverted fixed bounds", chart =>
            {
                chart.Series = Line((0, 1), (1, 2));
                chart.YAxis.Minimum = 10;
                chart.YAxis.Maximum = -10;
            }),
            ("tiny tick step", chart =>
            {
                chart.Series = Line((0, 0), (1, 1e6));
                chart.YAxis.TickStep = 1e-9;
            }),
            ("huge category range", chart =>
            {
                chart.Series = Line((0, 1), (1, 2));
                chart.XAxis.Kind = LuminaChartAxisKind.Category;
                chart.XAxis.Maximum = 1e12;
                chart.XAxis.TickStep = 1;
            }),
        };

        foreach (var (name, configure) in cases)
        {
            var chart = new LuminaLineChart { TransitionDuration = 0 };
            configure(chart);
            int completed = 0;
            chart.RenderCompleted += (_, _) => completed++;

            var stopwatch = Stopwatch.StartNew();
            Window window;
            // A regression here is an endless loop on the UI thread that keeps allocating tick
            // labels: it cannot be interrupted from the test, and on a machine with plenty of
            // RAM it grows to tens of GB before OutOfMemoryException. The guard kills the test
            // host early with a readable message instead.
            using (RunawayGuard.Start(name, MaxRenderAllocation, MaxRenderDuration))
            {
                window = Show(chart);
            }

            try
            {
                stopwatch.Stop();
                // RenderCompleted only fires when the render pass did not throw (the chart
                // swallows and traces render exceptions).
                Assert.True(completed > 0, $"'{name}': the render pass did not complete.");
                Assert.True(
                    stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                    $"'{name}': rendering took {stopwatch.Elapsed.TotalSeconds:0.0}s.");
            }
            finally
            {
                window.Close();
            }
        }
    }

    private const long MaxRenderAllocation = 512L * 1024 * 1024;
    private static readonly TimeSpan MaxRenderDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Watches allocations and wall time of a block that runs on the UI thread and cannot be
    /// cancelled. When either budget is exceeded the test host is killed after writing the reason
    /// to stderr, which the runner reports as a crashed test host.
    /// </summary>
    private sealed class RunawayGuard : IDisposable
    {
        private readonly Timer _timer;

        private RunawayGuard(string name, long maxAllocatedBytes, TimeSpan maxDuration)
        {
            long baseline = GC.GetTotalAllocatedBytes();
            var stopwatch = Stopwatch.StartNew();
            _timer = new Timer(
                _ =>
                {
                    long allocated = GC.GetTotalAllocatedBytes() - baseline;
                    if (allocated > maxAllocatedBytes || stopwatch.Elapsed > maxDuration)
                    {
                        // Kill rather than Environment.FailFast: FailFast goes through Windows
                        // Error Reporting and can stall for a minute writing a crash dump.
                        Console.Error.WriteLine(
                            $"ChartTests: '{name}' ran away while rendering "
                            + $"({allocated / (1024 * 1024)} MB allocated, {stopwatch.Elapsed.TotalSeconds:0}s). "
                            + "Tick generation most likely no longer terminates. Killing the test host.");
                        Console.Error.Flush();
                        Process.GetCurrentProcess().Kill();
                    }
                },
                null,
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(50));
        }

        public static RunawayGuard Start(string name, long maxAllocatedBytes, TimeSpan maxDuration) =>
            new(name, maxAllocatedBytes, maxDuration);

        public void Dispose() => _timer.Dispose();
    }

    [AvaloniaFact]
    public void AxisLabelsDoNotDependOnGridLines()
    {
        var chart = new LuminaLineChart
        {
            TransitionDuration = 0,
            ShowGridLines = false,
            AxisLabelBrush = Brushes.Black,
            Series = Line((0, 1000), (1, 5000), (2, 3000)),
        };
        // Keep the line well away from the label gutters so only labels can put ink there.
        chart.XAxis.Minimum = -1;
        chart.XAxis.Maximum = 3;
        chart.YAxis.Minimum = 0;
        chart.YAxis.Maximum = 6000;

        Window window = Show(chart);
        try
        {
            int withLabels = CountInkInLabelGutters(window);
            Assert.True(withLabels > 0, "Axis labels were not drawn while grid lines were hidden.");

            // Toggling at runtime must rebuild the cached labels, not just repaint them.
            chart.ShowAxisLabels = false;
            Assert.Equal(0, CountInkInLabelGutters(window));

            chart.ShowAxisLabels = true;
            Assert.Equal(withLabels, CountInkInLabelGutters(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SinglePointBarSeriesGetsAFullSlot()
    {
        var chart = new LuminaLineChart
        {
            TransitionDuration = 0,
            ShowGridLines = false,
            ShowAxisLabels = false,
            Series = Bars((0, 10)),
        };
        chart.XAxis.Kind = LuminaChartAxisKind.Category;

        Window window = Show(chart);
        try
        {
            int widest = WidestRedRun(window);
            // Previously the slot came from the gap between this series' own points, so a single
            // point produced a 1px bar.
            Assert.True(widest > Width / 3, $"Single bar is only {widest}px wide.");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BarsAroundAGapStayInsideTheirCategory()
    {
        var chart = new LuminaLineChart
        {
            TransitionDuration = 0,
            ShowGridLines = false,
            ShowAxisLabels = false,
            Series = Bars((0, 10), (1, double.NaN), (2, 10)),
        };
        chart.XAxis.Kind = LuminaChartAxisKind.Category;

        Window window = Show(chart);
        try
        {
            int widest = WidestRedRun(window);
            // Three categories share the plot width; with the NaN hole the old per-series gap
            // was two categories wide and the bars spilled into their neighbours.
            Assert.InRange(widest, 20, Width / 3);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LineChartReleasesSubscriptionsWhenDetached()
    {
        var points = new ObservableCollection<LuminaChartPoint> { new(0, 1), new(1, 2) };
        var series = new LuminaChartSeries { Points = points };
        var list = new ObservableCollection<LuminaChartSeries> { series };
        var axis = new LuminaChartAxis();
        var chart = new LuminaLineChart { Series = list, XAxis = axis, TransitionDuration = 0 };

        Window window = Show(chart);
        try
        {
            Assert.True(HandlerCount(list, nameof(list.CollectionChanged)) > 0);
            Assert.True(HandlerCount(points, nameof(points.CollectionChanged)) > 0);
            Assert.True(HandlerCount(series, nameof(series.PropertyChanged)) > 0);
            Assert.True(HandlerCount(axis, nameof(axis.PropertyChanged)) > 0);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();

            // A series edit or a Series swap while detached used to re-subscribe.
            series.Title = "edited while detached";
            chart.Series = new ObservableCollection<LuminaChartSeries> { series };
            chart.Series = list;

            Assert.Equal(0, HandlerCount(list, nameof(list.CollectionChanged)));
            Assert.Equal(0, HandlerCount(points, nameof(points.CollectionChanged)));
            Assert.Equal(0, HandlerCount(series, nameof(series.PropertyChanged)));
            Assert.Equal(0, HandlerCount(axis, nameof(axis.PropertyChanged)));

            // Re-attaching restores them.
            window.Content = chart;
            Dispatcher.UIThread.RunJobs();
            Assert.True(HandlerCount(points, nameof(points.CollectionChanged)) > 0);
            Assert.True(HandlerCount(series, nameof(series.PropertyChanged)) > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PieAndRadarChartsDoNotSubscribeWhileDetached()
    {
        var slice = new LuminaPieSlice { Label = "A", Value = 1 };
        var slices = new ObservableCollection<LuminaPieSlice> { slice };
        var radarValues = new ObservableCollection<double> { 1, 2, 3 };
        var radarSeries = new LuminaRadarSeries { Values = radarValues };
        var radarList = new ObservableCollection<LuminaRadarSeries> { radarSeries };

        // Never attached: setting the data must not subscribe.
        var pie = new LuminaPieChart { Slices = slices };
        var radar = new LuminaRadarChart { Series = radarList };
        Assert.Equal(0, HandlerCount(slices, nameof(slices.CollectionChanged)));
        Assert.Equal(0, HandlerCount(slice, nameof(slice.PropertyChanged)));
        Assert.Equal(0, HandlerCount(radarList, nameof(radarList.CollectionChanged)));
        Assert.Equal(0, HandlerCount(radarValues, nameof(radarValues.CollectionChanged)));

        var panel = new StackPanel { Children = { pie, radar } };
        Window window = Show(panel);
        try
        {
            Assert.True(HandlerCount(slice, nameof(slice.PropertyChanged)) > 0);
            Assert.True(HandlerCount(radarValues, nameof(radarValues.CollectionChanged)) > 0);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, HandlerCount(slices, nameof(slices.CollectionChanged)));
            Assert.Equal(0, HandlerCount(slice, nameof(slice.PropertyChanged)));
            Assert.Equal(0, HandlerCount(radarList, nameof(radarList.CollectionChanged)));
            Assert.Equal(0, HandlerCount(radarSeries, nameof(radarSeries.PropertyChanged)));
            Assert.Equal(0, HandlerCount(radarValues, nameof(radarValues.CollectionChanged)));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ChartsGetADefaultSizeInsideUnboundedPanels()
    {
        // A custom-drawn control has nothing to measure, so inside a StackPanel (or ScrollViewer,
        // Auto row) the charts used to collapse to 0px and simply not show up.
        var line = new LuminaLineChart { Series = Line((0, 1), (1, 2)) };
        var pie = new LuminaPieChart();
        var radar = new LuminaRadarChart();
        var panel = new StackPanel { Children = { line, pie, radar } };

        Window window = Show(new ScrollViewer { Content = panel });
        try
        {
            Assert.Equal(200, line.Bounds.Height);
            Assert.Equal(240, pie.Bounds.Height);
            Assert.Equal(240, radar.Bounds.Height);
            // In a finite slot the charts keep stretching as before.
            Assert.Equal(Width, line.Bounds.Width, 0.5);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ChartsFollowTheirThemeVariantScope()
    {
        var light = new LuminaLineChart();
        var dark = new LuminaLineChart();
        var scope = new ThemeVariantScope { RequestedThemeVariant = ThemeVariant.Dark, Child = dark };

        Window window = Show(new StackPanel { Children = { light, scope } });
        try
        {
            Assert.True(Application.Current!.TryFindResource("LuminaDividerBrush", ThemeVariant.Light, out object? lightDivider));
            Assert.True(Application.Current!.TryFindResource("LuminaDividerBrush", ThemeVariant.Dark, out object? darkDivider));

            // Previously the chart brushes were resolved once against the application variant,
            // so a chart inside a dark scope of a light app stayed light.
            Assert.Equal(((ISolidColorBrush)lightDivider!).Color, ((ISolidColorBrush)light.GridLineBrush!).Color);
            Assert.Equal(((ISolidColorBrush)darkDivider!).Color, ((ISolidColorBrush)dark.GridLineBrush!).Color);
            // Series colors too (slot 2 = teal; slot 1 follows the app-wide accent override).
            Assert.NotEqual(
                ((ISolidColorBrush)ChartDrawing.ResolvePaletteBrush(light, 1)).Color,
                ((ISolidColorBrush)ChartDrawing.ResolvePaletteBrush(dark, 1)).Color);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void SplineNeverOvershootsOrBendsBackwards()
    {
        // A sharp spike between flat runs is where Catmull-Rom overshoots (and with uneven x
        // spacing loops backwards in x).
        double[] px = [0, 10, 12, 14, 60, 61, 100];
        double[] py = [50, 50, 0, 50, 50, 10, 10];

        Avalonia.Point[] curve = LuminaLineChart.BuildSplinePoints(px, py, 16);

        for (int index = 1; index < curve.Length; index++)
        {
            Assert.True(curve[index].X >= curve[index - 1].X, $"x goes backwards at sample {index}.");
        }

        for (int segment = 0; segment < px.Length - 1; segment++)
        {
            double low = Math.Min(py[segment], py[segment + 1]) - 1e-9;
            double high = Math.Max(py[segment], py[segment + 1]) + 1e-9;
            foreach (Avalonia.Point point in curve.Where(p => p.X >= px[segment] && p.X <= px[segment + 1]))
            {
                Assert.InRange(point.Y, low, high);
            }
        }

        Assert.Equal(new Avalonia.Point(px[^1], py[^1]), curve[^1]);
    }

    [AvaloniaFact]
    public void LongLegendsWrapInsteadOfRunningOffTheChart()
    {
        var chart = new LuminaLineChart { Width = 240, Height = 200 };
        var items = Enumerable.Range(1, 8)
            .Select(i => (Text: new FormattedText($"Series number {i}", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Black), Color: (IBrush)Brushes.Red))
            .ToList();

        double height = ChartDrawing.MeasureLegendHeight(items, new Size(240, 200));
        Rect band = ChartDrawing.ComputeLegendRect(items, LuminaChartLegendPosition.TopLeft, new Size(240, 200));

        Assert.True(height >= 3 * ChartDrawing.LegendBandHeight, $"Expected several rows, got {height}px.");
        Assert.True(band.Right <= 240, $"Legend band ends at {band.Right}px, past the 240px chart.");
        Assert.True(band.Height > ChartDrawing.LegendFontSize, "The band should cover every row.");
    }

    [AvaloniaFact]
    public void PointChangesFromAWorkerThreadAreMarshalledToTheUIThread()
    {
        var points = new ObservableCollection<LuminaChartPoint> { new(0, 1), new(1, 2) };
        var chart = new LuminaLineChart
        {
            TransitionDuration = 0,
            Series = new List<LuminaChartSeries> { new() { Points = points } },
        };

        Window window = Show(chart);
        try
        {
            int renders = 0;
            chart.RenderCompleted += (_, _) => renders++;
            Assert.False(ReadField<bool>(chart, "_isDirty"));

            // The handler used to mutate the chart's render state on the worker thread, racing
            // the render pass. Now it only posts to the UI thread, so nothing changes until the
            // dispatcher runs.
            // A dedicated thread, not Task.Run(...).Wait(): waiting on a task that has not started
            // yet may inline it on the waiting (UI) thread, which would test nothing. Exceptions
            // are captured so a regression fails the test instead of killing the test host.
            Exception? workerError = null;
            var worker = new Thread(() =>
            {
                try
                {
                    points.Add(new LuminaChartPoint(2, 3));
                }
                catch (Exception exception)
                {
                    workerError = exception;
                }
            });
            worker.Start();
            worker.Join();
            Assert.Null(workerError);
            Assert.False(ReadField<bool>(chart, "_isDirty"), "Chart state was modified on the worker thread.");

            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame()?.Dispose();
            Assert.True(renders > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PieHoverSkipsSlicesThatAreNotDrawn()
    {
        // Straight above the centre is angle 0, the start of the first slice. Zero-sweep slices
        // at the start used to win the hit test there; a null one then crashed the tooltip.
        var pie = new LuminaPieChart
        {
            InnerRadiusRatio = 0,
            Slices = new List<LuminaPieSlice>
            {
                new() { Label = "Zero", Value = 0 },
                null!,
                new() { Label = "NaN", Value = double.NaN },
                new() { Label = "Real", Value = 10 },
            },
        };

        Window window = Show(pie);
        try
        {
            window.MouseMove(new Point(Width / 2.0, Height / 4.0), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame()?.Dispose();

            Assert.Equal(3, ReadField<int>(pie, "_hoverSliceIndex"));

            // Replacing the list must drop the stale hover index.
            pie.Slices = new List<LuminaPieSlice> { null!, new() { Value = 1 } };
            Assert.Equal(-1, ReadField<int>(pie, "_hoverSliceIndex"));
            window.CaptureRenderedFrame()?.Dispose();
        }
        finally
        {
            window.Close();
        }
    }

    private static List<LuminaChartSeries> Line(params (double X, double Y)[] points) =>
        [new LuminaChartSeries { Points = points.Select(p => new LuminaChartPoint(p.X, p.Y)).ToList() }];

    private static List<LuminaChartSeries> Bars(params (double X, double Y)[] points) =>
        [new LuminaBarSeries
        {
            Stroke = Brushes.Red,
            Fill = Brushes.Red,
            Points = points.Select(p => new LuminaChartPoint(p.X, p.Y)).ToList(),
        }];

    private static Window Show(Control content)
    {
        var window = new Window
        {
            Width = Width,
            Height = Height,
            Background = Brushes.White,
            Content = content,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame()?.Dispose();
        return window;
    }

    /// <summary>
    /// Counts dark pixels in the left 8 columns and the bottom 10 rows: with no series data there,
    /// they can only come from the y and x axis labels.
    /// </summary>
    private static int CountInkInLabelGutters(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        return CountPixels(window, (x, y, w, h) => x < 8 || y >= h - 10, (r, g, b) => r < 128 && g < 128 && b < 128);
    }

    private static int WidestRedRun(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        using WriteableBitmap bitmap = Capture(window);
        using ILockedFramebuffer frame = bitmap.Lock();
        int widest = 0;
        // Scan a row just above the bottom edge, where every bar is present.
        int row = frame.Size.Height - 20;
        int run = 0;
        for (int x = 0; x < frame.Size.Width; x++)
        {
            (byte r, byte g, byte b) = PixelAt(frame, x, row);
            run = r > 180 && g < 90 && b < 90 ? run + 1 : 0;
            widest = Math.Max(widest, run);
        }

        return widest;
    }

    private static int CountPixels(Window window, Func<int, int, int, int, bool> region, Func<byte, byte, byte, bool> match)
    {
        using WriteableBitmap bitmap = Capture(window);
        using ILockedFramebuffer frame = bitmap.Lock();
        int count = 0;
        for (int y = 0; y < frame.Size.Height; y++)
        {
            for (int x = 0; x < frame.Size.Width; x++)
            {
                if (!region(x, y, frame.Size.Width, frame.Size.Height))
                {
                    continue;
                }

                (byte r, byte g, byte b) = PixelAt(frame, x, y);
                if (match(r, g, b))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static WriteableBitmap Capture(Window window) =>
        window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");

    private static (byte R, byte G, byte B) PixelAt(ILockedFramebuffer frame, int x, int y)
    {
        int offset = y * frame.RowBytes + x * 4;
        byte first = Marshal.ReadByte(frame.Address, offset);
        byte second = Marshal.ReadByte(frame.Address, offset + 1);
        byte third = Marshal.ReadByte(frame.Address, offset + 2);
        return frame.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (first, second, third)
            : (third, second, first);
    }

    /// <summary>Number of handlers attached to a field-like event, found by walking the type hierarchy.</summary>
    private static int HandlerCount(object target, string eventName)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                return (field.GetValue(target) as Delegate)?.GetInvocationList().Length ?? 0;
            }
        }

        throw new InvalidOperationException($"No backing field for event '{eventName}' on {target.GetType()}.");
    }

    private static T ReadField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
}
