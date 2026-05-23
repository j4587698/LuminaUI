using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LinkedCategoryListShowcasePage : ContentPage
{
    public LinkedCategoryListShowcasePage()
    {
        DataContext = new LinkedCategoryListShowcaseViewModel();
        InitializeComponent();
    }
}
