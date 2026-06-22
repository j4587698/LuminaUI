using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class WindowDialogShowcasePage : LuminaPage
{
    public WindowDialogShowcasePage()
    {
        DataContext = new WindowDialogShowcaseViewModel(this);
        InitializeComponent();
    }
}
