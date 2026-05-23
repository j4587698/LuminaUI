using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LoadingShowcasePage : ContentPage
{
    public LoadingShowcasePage()
    {
        DataContext = new LoadingShowcaseViewModel();
        InitializeComponent();
    }
}
