using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CascaderShowcasePage : LuminaPage
{
    public CascaderShowcasePage()
    {
        InitializeComponent();
        DataContext = new CascaderShowcaseViewModel();
    }
}
