using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DateRangePickerShowcasePage : ContentPage
{
    public DateRangePickerShowcasePage()
    {
        DataContext = new DateRangePickerShowcaseViewModel();
        InitializeComponent();
    }
}
