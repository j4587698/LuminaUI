using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class AutoScrollTextShowcasePage : LuminaPage
{
    public AutoScrollTextShowcasePage()
    {
        DataContext = new StaticShowcaseViewModel();
        InitializeComponent();
    }
}
