using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class MotionShowcasePage : LuminaPage
{
    public MotionShowcasePage()
    {
        DataContext = new MotionShowcaseViewModel();
        InitializeComponent();
    }
}
