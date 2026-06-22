using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class RangeShowcasePage : LuminaPage
{
    public RangeShowcasePage()
    {
        DataContext = new RangeShowcaseViewModel();
        InitializeComponent();
    }
}
