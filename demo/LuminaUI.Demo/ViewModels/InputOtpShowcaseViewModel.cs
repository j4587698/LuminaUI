using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class InputOtpShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _verificationCode = string.Empty;

    [ObservableProperty]
    private string _paymentCode = "24";

    [ObservableProperty]
    private string _statusText = "Waiting for input";

    [RelayCommand]
    private void OtpCompleted(string code)
    {
        StatusText = $"Completed: {code}";
    }

    [RelayCommand]
    private void Clear()
    {
        VerificationCode = string.Empty;
        PaymentCode = string.Empty;
        StatusText = "Waiting for input";
    }
}
