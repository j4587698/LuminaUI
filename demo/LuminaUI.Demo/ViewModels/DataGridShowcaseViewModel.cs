using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class DataGridShowcaseViewModel : ObservableObject
{
    private int _nextIndex = 7;

    [ObservableProperty]
    private DataGridOrderViewModel? _selectedOrder;

    [ObservableProperty]
    private bool _canResizeColumns = true;

    [ObservableProperty]
    private bool _canReorderColumns = true;

    public ObservableCollection<DataGridOrderViewModel> Orders { get; } =
    [
        new("LUM-1201", "Design tokens", "Ready", "Mei Chen", 86, true),
        new("LUM-1202", "Desktop shell", "In review", "Noah Smith", 64, true),
        new("LUM-1203", "Mobile settings", "Blocked", "Ava Li", 42, false),
        new("LUM-1204", "Picker sheets", "Ready", "Yuki Tan", 91, true),
        new("LUM-1205", "Overlay layer", "Draft", "Iris Wang", 35, false),
        new("LUM-1206", "Table modules", "In review", "Kai Zhou", 58, true)
    ];

    public ObservableCollection<string> StatusOptions { get; } =
    [
        "Draft",
        "In review",
        "Ready",
        "Blocked"
    ];

    [RelayCommand]
    private void AddOrder()
    {
        Orders.Add(new DataGridOrderViewModel(
            $"LUM-12{_nextIndex:00}",
            "New component",
            "Draft",
            "Unassigned",
            12,
            false));
        _nextIndex++;
    }
}

public partial class DataGridOrderViewModel(
    string id,
    string project,
    string status,
    string owner,
    int progress,
    bool enabled) : ObservableObject
{
    [ObservableProperty]
    private string _id = id;

    [ObservableProperty]
    private string _project = project;

    [ObservableProperty]
    private string _status = status;

    [ObservableProperty]
    private string _owner = owner;

    [ObservableProperty]
    private int _progress = progress;

    [ObservableProperty]
    private bool _enabled = enabled;
}
