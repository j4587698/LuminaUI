using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ChartsComboShowcasePage : LuminaPage
{
    public ChartsComboShowcasePage()
    {
        DataContext = new ChartsComboShowcaseViewModel();
        InitializeComponent();
    }
}