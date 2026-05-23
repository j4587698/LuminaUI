using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class MotionShowcasePage : ContentPage
{
    public MotionShowcasePage()
    {
        DataContext = new MotionShowcaseViewModel();
        InitializeComponent();
    }
}
