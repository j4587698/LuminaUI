using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DataGridShowcasePage : LuminaPage
{
    public DataGridShowcasePage()
    {
        InitializeComponent();
        DataContext = new DataGridShowcaseViewModel();
    }
}
