using System.Collections.Generic;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Charts;

namespace LuminaUI.Demo.ViewModels;

public partial class ChartsPieRadarShowcaseViewModel : ObservableObject
{
    public ChartsPieRadarShowcaseViewModel()
    {
        DonutSlices =
        [
            new LuminaPieSlice { Label = "Direct", Value = 38, Brush = Blue() },
            new LuminaPieSlice { Label = "Organic", Value = 27, Brush = Teal() },
            new LuminaPieSlice { Label = "Referral", Value = 19, Brush = Orange() },
            new LuminaPieSlice { Label = "Social", Value = 10, Brush = Pink() },
            new LuminaPieSlice { Label = "Other", Value = 6, Brush = Indigo() }
        ];

        PieSlices =
        [
            new LuminaPieSlice { Label = "North", Value = 30, Brush = Blue() },
            new LuminaPieSlice { Label = "South", Value = 22, Brush = Teal() },
            new LuminaPieSlice { Label = "East", Value = 18, Brush = Orange() },
            new LuminaPieSlice { Label = "West", Value = 14, Brush = Pink() },
            new LuminaPieSlice { Label = "Central", Value = 16, Brush = Indigo() }
        ];

        RadarCategories =
        [
            "Speed", "Reliability", "Comfort", "Safety", "Efficiency", "Price"
        ];

        RadarSeries =
        [
            new LuminaRadarSeries
            {
                Title = "Model A",
                Values = [4, 5, 3, 4, 2, 5],
                Stroke = Blue()
            },
            new LuminaRadarSeries
            {
                Title = "Model B",
                Values = [3, 4, 5, 3, 4, 2],
                Stroke = Teal()
            }
        ];
    }

    public IList<LuminaPieSlice> DonutSlices { get; }

    public IList<LuminaPieSlice> PieSlices { get; }

    public IList<string> RadarCategories { get; }

    public IList<LuminaRadarSeries> RadarSeries { get; }

    private static SolidColorBrush Blue() => new(Color.FromRgb(0x25, 0x63, 0xEB));

    private static SolidColorBrush Teal() => new(Color.FromRgb(0x14, 0xB8, 0xA6));

    private static SolidColorBrush Orange() => new(Color.FromRgb(0xF5, 0x9E, 0x0B));

    private static SolidColorBrush Pink() => new(Color.FromRgb(0xEC, 0x48, 0x99));

    private static SolidColorBrush Indigo() => new(Color.FromRgb(0x8B, 0x5C, 0xF6));
}