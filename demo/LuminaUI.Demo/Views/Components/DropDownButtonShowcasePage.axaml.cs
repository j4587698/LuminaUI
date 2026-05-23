using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DropDownButtonShowcasePage : ContentPage
{
    public DropDownButtonShowcasePage()
    {
        DataContext = new DropDownButtonShowcaseViewModel();
        InitializeComponent();
    }
}
