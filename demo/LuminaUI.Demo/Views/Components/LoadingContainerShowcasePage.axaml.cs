using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LoadingContainerShowcasePage : ContentPage
{
    public LoadingContainerShowcasePage()
    {
        DataContext = new LoadingContainerShowcaseViewModel();
        InitializeComponent();
    }
}
