using System;

namespace LuminaUI.Controls;

public class LuminaSettingsSelectOption : LuminaSettingsOption
{
	protected override Type StyleKeyOverride => typeof(LuminaSettingsOption);

	public LuminaSettingsSelectOption()
	{
		base.Kind = LuminaSettingsOptionKind.Select;
		base.ShowChevron = true;
	}
}
