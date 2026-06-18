using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class NavigationContainersShowcaseViewModel : ObservableObject
{
    private readonly SandboxNotificationCenter _notificationCenter;

    [ObservableProperty]
    private bool _isCarouselAutoPlayEnabled = true;

    [ObservableProperty]
    private double _carouselAutoPlayIntervalSeconds = 2.5;

    [ObservableProperty]
    private string _carouselAutoPlayIntervalText = FormatSeconds(2.5);

    [ObservableProperty]
    private NotificationPosition _notificationPosition = NotificationPosition.TopRight;

    [ObservableProperty]
    private string _notificationPositionText = FormatNotificationPosition(NotificationPosition.TopRight);

    [ObservableProperty]
    private double _notificationDurationSeconds = 3;

    [ObservableProperty]
    private string _notificationDurationText = FormatSeconds(3);

    [ObservableProperty]
    private DrawerPlacement _drawerPlacement = DrawerPlacement.Left;

    [ObservableProperty]
    private bool _isDrawerOpen = true;

    public TimeSpan CarouselAutoPlayInterval => TimeSpan.FromSeconds(Math.Clamp(CarouselAutoPlayIntervalSeconds, 0.5, 30));

    public NavigationContainersShowcaseViewModel()
        : this(new SandboxNotificationCenter())
    {
    }

    public NavigationContainersShowcaseViewModel(SandboxNotificationCenter notificationCenter)
    {
        _notificationCenter = notificationCenter;
        _notificationCenter.SetPosition(NotificationPosition);
    }

    [RelayCommand]
    private async Task PushNavigationAsync(NavigationPage? navigationPage)
    {
        if (navigationPage == null)
        {
            return;
        }

        await navigationPage.PushAsync(CreateNavigationDetailPage(navigationPage));
    }

    [RelayCommand]
    private async Task PopNavigationAsync(NavigationPage? navigationPage)
    {
        if (navigationPage != null)
        {
            await navigationPage.PopAsync();
        }
    }

    [RelayCommand]
    private void SetNotificationPosition(NotificationPosition position)
    {
        NotificationPosition = position;
        _notificationCenter.SetPosition(position);
    }

    [RelayCommand]
    private void SetDrawerPlacement(DrawerPlacement placement)
    {
        DrawerPlacement = placement;
        IsDrawerOpen = true;
    }

    [RelayCommand]
    private void CloseDrawer()
    {
        IsDrawerOpen = false;
    }

    [RelayCommand]
    private void ShowInfoNotification()
    {
        ShowNotification("Sandbox.Text.0641", "Sandbox.Text.0642", NotificationType.Information);
    }

    [RelayCommand]
    private void ShowSuccessNotification()
    {
        ShowNotification("Sandbox.Text.0615", "Sandbox.Text.0643", NotificationType.Success);
    }

    [RelayCommand]
    private void ShowWarningNotification()
    {
        ShowNotification("Sandbox.Text.0644", "Sandbox.Text.0645", NotificationType.Warning);
    }

    private ContentPage CreateNavigationDetailPage(NavigationPage navigationPage)
    {
        var popButton = new Button
        {
            Classes = { "Outline", "Small" },
            Content = T("Sandbox.Text.0637"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Command = PopNavigationCommand,
            CommandParameter = navigationPage
        };

        var detailPage = new ContentPage
        {
            Header = T("Sandbox.Text.0638"),
            Content = new StackPanel
            {
                Margin = new Thickness(16, 68, 16, 16),
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Classes = { "Label" },
                        Text = T("Sandbox.Text.0639")
                    },
                    new TextBlock
                    {
                        Classes = { "Helper" },
                        Text = T("Sandbox.Text.0640"),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    popButton
                }
            }
        };
        return detailPage;
    }

    private void ShowNotification(string titleKey, string messageKey, NotificationType type)
    {
        var duration = TimeSpan.FromSeconds(Math.Clamp(NotificationDurationSeconds, 0.5, 30));
        _notificationCenter.Show(
            T(titleKey),
            T(messageKey),
            type,
            duration);
    }

    partial void OnCarouselAutoPlayIntervalSecondsChanged(double value)
    {
        CarouselAutoPlayIntervalText = FormatSeconds(value);
        OnPropertyChanged(nameof(CarouselAutoPlayInterval));
    }

    partial void OnNotificationDurationSecondsChanged(double value)
    {
        NotificationDurationText = FormatSeconds(value);
    }

    partial void OnNotificationPositionChanged(NotificationPosition value)
    {
        NotificationPositionText = FormatNotificationPosition(value);
    }

    private static string FormatSeconds(double value)
    {
        return $"{Math.Clamp(value, 0.5, 30):0.#}s";
    }

    private static string FormatNotificationPosition(NotificationPosition position)
    {
        var key = position switch
        {
            NotificationPosition.TopLeft => "Sandbox.Text.0378",
            NotificationPosition.BottomLeft => "Sandbox.Text.0380",
            NotificationPosition.BottomRight => "Sandbox.Text.0381",
            _ => "Sandbox.Text.0379"
        };
        return T(key);
    }

    private static string T(string key)
    {
        return LuminaLocalization.Get(key);
    }
}
