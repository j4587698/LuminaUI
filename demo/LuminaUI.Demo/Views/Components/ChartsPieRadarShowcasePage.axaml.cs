using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ChartsPieRadarShowcasePage : LuminaPage
{
    public ChartsPieRadarShowcasePage()
    {
        DataContext = new ChartsPieRadarShowcaseViewModel();
        InitializeComponent();
    }
}