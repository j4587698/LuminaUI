using System;

namespace LuminaUI.Controls;

public class LuminaSettingsSwitchOption : LuminaSettingsOption
{
    protected override Type StyleKeyOverride => typeof(LuminaSettingsOption);

    public LuminaSettingsSwitchOption()
    {
        Kind = LuminaSettingsOptionKind.Switch;
        ShowChevron = false;
    }
}
