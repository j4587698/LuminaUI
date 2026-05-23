using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class ButtonShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    private bool _isMenuOpen;

    [ObservableProperty]
    private string _buttonStatus = "Ready";

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SubmitAsync()
    {
        IsSubmitting = true;
        ButtonStatus = "Submitting...";

        await Task.Delay(1400);

        ButtonStatus = "Completed";
        IsSubmitting = false;
    }
}
