using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class WindowDialogShowcasePage : ContentPage
{
    public WindowDialogShowcasePage()
    {
        DataContext = new WindowDialogShowcaseViewModel(this);
        InitializeComponent();
    }
}
