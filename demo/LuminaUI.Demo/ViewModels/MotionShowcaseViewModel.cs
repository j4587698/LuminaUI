using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class MotionShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _showMiddleItem = true;

    [ObservableProperty]
    private bool _isWide;

    [ObservableProperty]
    private bool _isDimmed;

    [ObservableProperty]
    private bool _showStatus = true;

    [ObservableProperty]
    private bool _showMotionPanel = true;

    [ObservableProperty]
    private bool _isMotionControlEnabled = true;

    [ObservableProperty]
    private double _moveX;

    [ObservableProperty]
    private double _springMoveX;

    [ObservableProperty]
    private double _previewScale = 1;

    [ObservableProperty]
    private double _rotationAngle;

    [ObservableProperty]
    private string _motionStatusText = "Start / 1.00x";

    [ObservableProperty]
    private string _rotationStatusText = "0deg";

    [ObservableProperty]
    private string _sizeStatusText = "Size and opacity are ready";

    public double PreviewWidth => IsWide ? 260 : 112;

    public double PreviewHeight => IsWide ? 112 : 72;

    public double PreviewOpacity => IsDimmed ? 0.42 : 1;

    [RelayCommand]
    private void ToggleMove()
    {
        MoveX = MoveX == 0 ? 150 : 0;
        UpdateMotionStatus();
    }

    [RelayCommand]
    private void ToggleScale()
    {
        PreviewScale = PreviewScale == 1 ? 1.22 : 1;
        UpdateMotionStatus();
    }

    [RelayCommand]
    private void ToggleRotation()
    {
        RotationAngle = RotationAngle == 0 ? 135 : 0;
        RotationStatusText = $"{RotationAngle:0}deg";
    }

    [RelayCommand]
    private void ToggleSize()
    {
        IsWide = !IsWide;
        SizeStatusText = IsWide ? "Preview expanded" : "Preview collapsed";
    }

    [RelayCommand]
    private void ToggleOpacity()
    {
        IsDimmed = !IsDimmed;
        SizeStatusText = IsDimmed ? "Opacity reduced" : "Opacity restored";
    }

    [RelayCommand]
    private void ToggleMotionPanel()
    {
        ShowMotionPanel = !ShowMotionPanel;
    }

    [RelayCommand]
    private void ToggleMotionEnabled()
    {
        IsMotionControlEnabled = !IsMotionControlEnabled;
    }

    [RelayCommand]
    private void ToggleSpringMove()
    {
        SpringMoveX = SpringMoveX == 0 ? 150 : 0;
    }

    [RelayCommand]
    private void Reset()
    {
        ShowMiddleItem = true;
        IsWide = false;
        IsDimmed = false;
        ShowStatus = true;
        ShowMotionPanel = true;
        IsMotionControlEnabled = true;
        MoveX = 0;
        SpringMoveX = 0;
        PreviewScale = 1;
        RotationAngle = 0;
        MotionStatusText = "Start / 1.00x";
        RotationStatusText = "0deg";
        SizeStatusText = "Size and opacity are ready";
    }

    partial void OnShowMiddleItemChanged(bool value)
    {
        SizeStatusText = value ? "Middle item restored" : "Middle item hidden";
    }

    partial void OnShowStatusChanged(bool value)
    {
        SizeStatusText = value ? "Status row visible" : "Status row hidden";
    }

    partial void OnIsWideChanged(bool value)
    {
        OnPropertyChanged(nameof(PreviewWidth));
        OnPropertyChanged(nameof(PreviewHeight));
    }

    partial void OnIsDimmedChanged(bool value)
    {
        OnPropertyChanged(nameof(PreviewOpacity));
    }

    private void UpdateMotionStatus()
    {
        var position = MoveX == 0 ? "Start" : "Right";
        MotionStatusText = $"{position} / {PreviewScale:0.00}x";
    }
}
