using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Charts;

namespace LuminaUI.Demo.ViewModels;

public partial class ChartsComboShowcaseViewModel : ObservableObject
{
    public ChartsComboShowcaseViewModel()
    {
        ComboSeries =
        [
            new LuminaBarSeries
            {
                Points = ToPoints([12, 18, 15, 24, 21, 30, 26, 35, 31, 40, 36, 44])
            },
            new LuminaChartSeries
            {
                StrokeThickness = 2.5,
                ShowMarkers = true,
                Points = ToPoints([13.0, 16.5, 18.0, 20.0, 23.0, 25.5, 28.5, 30.5, 33.0, 35.5, 38.5, 40.0])
            }
        ];

        MixedSeries =
        [
            new LuminaChartSeries
            {
                ShowArea = true,
                Points = BuildWave(24.0, 10.0, 0.9)
            },
            new LuminaSplineSeries
            {
                StrokeThickness = 2.0,
                Points = BuildWave(16.0, 6.0, 1.4)
            },
            new LuminaScatterSeries
            {
                MarkerSize = 5.0,
                Points = BuildBlips(7)
            }
        ];

        _texts.Titles(ComboSeries, "Revenue", "MovingAverage");
        _texts.Titles(MixedSeries, "Signal", "Trend", "Anomalies");
    }

    private readonly ChartDemoTexts _texts = new();

    public IList<LuminaChartSeries> ComboSeries { get; }

    public IList<LuminaChartSeries> MixedSeries { get; }

    private static IList<LuminaChartPoint> ToPoints(IReadOnlyList<double> values)
    {
        var points = new List<LuminaChartPoint>(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            points.Add(new LuminaChartPoint(i + 1.0, values[i]));
        }

        return points;
    }

    private static IList<LuminaChartPoint> BuildWave(double start, double amplitude, double period)
    {
        var points = new List<LuminaChartPoint>(12);
        for (int i = 0; i < 12; i++)
        {
            double y = start + amplitude * Math.Sin((i + 1) * period) + (i % 3) * 0.8;
            points.Add(new LuminaChartPoint(i + 1.0, Math.Round(y, 1)));
        }

        return points;
    }

    private static IList<LuminaChartPoint> BuildBlips(int count)
    {
        var random = new Random(11);
        var points = new List<LuminaChartPoint>(count);
        for (int i = 0; i < count; i++)
        {
            points.Add(new LuminaChartPoint(1.5 + random.NextDouble() * 11.0, 30.0 + random.NextDouble() * 12.0));
        }

        return points;
    }
}