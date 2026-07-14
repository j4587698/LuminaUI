using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public class MemberModel
{
    public string Name { get; }
    public string Initials { get; }
    public string Role { get; }

    public MemberModel(string name, string initials, string role)
    {
        Name = name;
        Initials = initials;
        Role = role;
    }

    public override string ToString() => Name;
}

public partial class MultiSelectShowcaseViewModel : ObservableObject
{
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

    public ObservableCollection<MemberModel> ComplexMembers { get; } =
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
