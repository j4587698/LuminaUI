using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Charts;

namespace LuminaUI.Demo.ViewModels;

public partial class ChartsBarShowcaseViewModel : ObservableObject
{
    public ChartsBarShowcaseViewModel()
    {
        BarSeries =
        [
            new LuminaBarSeries { Title = "Revenue", Points = ToPoints([8, 14, 11, 19, 16, 22, 18, 27, 24, 31]) },
            new LuminaBarSeries { Title = "Expenses", Points = ToPoints([5, 9, 8, 12, 11, 14, 13, 17, 16, 19]) }
        ];

        HistogramSeries =
        [
            new LuminaBarSeries
            {
                Title = "Frequency",
                Points = BuildHistogramBins(new Random(42), sampleCount: 400, binCount: 7)
            }
        ];

        ParetoSeries = BuildPareto(
            new Dictionary<string, double>
            {
                ["Electronics"] = 342,
                ["Apparel"] = 214,
                ["Groceries"] = 151,
                ["Books"] = 96,
                ["Toys"] = 58,
                ["Other"] = 34
            });
    }

    public IList<LuminaChartSeries> BarSeries { get; }

    public IList<LuminaChartSeries> HistogramSeries { get; }

    public IList<LuminaChartSeries> ParetoSeries { get; }

    private static IList<LuminaChartPoint> ToPoints(IReadOnlyList<int> values)
    {
        var points = new List<LuminaChartPoint>(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            points.Add(new LuminaChartPoint(i, values[i]));
        }

        return points;
    }

    private static IList<LuminaChartPoint> BuildHistogramBins(Random random, int sampleCount, int binCount)
    {
        var samples = new double[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            double u1 = random.NextDouble();
            double u2 = random.NextDouble();
            samples[i] = 50.0 + (Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)) * 12.0;
        }

        double min = samples.Min();
        double max = samples.Max();
        double width = (max - min) / binCount;
        var bins = new int[binCount];
        foreach (double value in samples)
        {
            int index = (int)Math.Min(binCount - 1, Math.Floor((value - min) / width));
            bins[index]++;
        }

        var points = new List<LuminaChartPoint>(binCount);
        for (int i = 0; i < binCount; i++)
        {
            points.Add(new LuminaChartPoint(i + 0.5, bins[i]));
        }

        return points;
    }

    private static IList<LuminaChartSeries> BuildPareto(Dictionary<string, double> categories)
    {
        var ordered = categories.OrderByDescending(pair => pair.Value).ToArray();
        double total = ordered.Sum(pair => pair.Value);

        var barPoints = new List<LuminaChartPoint>(ordered.Length);
        var linePoints = new List<LuminaChartPoint>(ordered.Length);
        double cumulative = 0.0;
        for (int i = 0; i < ordered.Length; i++)
        {
            barPoints.Add(new LuminaChartPoint(i + 0.5, ordered[i].Value));
            cumulative += ordered[i].Value;
            linePoints.Add(new LuminaChartPoint(i + 0.5, cumulative / total * 100.0));
        }

        return
        [
            new LuminaBarSeries
            {
                Title = "Volume",
                Points = barPoints,
                BarWidthRatio = 0.6
            },
            new LuminaChartSeries
            {
                Title = "Cumulative %",
                Stroke = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0xF5, 0x9E, 0x0B)),
                StrokeThickness = 2.0,
                ShowMarkers = true,
                Points = linePoints
            }
        ];
    }
}