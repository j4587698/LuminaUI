using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DrawerPageShowcasePage : ContentPage
{
    public DrawerPageShowcasePage()
    {
        DataContext = new DrawerPageShowcaseViewModel();
        InitializeComponent();
    }
}
