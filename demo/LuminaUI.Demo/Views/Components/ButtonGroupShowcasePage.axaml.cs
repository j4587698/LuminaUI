using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ButtonGroupShowcasePage : ContentPage
{
    public ButtonGroupShowcasePage()
    {
        DataContext = new ButtonGroupShowcaseViewModel();
        InitializeComponent();
    }
}
