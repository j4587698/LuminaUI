using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class EmptyShowcasePage : ContentPage
{
    public EmptyShowcasePage()
    {
        InitializeComponent();
        DataContext = new EmptyShowcaseViewModel();
    }
}
