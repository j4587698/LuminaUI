using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TreeDataGridShowcasePage : LuminaPage
{
    public TreeDataGridShowcasePage()
    {
        InitializeComponent();
        DataContext = new TreeDataGridShowcaseViewModel();
    }
}
