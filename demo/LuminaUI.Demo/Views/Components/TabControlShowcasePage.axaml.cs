using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TabControlShowcasePage : LuminaPage
{
    public TabControlShowcasePage()
    {
        DataContext = new StaticShowcaseViewModel();
        InitializeComponent();
    }
}
