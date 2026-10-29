using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Charts;

namespace LuminaUI.Demo.ViewModels;

public partial class ChartsSplineShowcaseViewModel : ObservableObject
{
    public ChartsSplineShowcaseViewModel()
    {
        SplineSeries =
        [
            new LuminaSplineSeries { ShowArea = true, Points = BuildWave(24.0, 10.0, 0.9) },
            new LuminaSplineSeries { Points = BuildWave(16.0, 7.0, 1.3) }
        ];

        _texts.Titles(SplineSeries, "Demand", "Forecast");
    }

    private readonly ChartDemoTexts _texts = new();

    public IList<LuminaChartSeries> SplineSeries { get; }

    private static IList<LuminaChartPoint> BuildWave(double start, double amplitude, double period)
    {
        var points = new List<LuminaChartPoint>(9);
        for (int i = 0; i < 9; i++)
        {
            double y = start + amplitude * Math.Sin((i + 1) * period) + (i % 3) * 0.8;
            points.Add(new LuminaChartPoint(i + 1.0, Math.Round(y, 1)));
        }

        return points;
    }
}