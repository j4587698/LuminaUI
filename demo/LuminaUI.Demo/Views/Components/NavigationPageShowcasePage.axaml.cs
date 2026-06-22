using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class NavigationPageShowcasePage : LuminaPage
{
    public NavigationPageShowcasePage()
    {
        DataContext = new NavigationPageShowcaseViewModel();
        InitializeComponent();
    }
}
