using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class LoadingShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isDisabled = true;

    [ObservableProperty]
    private string _statusText = "Loading overlay enabled";

    [RelayCommand]
    private void ToggleLoading()
    {
        IsLoading = !IsLoading;
        StatusText = IsLoading ? "Loading overlay enabled" : "Content is interactive";
    }

    [RelayCommand]
    private void ToggleDisabled()
    {
        IsDisabled = !IsDisabled;
    }
}
