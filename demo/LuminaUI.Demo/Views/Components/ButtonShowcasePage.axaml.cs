using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ButtonShowcasePage : LuminaPage
{
    public ButtonShowcasePage()
    {
        DataContext = new ButtonShowcaseViewModel();
        InitializeComponent();
    }
}
