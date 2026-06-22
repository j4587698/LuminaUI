using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class RatingShowcasePage : LuminaPage
{
    public RatingShowcasePage()
    {
        DataContext = new RatingShowcaseViewModel();
        InitializeComponent();
    }
}

