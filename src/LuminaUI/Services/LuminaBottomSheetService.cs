using Avalonia.Controls;
using Avalonia.Threading;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public class LuminaBottomSheetService
{
    public static LuminaBottomSheetService Instance { get; } = new LuminaBottomSheetService();

    public void Show(object content)
    {
        TryShow(content);
    }

    public void Show(LuminaShell shell, object content)
    {
        TryShow(shell, content);
    }

    public void Show(LuminaTopView topView, object content)
    {
        TryShow(topView, content);
    }

    public void Show(Control owner, object content)
    {
        TryShow(owner, content);
    }

    public void ShowAtTop(Control owner, object content)
    {
        TryShowAtTop(owner, content);
    }

    public bool TryShow(object content)
    {
        return TryShowCore(LuminaOverlayHostResolver.FindDefault(), content);
    }

    public bool TryShow(LuminaShell shell, object content)
    {
        return TryShowCore(shell, content);
    }

    public bool TryShow(LuminaTopView topView, object content)
    {
        return TryShowCore(topView, content);
    }

    public bool TryShow(Control owner, object content)
    {
        return TryShowCore(LuminaOverlayHostResolver.FindFor(owner), content);
    }

    public bool TryShowAtTop(Control owner, object content)
    {
        return TryShowCore(LuminaOverlayHostResolver.FindTopFor(owner), content);
    }

    public void Close()
    {
        CloseCore(LuminaOverlayHostResolver.FindDefault());
    }

    public void Close(LuminaShell shell)
    {
        CloseCore(shell);
    }

    public void Close(LuminaTopView topView)
    {
        CloseCore(topView);
    }

    public void Close(Control owner)
    {
        CloseCore(LuminaOverlayHostResolver.FindFor(owner));
    }

    public void CloseAtTop(Control owner)
    {
        CloseCore(LuminaOverlayHostResolver.FindTopFor(owner));
    }

    private static bool TryShowCore(ILuminaOverlayHost? host, object content)
    {
        if (host == null)
        {
            return false;
        }
        Dispatcher.UIThread.Post(() => {
            host.ShowBottomSheet(LuminaBottomSheet.EnsureSheet(content));
        });
        return true;
    }

    private static void CloseCore(ILuminaOverlayHost? host)
    {
        if (host != null)
        {
            Dispatcher.UIThread.Post(() => {
                host.CloseBottomSheet();
            });
        }
    }
}
