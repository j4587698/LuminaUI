using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class EmptyShowcasePage : LuminaPage
{
    public EmptyShowcasePage()
    {
        InitializeComponent();
        DataContext = new EmptyShowcaseViewModel();
    }
}
