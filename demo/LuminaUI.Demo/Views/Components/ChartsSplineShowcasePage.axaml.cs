using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ChartsSplineShowcasePage : LuminaPage
{
    public ChartsSplineShowcasePage()
    {
        DataContext = new ChartsSplineShowcaseViewModel();
        InitializeComponent();
    }
}