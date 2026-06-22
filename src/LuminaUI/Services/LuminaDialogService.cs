using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Controls;
using LuminaUI.Localization;

namespace LuminaUI.Services;

public class LuminaDialogService : ILuminaDialogService
{
    public static ILuminaDialogService Instance { get; } = new LuminaDialogService();

    public void ShowDialog(object content, string? title = null)
    {
        ShowDialogCore(LuminaOverlayHostResolver.FindDefault(), content, title);
    }

    public void ShowDialog(LuminaShell shell, object content, string? title = null)
    {
        ShowDialogCore(shell, content, title);
    }

    public void ShowDialog(LuminaOverlayHost overlayHost, object content, string? title = null)
    {
        ShowDialogCore(overlayHost, content, title);
    }

    public void ShowDialog(LuminaTopView topView, object content, string? title = null)
    {
        ShowDialogCore(topView, content, title);
    }

    public void ShowDialog(Control owner, object content, string? title = null)
    {
        ShowDialogCore(LuminaOverlayHostResolver.FindFor(owner), content, title);
    }

    public void ShowTopDialog(Control owner, object content, string? title = null)
    {
        ShowDialogCore(LuminaOverlayHostResolver.FindTopFor(owner), content, title);
    }

    public Task<bool> ShowConfirmAsync(string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false)
    {
        return ShowConfirmCoreAsync(LuminaOverlayHostResolver.FindDefault(), title, message, confirmText, cancelText, isDanger);
    }

    public Task<bool> ShowConfirmAsync(LuminaShell shell, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false)
    {
        return ShowConfirmCoreAsync(shell, title, message, confirmText, cancelText, isDanger);
    }

    public Task<bool> ShowConfirmAsync(LuminaOverlayHost overlayHost, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false)
    {
        return ShowConfirmCoreAsync(overlayHost, title, message, confirmText, cancelText, isDanger);
    }

    public Task<bool> ShowConfirmAsync(LuminaTopView topView, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false)
    {
        return ShowConfirmCoreAsync(topView, title, message, confirmText, cancelText, isDanger);
    }

    public Task<bool> ShowConfirmAsync(Control owner, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false)
    {
        return ShowConfirmCoreAsync(LuminaOverlayHostResolver.FindFor(owner), title, message, confirmText, cancelText, isDanger);
    }

    public Task<bool> ShowTopConfirmAsync(Control owner, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false)
    {
        return ShowConfirmCoreAsync(LuminaOverlayHostResolver.FindTopFor(owner), title, message, confirmText, cancelText, isDanger);
    }

    public void CloseDialog()
    {
        CloseDialogCore(LuminaOverlayHostResolver.FindDefault());
    }

    public void CloseDialog(LuminaShell shell)
    {
        CloseDialogCore(shell);
    }

    public void CloseDialog(LuminaOverlayHost overlayHost)
    {
        CloseDialogCore(overlayHost);
    }

    public void CloseDialog(LuminaTopView topView)
    {
        CloseDialogCore(topView);
    }

    public void CloseDialog(Control owner)
    {
        CloseDialogCore(LuminaOverlayHostResolver.FindFor(owner));
    }

    public void CloseTopDialog(Control owner)
    {
        CloseDialogCore(LuminaOverlayHostResolver.FindTopFor(owner));
    }

    private static void ShowDialogCore(ILuminaOverlayHost? host, object content, string? title)
    {
        if (host != null)
        {
            Dispatcher.UIThread.Post(() => {
                host.ShowDialog(new LuminaDialog
                {
                    Title = title,
                    Content = content
                });
            });
        }
    }

    private static Task<bool> ShowConfirmCoreAsync(ILuminaOverlayHost? host, string title, string message, string? confirmText, string? cancelText, bool isDanger)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        if (host == null)
        {
            tcs.SetResult(result: false);
            return tcs.Task;
        }
        Dispatcher.UIThread.Post(() => {
            StackPanel stackPanel = new StackPanel
            {
                Margin = new Thickness(24.0)
            };
            TextBlock textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0.0, 0.0, 0.0, 24.0)
            };
            if (Application.Current != null && Application.Current.TryFindResource("LuminaTextSecondaryBrush", out object? value) && value is IBrush foreground)
            {
                textBlock.Foreground = foreground;
            }
            stackPanel.Children.Add(textBlock);
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12.0,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Button cancelButton = new Button
            {
                Content = (cancelText ?? LuminaLocalization.Get("Lumina.Common.Cancel")),
                Classes = { "Outline" }
            };
            cancelButton.Click += (_, _) => {
                host.CloseDialog();
                tcs.TrySetResult(result: false);
            };
            Button confirmButton = new Button
            {
                Content = (confirmText ?? LuminaLocalization.Get("Lumina.Common.Confirm"))
            };
            confirmButton.Classes.Add(isDanger ? "Danger" : "Primary");
            confirmButton.Click += (_, _) => {
                host.CloseDialog();
                tcs.TrySetResult(result: true);
            };
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(confirmButton);
            stackPanel.Children.Add(buttonPanel);
            host.ShowDialog(new LuminaDialog
            {
                Title = title,
                Content = stackPanel
            });
        });
        return tcs.Task;
    }

    private static void CloseDialogCore(ILuminaOverlayHost? host)
    {
        if (host != null)
        {
            Dispatcher.UIThread.Post(() => {
                host.CloseDialog();
            });
        }
    }
}
