using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LinkedCategoryListShowcasePage : LuminaPage
{
    public LinkedCategoryListShowcasePage()
    {
        DataContext = new LinkedCategoryListShowcaseViewModel();
        InitializeComponent();
    }
}
