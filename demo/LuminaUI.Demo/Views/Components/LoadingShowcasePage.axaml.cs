using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LoadingShowcasePage : LuminaPage
{
    public LoadingShowcasePage()
    {
        DataContext = new LoadingShowcaseViewModel();
        InitializeComponent();
    }
}
