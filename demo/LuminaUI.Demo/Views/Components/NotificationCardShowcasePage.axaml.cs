using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class NotificationCardShowcasePage : LuminaPage
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
