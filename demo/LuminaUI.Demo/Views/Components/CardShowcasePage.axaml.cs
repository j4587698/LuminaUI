using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CardShowcasePage : ContentPage
{
    public CardShowcasePage()
    {
        DataContext = new CardShowcaseViewModel();
        InitializeComponent();
    }
}
