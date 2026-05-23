using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class PopConfirmShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _status = "Waiting for confirmation";

    [RelayCommand]
    private void Confirm(string? action)
    {
        Status = action is null ? "Confirmed" : $"Confirmed: {action}";
    }

    [RelayCommand]
    private void Cancel(string? action)
    {
        Status = action is null ? "Cancelled" : $"Cancelled: {action}";
    }
}

