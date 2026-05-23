using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DesignSystemPage : LuminaPage
{
    public DesignSystemPage()
    {
        DataContext = new DesignSystemPageViewModel();
        InitializeComponent();
    }
}
