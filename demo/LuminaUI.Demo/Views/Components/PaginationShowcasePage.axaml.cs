using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class PaginationShowcasePage : ContentPage
{
    public PaginationShowcasePage()
    {
        DataContext = new PaginationShowcaseViewModel();
        InitializeComponent();
    }
}
