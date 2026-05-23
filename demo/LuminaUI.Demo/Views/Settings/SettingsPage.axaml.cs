using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        DataContext = new SettingsPageViewModel();
        InitializeComponent();
    }
}
