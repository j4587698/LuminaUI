using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class BreadcrumbShowcasePage : ContentPage
{
    public BreadcrumbShowcasePage()
    {
        DataContext = new BreadcrumbShowcaseViewModel();
        InitializeComponent();
    }
}

