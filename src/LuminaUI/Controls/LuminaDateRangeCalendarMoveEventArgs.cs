using System;

namespace LuminaUI.Controls;

public sealed class LuminaDateRangeCalendarMoveEventArgs(int monthOffset) : EventArgs
{
    public int MonthOffset { get; } = monthOffset;
}
