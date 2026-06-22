using Avalonia.Controls;
using LuminaUI.Controls;

namespace LuminaUI.Demo.Views;

public partial class RangeSliderShowcasePage : LuminaPage
{
    public RangeSliderShowcasePage()
    {
        InitializeComponent();
        DataContext = new ViewModels.RangeSliderShowcaseViewModel();
    }
}
