using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DrawerPageShowcasePage : LuminaPage
{
    public DrawerPageShowcasePage()
    {
        DataContext = new DrawerPageShowcaseViewModel();
        InitializeComponent();
    }
}
