namespace LuminaUI.Controls;

public static class LuminaSheetPlacement
{
    public static bool ShouldUseSheet(LuminaPopupType popupType)
    {
        return popupType switch
        {
            LuminaPopupType.Sheet => true,
            LuminaPopupType.Auto => IsMobilePlatform(),
            _ => false
        };
    }

    public static bool IsMobilePlatform()
    {
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
    }
}
