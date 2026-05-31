using System;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaDatePicker : CalendarDatePicker
{
    public static readonly StyledProperty<string> DateFormatProperty =
        AvaloniaProperty.Register<LuminaDatePicker, string>(nameof(DateFormat), "yyyy-MM-dd");

    public string DateFormat
    {
        get => GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(CalendarDatePicker);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DateFormatProperty)
        {
            LuminaPickerFormat.ApplyDateFormat(this, DateFormat);
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LuminaPickerFormat.ApplyDateFormat(this, DateFormat);
    }
}
