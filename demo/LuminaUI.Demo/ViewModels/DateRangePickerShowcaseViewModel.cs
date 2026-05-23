using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class DateRangePickerShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRangeText))]
    private DateTime? _startDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRangeText))]
    private DateTime? _endDate = DateTime.Today.AddDays(6);

    [ObservableProperty]
    private string _dateFormat = "yyyy-MM-dd";

    public string SelectedRangeText => StartDate == null || EndDate == null
        ? "No date range selected"
        : $"{StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}";

    [RelayCommand]
    private void SelectThisWeek()
    {
        StartDate = DateTime.Today;
        EndDate = DateTime.Today.AddDays(6);
    }

    [RelayCommand]
    private void SelectNextMonth()
    {
        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        StartDate = firstDay;
        EndDate = firstDay.AddMonths(1).AddDays(-1);
    }
}
