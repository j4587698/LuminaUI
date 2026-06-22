using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class InputOtpShowcasePage : LuminaPage
{
    public InputOtpShowcasePage()
    {
        DataContext = new InputOtpShowcaseViewModel();
        InitializeComponent();
    }
}
