using Avalonia.Controls;

namespace LuminaUI.Demo.Views;

public partial class RangeSliderShowcasePage : ContentPage
{
    public RangeSliderShowcasePage()
    {
        InitializeComponent();
        DataContext = new ViewModels.RangeSliderShowcaseViewModel();
    }
}
