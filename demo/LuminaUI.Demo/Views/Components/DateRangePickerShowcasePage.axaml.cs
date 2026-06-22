using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DateRangePickerShowcasePage : LuminaPage
{
    public DateRangePickerShowcasePage()
    {
        DataContext = new DateRangePickerShowcaseViewModel();
        InitializeComponent();
    }
}
