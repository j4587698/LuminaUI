using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class BreadcrumbShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _lastNavigation = "No breadcrumb item selected";

    public ObservableCollection<BreadcrumbNodeViewModel> Nodes { get; }

    public BreadcrumbShowcaseViewModel()
    {
        var command = new RelayCommand<string?>(Navigate);
        Nodes =
        [
            new("⌂", "Workspace", "Workspace", command),
            new("◫", "Products", "Products", command),
            new("●", "LuminaUI", "LuminaUI", command),
            new("◆", "Release", "Release", command)
        ];
    }

    [RelayCommand]
    private void Navigate(string? key)
    {
        LastNavigation = key is null ? "No breadcrumb item selected" : $"Selected: {key}";
    }
}

public sealed record BreadcrumbNodeViewModel(
    string Icon,
    string Title,
    string Key,
    IRelayCommand<string?> Command);

