using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class StepsShowcasePage : LuminaPage
{
    public StepsShowcasePage()
    {
        InitializeComponent();
        DataContext = new StepsShowcaseViewModel();
    }
}
