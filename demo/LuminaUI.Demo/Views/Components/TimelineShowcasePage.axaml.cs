using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TimelineShowcasePage : ContentPage
{
    public TimelineShowcasePage()
    {
        DataContext = new TimelineShowcaseViewModel();
        InitializeComponent();
    }
}
