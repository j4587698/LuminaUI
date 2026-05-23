using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class TagInputShowcaseViewModel : ObservableObject
{
    public ObservableCollection<string> LockedLabels { get; } =
    [
        "Pinned",
        "No delete"
    ];

    public ObservableCollection<string> Labels { get; } =
    [
        "Design",
        "AOT",
        "Mobile"
    ];

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void AddDefaultTags()
    {
        AddTag("Avalonia");
        AddTag("LuminaUI");
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
