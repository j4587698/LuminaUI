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
            new LuminaPieSlice { Value = 38 },
            new LuminaPieSlice { Value = 27 },
            new LuminaPieSlice { Value = 19 },
            new LuminaPieSlice { Value = 10 },
            new LuminaPieSlice { Value = 6 }
        ];

        PieSlices =
        [
            new LuminaPieSlice { Value = 30 },
            new LuminaPieSlice { Value = 22 },
            new LuminaPieSlice { Value = 18 },
            new LuminaPieSlice { Value = 14 },
            new LuminaPieSlice { Value = 16 }
        ];

        RadarSeries =
        [
            new LuminaRadarSeries
            {
                Values = [4, 5, 3, 4, 2, 5]
            },
            new LuminaRadarSeries
            {
                Values = [3, 4, 5, 3, 4, 2]
            }
        ];

        _texts.Labels(DonutSlices, "Direct", "Organic", "Referral", "Social", "Other");
        _texts.Labels(PieSlices, "North", "South", "East", "West", "Central");
        _texts.Titles(RadarSeries, "ModelA", "ModelB");
        // A new list each time: the chart repaints when Categories is replaced.
        _texts.Track(() => RadarCategories = ChartDemoTexts.GetAll("Speed", "Reliability", "Comfort", "Safety", "Efficiency", "Price"));
    }

    private readonly ChartDemoTexts _texts = new();

    public IList<LuminaPieSlice> DonutSlices { get; }

    public IList<LuminaPieSlice> PieSlices { get; }

    [ObservableProperty]
    private IReadOnlyList<string> _radarCategories = [];

    public IList<LuminaRadarSeries> RadarSeries { get; }
}
