using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TransitioningContentControlShowcasePage : LuminaPage
{
    public TransitioningContentControlShowcasePage()
    {
        DataContext = new TransitioningContentControlShowcaseViewModel();
        InitializeComponent();
    }
}
