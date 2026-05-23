using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ActionControlsShowcasePage : ContentPage
{
    public ActionControlsShowcasePage()
    {
        DataContext = new EventToCommandDemoViewModel();
        InitializeComponent();
    }
}
