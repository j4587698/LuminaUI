namespace LuminaUI.Controls;

public static class LuminaPlatform
{
    private static Func<bool>? s_preferSheetPopupProvider;

    private static Func<bool>? s_supportsNativeMenuProvider;

    private static Func<bool>? s_useNativeExtendedSystemButtonsByDefaultProvider;

    private static Func<bool>? s_nativeSystemButtonAreaOnLeftProvider;

    public static bool PreferSheetPopup => (s_preferSheetPopupProvider ?? IsKnownMobilePlatform)();

    public static bool SupportsNativeMenu => (s_supportsNativeMenuProvider ?? IsKnownNativeMenuPlatform)();

    public static bool UseNativeExtendedSystemButtonsByDefault => (s_useNativeExtendedSystemButtonsByDefaultProvider ?? IsKnownNativeExtendedSystemButtonPlatform)();

    public static bool IsNativeSystemButtonAreaOnLeft => (s_nativeSystemButtonAreaOnLeftProvider ?? IsKnownNativeSystemButtonAreaOnLeftPlatform)();

    public static void SetPreferSheetPopupProvider(Func<bool>? provider)
    {
        s_preferSheetPopupProvider = provider;
    }

    public static void ResetPreferSheetPopupProvider()
    {
        s_preferSheetPopupProvider = null;
    }

    public static void SetSupportsNativeMenuProvider(Func<bool>? provider)
    {
        s_supportsNativeMenuProvider = provider;
    }

    public static void ResetSupportsNativeMenuProvider()
    {
        s_supportsNativeMenuProvider = null;
    }

    public static void SetUseNativeExtendedSystemButtonsByDefaultProvider(Func<bool>? provider)
    {
        s_useNativeExtendedSystemButtonsByDefaultProvider = provider;
    }

    public static void ResetUseNativeExtendedSystemButtonsByDefaultProvider()
    {
        s_useNativeExtendedSystemButtonsByDefaultProvider = null;
    }

    public static void SetNativeSystemButtonAreaOnLeftProvider(Func<bool>? provider)
    {
        s_nativeSystemButtonAreaOnLeftProvider = provider;
    }

    public static void ResetNativeSystemButtonAreaOnLeftProvider()
    {
        s_nativeSystemButtonAreaOnLeftProvider = null;
    }

    private static bool IsKnownMobilePlatform()
    {
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
    }

    private static bool IsKnownNativeMenuPlatform()
    {
        return OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();
    }

    private static bool IsKnownNativeExtendedSystemButtonPlatform()
    {
        return OperatingSystem.IsMacOS();
    }

    private static bool IsKnownNativeSystemButtonAreaOnLeftPlatform()
    {
        return OperatingSystem.IsMacOS();
    }
}
