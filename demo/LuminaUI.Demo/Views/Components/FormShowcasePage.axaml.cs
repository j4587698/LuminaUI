using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class FormShowcasePage : LuminaPage
{
    public FormShowcasePage()
    {
        DataContext = new FormShowcaseViewModel();
        InitializeComponent();
    }
}
