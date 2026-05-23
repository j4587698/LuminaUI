using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DescriptionsShowcasePage : ContentPage
{
    public DescriptionsShowcasePage()
    {
        DataContext = new DescriptionsShowcaseViewModel();
        InitializeComponent();
    }
}

