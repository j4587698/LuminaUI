using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TimelineShowcasePage : LuminaPage
{
    public TimelineShowcasePage()
    {
        DataContext = new TimelineShowcaseViewModel();
        InitializeComponent();
    }
}
