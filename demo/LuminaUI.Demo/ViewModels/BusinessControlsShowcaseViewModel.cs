using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class BusinessControlsShowcaseViewModel : ObservableObject
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

    public ObservableCollection<string> LockedLabels { get; } =
    [
        "Pinned",
        "No delete"
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

    [ObservableProperty]
    private bool _isLoading = true;

    [RelayCommand]
    private void ToggleLoading()
    {
        IsLoading = !IsLoading;
        StatusText = IsLoading ? "Loading state enabled" : "Loading state cleared";
    }

    [RelayCommand]
    private void AddDefaultTags()
    {
        AddTag("Avalonia");
        AddTag("LuminaUI");
    }

    [RelayCommand]
    private void ResetSelection()
    {
        SelectedMembers.Clear();
        SelectedMembers.Add("Ava Chen");
        SelectedMembers.Add("Mia Johnson");
        StatusText = "Selection reset";
    }

    [RelayCommand]
    private void SubmitForm()
    {
        StatusText = $"Submitting {SelectedMembers.Count} member(s), {Labels.Count} tag(s)";
    }

    [RelayCommand]
    private void RunCloseButton()
    {
        StatusText = "Close button clicked";
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

    private void AddTag(string tag)
    {
        if (!Labels.Contains(tag))
        {
            Labels.Add(tag);
        }
    }
}
