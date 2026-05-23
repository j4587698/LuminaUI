using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ButtonShowcasePage : ContentPage
{
    public ButtonShowcasePage()
    {
        DataContext = new ButtonShowcaseViewModel();
        InitializeComponent();
    }
}
