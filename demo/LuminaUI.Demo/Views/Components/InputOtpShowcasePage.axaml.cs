using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class InputOtpShowcasePage : ContentPage
{
    public InputOtpShowcasePage()
    {
        DataContext = new InputOtpShowcaseViewModel();
        InitializeComponent();
    }
}
