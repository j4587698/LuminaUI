using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class FormShowcaseViewModel : ObservableObject
{
    public ObservableCollection<string> TeamMembers { get; } =
    [
        "Ava Chen",
        "Noah Smith",
        "Mia Johnson",
        "Leo Wang",
        "Sofia Garcia"
    ];

    public ObservableCollection<string> SelectedMembers { get; } =
    [
        "Ava Chen",
        "Mia Johnson"
    ];

    public ObservableCollection<string> Labels { get; } =
    [
        "Design",
        "AOT",
        "Mobile"
    ];

    public ObservableCollection<string> Priorities { get; } =
    [
        "Normal",
        "High",
        "Urgent"
    ];

    public ObservableCollection<string> SelectedPriorities { get; } =
    [
        "High"
    ];

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void SubmitForm()
    {
        StatusText = $"Submitting {SelectedMembers.Count} member(s), {Labels.Count} tag(s)";
    }

    [RelayCommand]
    private void TagAdded(string tag)
    {
        StatusText = $"Tag added: {tag}";
    }

    [RelayCommand]
    private void TagRemoved(object tag)
    {
        StatusText = $"Tag removed: {tag}";
    }
}
