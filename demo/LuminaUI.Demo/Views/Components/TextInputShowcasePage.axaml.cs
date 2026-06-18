using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TextInputShowcasePage : ContentPage
{
    public TextInputShowcasePage()
    {
        DataContext = new TextInputShowcaseViewModel();
        InitializeComponent();
    }
}
