using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TagInputShowcasePage : ContentPage
{
    public TagInputShowcasePage()
    {
        DataContext = new TagInputShowcaseViewModel();
        InitializeComponent();
    }
}
