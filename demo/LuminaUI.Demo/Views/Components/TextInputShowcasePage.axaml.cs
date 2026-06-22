using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TextInputShowcasePage : LuminaPage
{
    public TextInputShowcasePage()
    {
        DataContext = new TextInputShowcaseViewModel();
        InitializeComponent();
    }
}
