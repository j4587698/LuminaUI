using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public partial class ButtonGroupShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DensityText))]
    private int _selectedDensityIndex = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BillingText))]
    private int _selectedBillingIndex;

    public string DensityText => SelectedDensityIndex switch
    {
        0 => "Compact density selected",
        2 => "Expanded density selected",
        _ => "Comfortable density selected"
    };

    public string BillingText => SelectedBillingIndex switch
    {
        1 => "Weekly billing selected",
        2 => "Monthly billing selected",
        _ => "Daily billing selected"
    };
}
