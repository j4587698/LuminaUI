using System;

namespace LuminaUI.Controls;

public sealed class LuminaDateRangeCalendarDateEventArgs(DateTime? date) : EventArgs
{
    public DateTime? Date { get; } = date?.Date;
}
