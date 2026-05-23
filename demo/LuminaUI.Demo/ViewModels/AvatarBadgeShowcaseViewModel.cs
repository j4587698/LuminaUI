using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class AvatarBadgeShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private int _notificationCount = 12;

    [ObservableProperty]
    private int _reviewCount = 128;

    [RelayCommand]
    private void AddNotification()
    {
        NotificationCount++;
    }

    [RelayCommand]
    private void ClearNotifications()
    {
        NotificationCount = 0;
    }
}
