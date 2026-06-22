using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ButtonGroupShowcasePage : LuminaPage
{
    public ButtonGroupShowcasePage()
    {
        DataContext = new ButtonGroupShowcaseViewModel();
        InitializeComponent();
    }
}
