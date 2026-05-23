using System;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class NotificationCardShowcaseViewModel : ObservableObject
{
    private readonly SandboxNotificationCenter _notificationCenter;

    [ObservableProperty]
    private NotificationPosition _notificationPosition = NotificationPosition.TopRight;

    [ObservableProperty]
    private string _notificationPositionText = SandboxTextLocalizer.Localize("Top right");

    [ObservableProperty]
    private double _notificationDurationSeconds = 3;

    [ObservableProperty]
    private string _notificationDurationText = "3s";

    public NotificationCardShowcaseViewModel() : this(new SandboxNotificationCenter()) { }

    public NotificationCardShowcaseViewModel(SandboxNotificationCenter notificationCenter)
    {
        _notificationCenter = notificationCenter;
        _notificationCenter.SetPosition(NotificationPosition);
    }

    [RelayCommand]
    private void SetNotificationPosition(NotificationPosition position)
    {
        NotificationPosition = position;
        _notificationCenter.SetPosition(position);
    }

    [RelayCommand]
    private void ShowInfoNotification()
    {
        ShowNotification("System message", "Background sync completed.", NotificationType.Information);
    }

    [RelayCommand]
    private void ShowSuccessNotification()
    {
        ShowNotification("Saved", "The workspace settings were saved.", NotificationType.Success);
    }

    [RelayCommand]
    private void ShowWarningNotification()
    {
        ShowNotification("Attention", "The next sync needs confirmation.", NotificationType.Warning);
    }

    [RelayCommand]
    private void ShowErrorNotification()
    {
        ShowNotification("Error", "Failed to sync the workspace.", NotificationType.Error);
    }

    [RelayCommand]
    private void ShowComplexNotification()
    {
        ShowNotification("System Update Available", "A new version of the workspace is available for download. This update includes several bug fixes and performance improvements.", NotificationType.Information);
    }

    private void ShowNotification(string title, string message, NotificationType type)
    {
        var duration = TimeSpan.FromSeconds(Math.Clamp(NotificationDurationSeconds, 0.5, 30));
        _notificationCenter.Show(
            SandboxTextLocalizer.Localize(title),
            SandboxTextLocalizer.Localize(message),
            type,
            duration);
    }

    partial void OnNotificationDurationSecondsChanged(double value)
    {
        NotificationDurationText = $"{Math.Clamp(value, 0.5, 30):0.#}s";
    }

    partial void OnNotificationPositionChanged(NotificationPosition value)
    {
        var text = value switch
        {
            NotificationPosition.TopLeft => "Top left",
            NotificationPosition.BottomLeft => "Bottom left",
            NotificationPosition.BottomRight => "Bottom right",
            _ => "Top right"
        };
        NotificationPositionText = SandboxTextLocalizer.Localize(text);
    }
}
