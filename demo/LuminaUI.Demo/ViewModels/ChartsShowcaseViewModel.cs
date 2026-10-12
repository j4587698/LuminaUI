using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Charts;

namespace LuminaUI.Demo.ViewModels;

public partial class ChartsShowcaseViewModel : ObservableObject
{
    public ChartsShowcaseViewModel()
    {
        AreaSeries =
        [
            new LuminaChartSeries
            {
                Title = "Revenue",
                ShowArea = true,
                ShowMarkers = true,
                Points = BuildWave(start: 24.0, amplitude: 10.0, period: 2.0, phase: 0.0, count: 13)
            },
            new LuminaChartSeries
            {
                Title = "Expenses",
                Points = BuildWave(start: 16.0, amplitude: 6.0, period: 3.0, phase: 1.0, count: 13)
            }
        ];

        CompactSeries =
        [
            new LuminaChartSeries
            {
                Title = "Visits",
                ShowMarkers = true,
                Points = BuildBlips(count: 9)
            }
        ];

        StressSeries =
        [
            new LuminaChartSeries
            {
                Title = "200k samples",
                ShowArea = true,
                Points = BuildStressWave(200_000)
            }
        ];

        LiveSeries =
        [
            new LuminaChartSeries
            {
                Title = "Live",
                ShowArea = true,
                ShowMarkers = true,
                Points = new ObservableCollection<LuminaChartPoint>()
            }
        ];

        StepSeries =
        [
            new LuminaChartSeries
            {
                Title = "Queue length",
                IsStep = true,
                ShowMarkers = true,
                Points = ToPoints([4.0, 7.0, 7.0, 12.0, 12.0, 9.0, 9.0, 15.0, 15.0, 11.0])
            }
        ];

        SparkSeries =
        [
            new LuminaChartSeries
            {
                Title = "Spark",
                StrokeThickness = 1.5,
                Points = BuildStressWave(48)
            }
        ];
    }

    public IList<LuminaChartSeries> AreaSeries { get; }

    public IList<LuminaChartSeries> CompactSeries { get; }

    public IList<LuminaChartSeries> StressSeries { get; }

    public IList<LuminaChartSeries> LiveSeries { get; }

    public IList<LuminaChartSeries> StepSeries { get; }

    public IList<LuminaChartSeries> SparkSeries { get; }

    private static IList<LuminaChartPoint> BuildWave(double start, double amplitude, double period, double phase, int count)
    {
        var points = new List<LuminaChartPoint>(count);
        for (int i = 0; i < count; i++)
        {
            double y = start + amplitude * Math.Sin((i + phase) * period);
            points.Add(new LuminaChartPoint(i + 1.0, Math.Round(y, 1)));
        }

        return points;
    }

    private static IList<LuminaChartPoint> ToPoints(IReadOnlyList<double> values)
    {
        var points = new List<LuminaChartPoint>(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            points.Add(new LuminaChartPoint(i + 1.0, values[i]));
        }

        return points;
    }

    private static IList<LuminaChartPoint> BuildBlips(int count)
    {
        var points = new List<LuminaChartPoint>(count);
        for (int i = 0; i < count; i++)
        {
            double y = 32.0 + Math.Abs(Math.Sin(i * 1.3) * 24.0) + (i % 2 == 0 ? 6.0 : 0.0);
            points.Add(new LuminaChartPoint(i + 1.0, Math.Round(y, 1)));
        }

        return points;
    }

    private static IList<LuminaChartPoint> BuildStressWave(int count)
    {
        var points = new List<LuminaChartPoint>(count);
        var random = new Random(42);
        for (int i = 0; i < count; i++)
        {
            double envelope = 1.0 + 0.15 * Math.Sin(i * 0.002);
            double fast = Math.Sin(i * 0.05) * 12.0;
            double medium = Math.Sin(i * 0.008) * 22.0;
            double y = 50.0 + (medium + fast) * envelope + (random.NextDouble() - 0.5) * 4.0;
            points.Add(new LuminaChartPoint(i + 1.0, y));
        }

        return points;
    }
}