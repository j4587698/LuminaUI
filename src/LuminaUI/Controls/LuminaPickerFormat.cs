using Avalonia.Controls;

namespace LuminaUI.Controls;

internal static class LuminaPickerFormat
{
    public const string DefaultDateFormat = "yyyy-MM-dd";

    public const string DefaultTimeFormat = "HH:mm";

    public static void ApplyDateFormat(DatePicker picker, string? format)
    {
        string normalized = NormalizeFormat(format, "yyyy-MM-dd");
        picker.YearFormat = ExtractDatePartFormat(normalized, 'y', "yyyy");
        picker.MonthFormat = ExtractDatePartFormat(normalized, 'M', "MM");
        if (normalized.Contains('d'))
        {
            picker.DayFormat = ExtractDatePartFormat(normalized, 'd', "dd");
            picker.DayVisible = true;
        }
        else
        {
            picker.DayVisible = false;
        }
    }

    public static void ApplyDateFormat(CalendarDatePicker picker, string? format)
    {
        picker.SelectedDateFormat = CalendarDatePickerFormat.Custom;
        picker.CustomDateFormatString = NormalizeFormat(format, "yyyy-MM-dd");
    }

    public static void ApplyTimeFormat(TimePicker picker, string? format)
    {
        string normalized = NormalizeFormat(format, "HH:mm");
        picker.ClockIdentifier = normalized.Contains('h') ? "12HourClock" : "24HourClock";
        picker.UseSeconds = normalized.Contains('s');
    }

    private static string NormalizeFormat(string? format, string fallback)
    {
        return string.IsNullOrWhiteSpace(format) ? fallback : format;
    }

    private static string ExtractDatePartFormat(string format, char patternChar, string fallback)
    {
        int start = format.IndexOf(patternChar);
        if (start < 0)
        {
            return fallback;
        }
        int end;
        for (end = start + 1; end < format.Length && format[end] == patternChar; end++)
        {
        }
        return format.Substring(start, end - start);
    }
}
