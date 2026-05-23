using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class PropertiesShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _controlName = "PrimaryActionButton";

    [ObservableProperty]
    private string _controlType = "Button";

    [ObservableProperty]
    private string _layoutMode = "Stretch";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _useCompiledBinding = true;

    [ObservableProperty]
    private string _description = "A two-column editor row can host any Avalonia control in the value cell.";

    public ObservableCollection<string> LayoutModes { get; } =
    [
        "Auto",
        "Stretch",
        "Center",
        "Right"
    ];

    public ObservableCollection<PropertyDetailViewModel> Details { get; } =
    [
        new("Assembly", "LuminaUI.Demo"),
        new("Namespace", "LuminaUI.Demo.Views.Components"),
        new("AOT", "Uses compiled bindings and avoids reflection-only row generation."),
        new("Notes", "Long string values are wrapped by the LuminaProperties string template without changing global Label behavior.")
    ];
}

public sealed record PropertyDetailViewModel(string Name, string Value);
