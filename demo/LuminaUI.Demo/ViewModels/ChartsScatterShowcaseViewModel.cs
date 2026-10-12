using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Charts;

namespace LuminaUI.Demo.ViewModels;

public partial class ChartsScatterShowcaseViewModel : ObservableObject
{
    public ChartsScatterShowcaseViewModel()
    {
        ScatterSeries =
        [
            new LuminaScatterSeries { Title = "Cluster A", MarkerSize = 4.0, Points = BuildCluster(60, 30, 14) },
            new LuminaScatterSeries { Title = "Cluster B", MarkerSize = 4.0, Points = BuildCluster(55, 70, 12) }
        ];
    }

    public IList<LuminaChartSeries> ScatterSeries { get; }

    private static IList<LuminaChartPoint> BuildCluster(int count, double cy, double radius)
    {
        var random = new Random(3);
        var points = new List<LuminaChartPoint>(count);
        for (int i = 0; i < count; i++)
        {
            double angle = random.NextDouble() * Math.PI * 2.0;
            double r = radius * Math.Sqrt(random.NextDouble());
            points.Add(new LuminaChartPoint(cy + Math.Cos(angle) * r * 1.4, cy + Math.Sin(angle) * r));
        }

        return points;
    }
}