using Avalonia.Controls;
using LuminaUI.Localization;

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
        return LuminaLocalization.Get("Sandbox.Text.0018");
    }

    public static string GetWindowStatus(WindowTransparencyLevel actualTransparencyLevel)
    {
        return SupportsHardwareBlur(actualTransparencyLevel)
            ? LuminaLocalization.Format(SandboxLocalization.WindowGlassActiveFormat, actualTransparencyLevel)
            : LuminaLocalization.Format(SandboxLocalization.WindowGlassFallbackFormat, actualTransparencyLevel);
    }
}
