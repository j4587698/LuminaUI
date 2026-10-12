using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ChartsScatterShowcasePage : LuminaPage
{
    public ChartsScatterShowcasePage()
    {
        DataContext = new ChartsScatterShowcaseViewModel();
        InitializeComponent();
    }
}