using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class NavigationPageShowcasePage : ContentPage
{
    public NavigationPageShowcasePage()
    {
        DataContext = new NavigationPageShowcaseViewModel();
        InitializeComponent();
    }
}
