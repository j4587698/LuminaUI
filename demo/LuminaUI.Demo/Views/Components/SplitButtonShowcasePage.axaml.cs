using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class SplitButtonShowcasePage : ContentPage
{
    public SplitButtonShowcasePage()
    {
        DataContext = new SplitButtonShowcaseViewModel();
        InitializeComponent();
    }
}
