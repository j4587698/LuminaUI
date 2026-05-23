using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class NotificationCardShowcasePage : ContentPage
{
    public NotificationCardShowcasePage()
        : this(new SandboxNotificationCenter())
    {
    }

    public NotificationCardShowcasePage(SandboxNotificationCenter notificationCenter)
    {
        DataContext = new NotificationCardShowcaseViewModel(notificationCenter);
        InitializeComponent();
    }
}
