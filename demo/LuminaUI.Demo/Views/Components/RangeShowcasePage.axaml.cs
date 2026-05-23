using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class RangeShowcasePage : ContentPage
{
    public RangeShowcasePage()
    {
        DataContext = new RangeShowcaseViewModel();
        InitializeComponent();
    }
}
