using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class AvatarBadgeShowcasePage : ContentPage
{
    public AvatarBadgeShowcasePage()
    {
        DataContext = new AvatarBadgeShowcaseViewModel();
        InitializeComponent();
    }
}
