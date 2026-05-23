using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DetailContentPage : ContentPage
{
    public DetailContentPage()
    {
        DataContext = new DetailContentPageViewModel();
        InitializeComponent();
    }
}
