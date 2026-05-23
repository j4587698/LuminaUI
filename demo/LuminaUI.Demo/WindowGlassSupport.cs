using Avalonia.Controls;

namespace LuminaUI.Demo;

internal static class WindowGlassSupport
{
    public static bool SupportsHardwareBlur(WindowTransparencyLevel actualTransparencyLevel)
    {
        return actualTransparencyLevel == WindowTransparencyLevel.AcrylicBlur
            || actualTransparencyLevel == WindowTransparencyLevel.Blur;
    }

    public static string GetCardStatus(WindowTransparencyLevel actualTransparencyLevel)
    {
        return SandboxTextLocalizer.Localize("Card glass: backdrop blur.");
    }

    public static string GetWindowStatus(WindowTransparencyLevel actualTransparencyLevel)
    {
        return SupportsHardwareBlur(actualTransparencyLevel)
            ? SandboxTextLocalizer.Format("Window glass active: {0}", "窗口玻璃已启用：{0}", actualTransparencyLevel)
            : SandboxTextLocalizer.Format("Window glass fallback: {0}", "窗口玻璃回退：{0}", actualTransparencyLevel);
    }
}
