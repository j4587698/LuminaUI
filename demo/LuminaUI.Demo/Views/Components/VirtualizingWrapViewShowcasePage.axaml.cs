using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class VirtualizingWrapViewShowcasePage : LuminaPage
{
    public VirtualizingWrapViewShowcasePage()
    {
        DataContext = new VirtualizingWrapViewShowcaseViewModel();
        InitializeComponent();
    }
}
