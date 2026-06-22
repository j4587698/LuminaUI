using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class NavigationContainersShowcasePage : LuminaPage
{
    public NavigationContainersShowcasePage()
        : this(new SandboxNotificationCenter())
    {
    }

    public NavigationContainersShowcasePage(SandboxNotificationCenter notificationCenter)
    {
        DataContext = new NavigationContainersShowcaseViewModel(notificationCenter);
        InitializeComponent();
    }
}
