using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TabStripShowcasePage : LuminaPage
{
    public TabStripShowcasePage()
    {
        DataContext = new StaticShowcaseViewModel();
        InitializeComponent();
    }
}
