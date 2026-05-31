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
        return TryShowCore(LuminaShell.Current, content);
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
        return TryShowCore(FindHost(owner), content);
    }

    public bool TryShowAtTop(Control owner, object content)
    {
        return TryShowCore(LuminaTopView.FindOuterFor(owner), content);
    }

    public void Close()
    {
        CloseCore(LuminaShell.Current);
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
        CloseCore(FindHost(owner));
    }

    public void CloseAtTop(Control owner)
    {
        CloseCore(LuminaTopView.FindOuterFor(owner));
    }

    private static bool TryShowCore(ILuminaOverlayHost? host, object content)
    {
        if (host == null)
        {
            return false;
        }
        Dispatcher.UIThread.Post(() => {
            host.ShowBottomSheet(new LuminaBottomSheet
            {
                Content = content
            });
        });
        return true;
    }

    private static ILuminaOverlayHost? FindHost(Control owner)
    {
        ILuminaOverlayHost? luminaOverlayHost = LuminaShell.FindFor(owner);
        return luminaOverlayHost ?? LuminaTopView.FindFor(owner);
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
