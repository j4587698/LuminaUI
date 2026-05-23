using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class ColorPickerShowcaseViewModel : ObservableObject
{
    private bool _isSynchronizingColor;

    [ObservableProperty]
    private Color _accentColor = Color.Parse("#3B82F6");

    [ObservableProperty]
    private HsvColor _accentHsvColor = new(Color.Parse("#3B82F6"));

    [ObservableProperty]
    private IBrush _accentBrush = new SolidColorBrush(Color.Parse("#3B82F6"));

    [ObservableProperty]
    private string _hexValue = "#FF3B82F6";

    [ObservableProperty]
    private bool _isAlphaVisible = true;

    [ObservableProperty]
    private bool _isColorPaletteVisible = true;

    [ObservableProperty]
    private bool _isColorComponentsVisible = true;

    [ObservableProperty]
    private bool _isColorSpectrumVisible = true;

    [ObservableProperty]
    private bool _isColorPreviewVisible = true;

    [ObservableProperty]
    private bool _isHexInputVisible = true;

    partial void OnAccentColorChanged(Color value)
    {
        AccentBrush = new SolidColorBrush(value);
        HexValue = value.ToString();

        if (_isSynchronizingColor)
        {
            return;
        }

        _isSynchronizingColor = true;
        AccentHsvColor = new HsvColor(value);
        _isSynchronizingColor = false;
    }

    partial void OnAccentHsvColorChanged(HsvColor value)
    {
        if (_isSynchronizingColor)
        {
            return;
        }

        _isSynchronizingColor = true;
        AccentColor = (Color)value;
        _isSynchronizingColor = false;
    }
}
