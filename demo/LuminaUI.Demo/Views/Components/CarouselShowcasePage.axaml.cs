using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CarouselShowcasePage : ContentPage
{
    public CarouselShowcasePage()
    {
        DataContext = new CarouselShowcaseViewModel();
        InitializeComponent();
    }
}
