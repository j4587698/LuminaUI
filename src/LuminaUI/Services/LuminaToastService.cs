using System;
using Avalonia.Controls;
using Avalonia.Threading;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public class LuminaToastService : ILuminaToastService
{
    public static ILuminaToastService Instance { get; } = new LuminaToastService();

    public void Show(string content, TimeSpan? duration = null)
    {
        ShowCore(LuminaOverlayHostResolver.FindDefault(), content, duration);
    }

    public void Show(LuminaShell shell, string content, TimeSpan? duration = null)
    {
        ShowCore(shell, content, duration);
    }

    public void Show(LuminaTopView topView, string content, TimeSpan? duration = null)
    {
        ShowCore(topView, content, duration);
    }

    public void Show(Control owner, string content, TimeSpan? duration = null)
    {
        ShowCore(LuminaOverlayHostResolver.FindFor(owner), content, duration);
    }

    public void ShowAtTop(Control owner, string content, TimeSpan? duration = null)
    {
        ShowCore(LuminaOverlayHostResolver.FindTopFor(owner), content, duration);
    }

    private static void ShowCore(ILuminaOverlayHost? host, string content, TimeSpan? duration)
    {
        if (host != null)
        {
            Dispatcher.UIThread.Post(() => {
                host.ShowToast(new LuminaToast
                {
                    Content = content
                }, duration ?? host.ToastDuration);
            });
        }
    }
}
