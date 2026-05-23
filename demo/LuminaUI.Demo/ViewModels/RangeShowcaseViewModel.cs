using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class RangeShowcaseViewModel : ObservableObject
{
    public double DurationSeconds { get; } = 222;

    public string DurationText => FormatTime(DurationSeconds);

    [ObservableProperty]
    private double _playbackPosition = 78;

    [ObservableProperty]
    private double _bufferedPosition = 156;

    [ObservableProperty]
    private string _positionText = FormatTime(78);

    [ObservableProperty]
    private double _sharedProgress = 64;

    [ObservableProperty]
    private string _sharedProgressText = FormatPercent(64);

    partial void OnPlaybackPositionChanged(double value)
    {
        PositionText = FormatTime(value);
    }

    partial void OnSharedProgressChanged(double value)
    {
        SharedProgressText = FormatPercent(value);
    }

    private static string FormatTime(double seconds)
    {
        var clamped = Math.Max(0, seconds);
        var totalSeconds = (int)Math.Round(clamped);
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    private static string FormatPercent(double value)
    {
        return $"{Math.Clamp(value, 0, 100):0}%";
    }
}
