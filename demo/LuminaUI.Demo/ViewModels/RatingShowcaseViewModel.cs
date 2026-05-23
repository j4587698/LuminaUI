using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class RatingShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private double _ratingValue = 3.5;

    [ObservableProperty]
    private double _compactValue = 4;
}

