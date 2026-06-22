using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class SmartComponentsShowcasePage : LuminaPage
{
    public SmartComponentsShowcasePage()
    {
        DataContext = new StaticShowcaseViewModel();
        InitializeComponent();
    }
}
