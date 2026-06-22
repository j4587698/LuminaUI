using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class FoundationShowcasePage : LuminaPage
{
    public FoundationShowcasePage()
    {
        DataContext = new FoundationShowcaseViewModel();
        InitializeComponent();
    }
}
