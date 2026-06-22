using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class AutoCompleteBoxShowcasePage : LuminaPage
{
    public AutoCompleteBoxShowcasePage()
    {
        DataContext = new AutoCompleteBoxShowcaseViewModel();
        InitializeComponent();
    }
}
