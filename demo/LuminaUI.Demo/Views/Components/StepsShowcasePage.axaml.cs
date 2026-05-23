using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class StepsShowcasePage : ContentPage
{
    public StepsShowcasePage()
    {
        InitializeComponent();
        DataContext = new StepsShowcaseViewModel();
    }
}
