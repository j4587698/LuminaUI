using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ImageShowcasePage : ContentPage
{
    public ImageShowcasePage()
    {
        DataContext = new ImageShowcaseViewModel();
        InitializeComponent();
    }
}
