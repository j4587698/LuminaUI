using Avalonia.Controls;
using LuminaUI.Controls;

namespace LuminaUI.Demo.Views;

public partial class IconButtonShowcasePage : LuminaPage
{
    public IconButtonShowcasePage()
    {
        InitializeComponent();
        DataContext = new ViewModels.IconButtonShowcaseViewModel();
    }
}
