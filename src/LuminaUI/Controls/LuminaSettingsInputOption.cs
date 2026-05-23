using System;

namespace LuminaUI.Controls;

public class LuminaSettingsInputOption : LuminaSettingsOption
{
	protected override Type StyleKeyOverride => typeof(LuminaSettingsOption);

	public LuminaSettingsInputOption()
	{
		base.Kind = LuminaSettingsOptionKind.TextInput;
		base.ShowChevron = true;
	}
}
