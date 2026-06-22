using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class SplitButtonShowcasePage : LuminaPage
{
    public SplitButtonShowcasePage()
    {
        DataContext = new SplitButtonShowcaseViewModel();
        InitializeComponent();
    }
}
