using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class FormShowcasePage : ContentPage
{
    public FormShowcasePage()
    {
        DataContext = new FormShowcaseViewModel();
        InitializeComponent();
    }
}
