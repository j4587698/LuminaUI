using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public record MemberModel(string Name, string Initials, string Role)
{
    public override string ToString() => Name;
}

public partial class MultiSelectShowcaseViewModel : ObservableObject
{
    public IReadOnlyList<string> Priorities { get; } =
    [
        "Normal",
        "High",
        "Urgent"
    ];

    public ObservableCollection<string> SelectedPriorities { get; } =
    [
        "High"
    ];

    public IReadOnlyList<string> TeamMembers { get; } =
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

    public IReadOnlyList<MemberModel> ComplexMembers { get; } =
    [
        new MemberModel("Ava Chen", "AC", "Designer"),
        new MemberModel("Noah Smith", "NS", "Developer"),
        new MemberModel("Mia Johnson", "MJ", "Product Manager"),
        new MemberModel("Leo Wang", "LW", "QA Engineer"),
        new MemberModel("Sofia Garcia", "SG", "Marketing")
    ];

    public ObservableCollection<MemberModel> SelectedComplexMembers { get; } = [];

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void ResetSelection()
    {
        SelectedMembers.Clear();
        SelectedMembers.Add("Ava Chen");
        SelectedMembers.Add("Mia Johnson");

        SelectedPriorities.Clear();
        SelectedPriorities.Add("High");

        SelectedComplexMembers.Clear();

        StatusText = "Selection reset";
    }
}
