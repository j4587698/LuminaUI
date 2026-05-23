using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class PropertiesShowcasePage : ContentPage
{
    public PropertiesShowcasePage()
    {
        InitializeComponent();
        DataContext = new PropertiesShowcaseViewModel();
    }
}
