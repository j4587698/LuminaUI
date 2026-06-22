using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DescriptionsShowcasePage : LuminaPage
{
    public DescriptionsShowcasePage()
    {
        DataContext = new DescriptionsShowcaseViewModel();
        InitializeComponent();
    }
}

