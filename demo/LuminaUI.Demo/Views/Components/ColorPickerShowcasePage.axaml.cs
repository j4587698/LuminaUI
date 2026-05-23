using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ColorPickerShowcasePage : ContentPage
{
    public ColorPickerShowcasePage()
    {
        InitializeComponent();
        DataContext = new ColorPickerShowcaseViewModel();
    }
}
