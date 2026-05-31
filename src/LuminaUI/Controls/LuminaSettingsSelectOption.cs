using System;

namespace LuminaUI.Controls;

public class LuminaSettingsSelectOption : LuminaSettingsOption
{
    protected override Type StyleKeyOverride => typeof(LuminaSettingsOption);

    public LuminaSettingsSelectOption()
    {
        Kind = LuminaSettingsOptionKind.Select;
        ShowChevron = true;
    }
}
