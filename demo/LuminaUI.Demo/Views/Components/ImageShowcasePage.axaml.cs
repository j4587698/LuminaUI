using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ImageShowcasePage : LuminaPage
{
    public ImageShowcasePage()
    {
        DataContext = new ImageShowcaseViewModel();
        InitializeComponent();
    }
}
