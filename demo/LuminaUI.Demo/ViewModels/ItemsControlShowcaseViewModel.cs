using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class ItemsControlShowcaseViewModel : ObservableObject
{
    private int _nextCardIndex = 49;

    public ObservableCollection<VirtualFlowCardViewModel> CompactCards { get; } = [];

    public ObservableCollection<VirtualFlowCardViewModel> MasonryCards { get; } = [];

    public ItemsControlShowcaseViewModel()
    {
        for (var index = 1; index <= 48; index++)
        {
            CompactCards.Add(CreateCard(index, compact: true));
            MasonryCards.Add(CreateCard(index, compact: false));
        }
    }

    [RelayCommand]
    private void AddCards()
    {
        for (var count = 0; count < 6; count++)
        {
            CompactCards.Add(CreateCard(_nextCardIndex, compact: true));
            MasonryCards.Add(CreateCard(_nextCardIndex, compact: false));
            _nextCardIndex++;
        }
    }

    [RelayCommand]
    private void ResetCards()
    {
        CompactCards.Clear();
        MasonryCards.Clear();
        _nextCardIndex = 49;

        for (var index = 1; index <= 48; index++)
        {
            CompactCards.Add(CreateCard(index, compact: true));
            MasonryCards.Add(CreateCard(index, compact: false));
        }
    }

    private static VirtualFlowCardViewModel CreateCard(int index, bool compact)
    {
        var paletteIndex = index % 6;
        var title = $"LUM-{index:000}";
        var status = paletteIndex switch
        {
            0 => "Design",
            1 => "Ready",
            2 => "Review",
            3 => "Blocked",
            4 => "Build",
            _ => "Draft"
        };

        var summary = compact
            ? $"Virtual item {index:000}"
            : CreateMasonrySummary(index);

        var height = compact ? 92 : 118 + index % 5 * 28;
        return new VirtualFlowCardViewModel(title, status, summary, height);
    }

    private static string CreateMasonrySummary(int index)
    {
        return (index % 4) switch
        {
            0 => "Adaptive card content with a short status block.",
            1 => "A taller item with preview text, metadata, and room for actions.",
            2 => "Dense operational card for scan-heavy layouts.",
            _ => "Mixed-height content keeps columns balanced while the panel only realizes the viewport."
        };
    }
}

public partial class VirtualFlowCardViewModel(
    string title,
    string status,
    string summary,
    double contentHeight) : ObservableObject
{
    public string Title { get; } = title;

    public string Status { get; } = status;

    public string Summary { get; } = summary;

    public double ContentHeight { get; } = contentHeight;
}