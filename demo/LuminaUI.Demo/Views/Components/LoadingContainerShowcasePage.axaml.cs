using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LoadingContainerShowcasePage : LuminaPage
{
    public LoadingContainerShowcasePage()
    {
        DataContext = new LoadingContainerShowcaseViewModel();
        InitializeComponent();
    }
}
