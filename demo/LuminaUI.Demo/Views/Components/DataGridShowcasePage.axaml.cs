using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DataGridShowcasePage : ContentPage
{
    public DataGridShowcasePage()
    {
        InitializeComponent();
        DataContext = new DataGridShowcaseViewModel();
    }
}
