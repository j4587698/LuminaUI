using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class OverlaysShowcasePage : ContentPage
{
    public OverlaysShowcasePage()
    {
        DataContext = new OverlaysShowcaseViewModel(this);
        InitializeComponent();
    }
}
