using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;

namespace LuminaUI.Demo.ViewModels;

public partial class RangeSliderShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceRangeText))]
    private double _priceLower = 120;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceRangeText))]
    private double _priceUpper = 680;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreRangeText))]
    private double _scoreLower = 35;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreRangeText))]
    private double _scoreUpper = 82;

    [ObservableProperty]
    private string _rangeCommandText = "Range command is waiting";

    public string PriceRangeText => $"${PriceLower:0} - ${PriceUpper:0}";

    public string ScoreRangeText => $"{ScoreLower:0} - {ScoreUpper:0}";

    [RelayCommand]
    private void ApplyPricePreset()
    {
        PriceLower = 200;
        PriceUpper = 500;
    }

    [RelayCommand]
    private void RangeChanged(LuminaRangeValue value)
    {
        RangeCommandText = $"Committed range: {value.LowerValue:0} - {value.UpperValue:0}";
    }
}
