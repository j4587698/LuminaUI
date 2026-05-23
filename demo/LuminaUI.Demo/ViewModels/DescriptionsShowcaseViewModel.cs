using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class DescriptionsShowcaseViewModel : ObservableObject
{
    public ObservableCollection<DescriptionItemViewModel> ProjectDetails { get; } =
    [
        new("Project", "LuminaUI"),
        new("Version", "12.0 preview"),
        new("Platform", "Desktop / Mobile / Browser"),
        new("Owner", "Design system team"),
        new("Status", "Active"),
        new("Updated", "2026-05-11")
    ];

    public ObservableCollection<DescriptionItemViewModel> Metrics { get; } =
    [
        new("Components", "64"),
        new("Tokens", "128"),
        new("Themes", "2"),
        new("Coverage", "AOT ready")
    ];
}

public sealed record DescriptionItemViewModel(string Label, string Value);

