using System;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class NotificationCardShowcaseViewModel : ObservableObject
{
    private readonly SandboxNotificationCenter _notificationCenter;

    [ObservableProperty]
    private NotificationPosition _notificationPosition = NotificationPosition.TopRight;

    [ObservableProperty]
    private string _notificationPositionText = T("Sandbox.Text.0379");

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
        ShowNotification(T("Sandbox.Text.0641"), T("Sandbox.Text.0642"), NotificationType.Information);
    }

    [RelayCommand]
    private void ShowSuccessNotification()
    {
        ShowNotification(T("Sandbox.Text.0615"), T("Sandbox.Text.0643"), NotificationType.Success);
    }

    [RelayCommand]
    private void ShowWarningNotification()
    {
        ShowNotification(T("Sandbox.Text.0644"), T("Sandbox.Text.0645"), NotificationType.Warning);
    }

    [RelayCommand]
    private void ShowErrorNotification()
    {
        ShowNotification(T("Sandbox.Text.0810"), T(SandboxLocalization.NotificationsSyncFailed), NotificationType.Error);
    }

    [RelayCommand]
    private void ShowComplexNotification()
    {
        ShowNotification(
            T(SandboxLocalization.NotificationsSystemUpdateTitle),
            T(SandboxLocalization.NotificationsSystemUpdateMessage),
            NotificationType.Information);
    }

    private void ShowNotification(string title, string message, NotificationType type)
    {
        var duration = TimeSpan.FromSeconds(Math.Clamp(NotificationDurationSeconds, 0.5, 30));
        _notificationCenter.Show(
            title,
            message,
            type,
            duration);
    }

    partial void OnNotificationDurationSecondsChanged(double value)
    {
        NotificationDurationText = $"{Math.Clamp(value, 0.5, 30):0.#}s";
    }

    partial void OnNotificationPositionChanged(NotificationPosition value)
    {
        var key = value switch
        {
            NotificationPosition.TopLeft => "Sandbox.Text.0378",
            NotificationPosition.BottomLeft => "Sandbox.Text.0380",
            NotificationPosition.BottomRight => "Sandbox.Text.0381",
            _ => "Sandbox.Text.0379"
        };
        NotificationPositionText = T(key);
    }

    private static string T(string key)
    {
        return LuminaLocalization.Get(key);
    }
}
