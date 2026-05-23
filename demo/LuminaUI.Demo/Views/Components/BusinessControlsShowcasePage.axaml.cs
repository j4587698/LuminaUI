using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class BusinessControlsShowcasePage : ContentPage
{
    public BusinessControlsShowcasePage()
    {
        DataContext = new BusinessControlsShowcaseViewModel();
        InitializeComponent();
    }
}
