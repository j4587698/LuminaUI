using Avalonia.Controls;

namespace LuminaUI.Demo.Views;

public partial class IconButtonShowcasePage : ContentPage
{
    public IconButtonShowcasePage()
    {
        InitializeComponent();
        DataContext = new ViewModels.IconButtonShowcaseViewModel();
    }
}
