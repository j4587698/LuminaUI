using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class CarouselShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCarouselAutoPlayEnabled = true;

    [ObservableProperty]
    private double _carouselAutoPlayIntervalSeconds = 2.5;

    [ObservableProperty]
    private string _carouselAutoPlayIntervalText = "2.5s";

    public TimeSpan CarouselAutoPlayInterval => TimeSpan.FromSeconds(Math.Clamp(CarouselAutoPlayIntervalSeconds, 0.5, 30));

    partial void OnCarouselAutoPlayIntervalSecondsChanged(double value)
    {
        CarouselAutoPlayIntervalText = $"{Math.Clamp(value, 0.5, 30):0.#}s";
        OnPropertyChanged(nameof(CarouselAutoPlayInterval));
    }
}
