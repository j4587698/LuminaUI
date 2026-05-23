using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class EventToCommandDemoViewModel : ObservableObject
{
    private int _eventCount;

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

    [ObservableProperty]
    private string _lastEvent = "No event yet.";

    [RelayCommand]
    private void Record(object? parameter)
    {
        _eventCount++;
        LastEvent = parameter switch
        {
            null => $"Event #{_eventCount}",
            string text => $"Event #{_eventCount}: {text}",
            _ => $"Event #{_eventCount}: {parameter.GetType().Name}"
        };
    }
}
