using System;

namespace LuminaUI.Controls;

public class LuminaSettingsInputOption : LuminaSettingsOption
{
    protected override Type StyleKeyOverride => typeof(LuminaSettingsOption);

    public LuminaSettingsInputOption()
    {
        Kind = LuminaSettingsOptionKind.TextInput;
        ShowChevron = true;
    }
}
