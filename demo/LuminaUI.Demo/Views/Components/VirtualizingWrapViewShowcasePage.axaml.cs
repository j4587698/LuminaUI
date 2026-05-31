using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class VirtualizingWrapViewShowcasePage : ContentPage
{
    public VirtualizingWrapViewShowcasePage()
    {
        DataContext = new VirtualizingWrapViewShowcaseViewModel();
        InitializeComponent();
    }
}
