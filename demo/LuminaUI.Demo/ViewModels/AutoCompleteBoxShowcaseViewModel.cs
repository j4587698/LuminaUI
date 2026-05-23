using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class AutoCompleteBoxShowcaseViewModel : ObservableObject
{
    public IReadOnlyList<string> Components { get; } =
    [
        "AutoCompleteBox",
        "ButtonSpinner",
        "CommandBar",
        "DropDownButton",
        "PipsPager",
        "RefreshContainer",
        "SplitButton",
        "ToggleSplitButton"
    ];
}
