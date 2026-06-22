using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ActionControlsShowcasePage : LuminaPage
{
    public ActionControlsShowcasePage()
    {
        DataContext = new EventToCommandDemoViewModel();
        InitializeComponent();
    }
}
