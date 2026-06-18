using System;

namespace LuminaUI.Controls;

public interface ILuminaOverlayHost
{
    TimeSpan ToastDuration { get; }

    void ShowToast(object? content);

    void ShowToast(object? content, TimeSpan duration);

    void ClearToast();

    void ShowDialog(object? content);

    void CloseDialog();

    void ShowBottomSheet(object? content);

    void CloseBottomSheet();

    void ShowDrawer(object? content);

    void CloseDrawer();
}
