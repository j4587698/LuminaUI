using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class SandboxNotificationCenter : ObservableObject
{
    private static readonly IBrush InformationBrush = new SolidColorBrush(Color.Parse("#1677FF"));
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#22C55E"));
    private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#F59E0B"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#EF4444"));

    [ObservableProperty]
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Right;

    [ObservableProperty]
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;

    public ObservableCollection<SandboxNotificationItem> Notifications { get; } = [];

    public void SetPosition(NotificationPosition position)
    {
        HorizontalAlignment = position is NotificationPosition.TopLeft or NotificationPosition.BottomLeft
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Right;

        VerticalAlignment = position is NotificationPosition.BottomLeft or NotificationPosition.BottomRight
            ? VerticalAlignment.Bottom
            : VerticalAlignment.Top;
    }

    public void Show(string title, string message, NotificationType type, TimeSpan duration)
    {
        var normalizedDuration = NormalizeDuration(duration);
        var durationMilliseconds = (int)normalizedDuration.TotalMilliseconds;
        var item = new SandboxNotificationItem(
            title,
            message,
            type,
            GetNotificationBrush(type),
            durationMilliseconds,
            Close);

        Notifications.Add(item);
        item.StartAutoClose();
    }

    private void Close(SandboxNotificationItem? item)
    {
        if (item != null)
        {
            item.StopAutoClose();
            Notifications.Remove(item);
        }
    }

    private static TimeSpan NormalizeDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMilliseconds(500))
        {
            return TimeSpan.FromMilliseconds(500);
        }

        return duration > TimeSpan.FromSeconds(30)
            ? TimeSpan.FromSeconds(30)
            : duration;
    }

    private static IBrush GetNotificationBrush(NotificationType type)
    {
        return type switch
        {
            NotificationType.Success => SuccessBrush,
            NotificationType.Warning => WarningBrush,
            NotificationType.Error => ErrorBrush,
            _ => InformationBrush
        };
    }
}

public sealed class SandboxNotificationItem
{
    private readonly Action<SandboxNotificationItem> _close;
    private readonly CancellationTokenSource _autoCloseCancellation = new();
    private readonly int _durationMilliseconds;

    public SandboxNotificationItem(
        string title,
        string message,
        NotificationType type,
        IBrush accentBrush,
        int durationMilliseconds,
        Action<SandboxNotificationItem> close)
    {
        Title = title;
        Message = message;
        Type = type;
        AccentBrush = accentBrush;
        _close = close;
        _durationMilliseconds = Math.Clamp(durationMilliseconds, 500, 30000);
        CloseCommand = new RelayCommand(() => close(this));
    }

    public string Title { get; }
    public string Message { get; }
    public NotificationType Type { get; }
    public IBrush AccentBrush { get; }
    public ICommand CloseCommand { get; }

    public void StartAutoClose()
    {
        _ = AutoCloseAsync();
    }

    public void StopAutoClose()
    {
        _autoCloseCancellation.Cancel();
    }

    private async Task AutoCloseAsync()
    {
        try
        {
            await Task.Delay(_durationMilliseconds, _autoCloseCancellation.Token).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => _close(this));
        }
        catch (OperationCanceledException)
        {
        }
    }
}
