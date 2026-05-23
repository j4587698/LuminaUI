using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class AutoCompleteBoxShowcasePage : ContentPage
{
    public AutoCompleteBoxShowcasePage()
    {
        DataContext = new AutoCompleteBoxShowcaseViewModel();
        InitializeComponent();
    }
}
