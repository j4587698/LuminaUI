using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CascaderShowcasePage : ContentPage
{
    public CascaderShowcasePage()
    {
        InitializeComponent();
        DataContext = new CascaderShowcaseViewModel();
    }
}
