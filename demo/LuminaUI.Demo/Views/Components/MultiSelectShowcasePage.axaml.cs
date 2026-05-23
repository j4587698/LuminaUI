using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class MultiSelectShowcasePage : ContentPage
{
    public MultiSelectShowcasePage()
    {
        DataContext = new MultiSelectShowcaseViewModel();
        InitializeComponent();
    }
}
