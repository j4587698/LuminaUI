using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class CardShowcasePage : LuminaPage
{
    public CardShowcasePage()
    {
        DataContext = new CardShowcaseViewModel();
        InitializeComponent();
    }
}
