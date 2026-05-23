using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class FoundationShowcasePage : ContentPage
{
    public FoundationShowcasePage()
    {
        DataContext = new FoundationShowcaseViewModel();
        InitializeComponent();
    }
}
