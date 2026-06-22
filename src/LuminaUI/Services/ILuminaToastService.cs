using System;
using Avalonia.Controls;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public interface ILuminaToastService
{
    void Show(string content, TimeSpan? duration = null);

    void Show(LuminaShell shell, string content, TimeSpan? duration = null);

    void Show(LuminaOverlayHost overlayHost, string content, TimeSpan? duration = null);

    void Show(LuminaTopView topView, string content, TimeSpan? duration = null);

    void Show(Control owner, string content, TimeSpan? duration = null);

    void ShowAtTop(Control owner, string content, TimeSpan? duration = null);
}
