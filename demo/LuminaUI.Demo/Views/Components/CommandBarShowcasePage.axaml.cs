using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CommandBarShowcasePage : LuminaPage
{
    public CommandBarShowcasePage()
    {
        DataContext = new CommandBarShowcaseViewModel();
        InitializeComponent();
    }
}
