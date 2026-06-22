using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class BreadcrumbShowcasePage : LuminaPage
{
    public BreadcrumbShowcasePage()
    {
        DataContext = new BreadcrumbShowcaseViewModel();
        InitializeComponent();
    }
}

