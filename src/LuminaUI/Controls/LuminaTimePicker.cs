using System;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaTimePicker : TimePicker
{
    public static readonly StyledProperty<string> TimeFormatProperty =
        AvaloniaProperty.Register<LuminaTimePicker, string>(nameof(TimeFormat), "HH:mm");

    public string TimeFormat
    {
        get => GetValue(TimeFormatProperty);
        set => SetValue(TimeFormatProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TimePicker);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TimeFormatProperty)
        {
            LuminaPickerFormat.ApplyTimeFormat(this, TimeFormat);
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LuminaPickerFormat.ApplyTimeFormat(this, TimeFormat);
    }
}
