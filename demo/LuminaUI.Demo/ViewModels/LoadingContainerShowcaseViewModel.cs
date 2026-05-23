using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class LoadingContainerShowcaseViewModel : ObservableObject
{
    public ObservableCollection<string> TeamMembers { get; } =
    [
        "Ava Chen",
        "Noah Smith",
        "Mia Johnson",
        "Leo Wang",
        "Sofia Garcia"
    ];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _statusText = "Loading state enabled";

    [RelayCommand]
    private void ToggleLoading()
    {
        IsLoading = !IsLoading;
        StatusText = IsLoading ? "Loading state enabled" : "Loading state cleared";
    }
}
