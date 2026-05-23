using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CommandBarShowcasePage : ContentPage
{
    public CommandBarShowcasePage()
    {
        DataContext = new CommandBarShowcaseViewModel();
        InitializeComponent();
    }
}
