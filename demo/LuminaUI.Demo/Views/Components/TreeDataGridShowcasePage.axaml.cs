using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TreeDataGridShowcasePage : ContentPage
{
    public TreeDataGridShowcasePage()
    {
        InitializeComponent();
        DataContext = new TreeDataGridShowcaseViewModel();
    }
}
