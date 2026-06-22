using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class BusinessControlsShowcasePage : LuminaPage
{
    public BusinessControlsShowcasePage()
    {
        DataContext = new BusinessControlsShowcaseViewModel();
        InitializeComponent();
    }
}
