using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class IconButtonShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _lastAction = "No action yet";

    [RelayCommand]
    private void RunAction(string action)
    {
        LastAction = $"{action} clicked";
    }
}
