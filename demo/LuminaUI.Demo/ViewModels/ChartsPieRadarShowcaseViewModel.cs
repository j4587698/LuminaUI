using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Charts;

namespace LuminaUI.Demo.ViewModels;

public partial class ChartsPieRadarShowcaseViewModel : ObservableObject
{
    public ChartsPieRadarShowcaseViewModel()
    {
        // Slice and series brushes are left null on purpose: the charts fall back to the
        // LuminaChartColor1..8 palette, which follows the active light/dark theme.
        DonutSlices =
        [
            new LuminaPieSlice { Label = "Direct", Value = 38 },
            new LuminaPieSlice { Label = "Organic", Value = 27 },
            new LuminaPieSlice { Label = "Referral", Value = 19 },
            new LuminaPieSlice { Label = "Social", Value = 10 },
            new LuminaPieSlice { Label = "Other", Value = 6 }
        ];

        PieSlices =
        [
            new LuminaPieSlice { Label = "North", Value = 30 },
            new LuminaPieSlice { Label = "South", Value = 22 },
            new LuminaPieSlice { Label = "East", Value = 18 },
            new LuminaPieSlice { Label = "West", Value = 14 },
            new LuminaPieSlice { Label = "Central", Value = 16 }
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
                Values = [4, 5, 3, 4, 2, 5]
            },
            new LuminaRadarSeries
            {
                Title = "Model B",
                Values = [3, 4, 5, 3, 4, 2]
            }
        ];
    }

    public IList<LuminaPieSlice> DonutSlices { get; }

    public IList<LuminaPieSlice> PieSlices { get; }

    public IList<string> RadarCategories { get; }

    public IList<LuminaRadarSeries> RadarSeries { get; }
}
