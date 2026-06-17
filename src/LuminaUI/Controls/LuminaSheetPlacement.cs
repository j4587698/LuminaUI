namespace LuminaUI.Controls;

public static class LuminaSheetPlacement
{
    public static bool ShouldUseSheet(LuminaPopupType popupType)
    {
        return popupType switch
        {
            LuminaPopupType.Sheet => true,
            LuminaPopupType.Auto => LuminaPlatform.PreferSheetPopup,
            _ => false
        };
    }

    public static bool IsMobilePlatform()
    {
        return LuminaPlatform.PreferSheetPopup;
    }
}
