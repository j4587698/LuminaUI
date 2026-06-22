using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ColorPickerShowcasePage : LuminaPage
{
    public ColorPickerShowcasePage()
    {
        InitializeComponent();
        DataContext = new ColorPickerShowcaseViewModel();
    }
}
