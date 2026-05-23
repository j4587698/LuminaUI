using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class PopConfirmShowcasePage : ContentPage
{
    public PopConfirmShowcasePage()
    {
        DataContext = new PopConfirmShowcaseViewModel();
        InitializeComponent();
    }
}

