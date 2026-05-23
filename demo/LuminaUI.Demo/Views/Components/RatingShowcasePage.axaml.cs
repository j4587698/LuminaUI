using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class RatingShowcasePage : ContentPage
{
    public RatingShowcasePage()
    {
        DataContext = new RatingShowcaseViewModel();
        InitializeComponent();
    }
}

