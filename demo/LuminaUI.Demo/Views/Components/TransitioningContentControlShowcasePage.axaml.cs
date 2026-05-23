using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class TransitioningContentControlShowcasePage : ContentPage
{
    public TransitioningContentControlShowcasePage()
    {
        DataContext = new TransitioningContentControlShowcaseViewModel();
        InitializeComponent();
    }
}
