using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CollectionShowcasePage : LuminaPage
{
    public CollectionShowcasePage()
    {
        DataContext = new StaticShowcaseViewModel();
        InitializeComponent();
    }
}
