using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class PaginationShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentPage = 4;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _compactPage = 1;

    [ObservableProperty]
    private string _pageStatus = "Current page: 4";

    public int PageCount => 18;

    public int TotalCount => 326;

    [RelayCommand]
    private void PageChanged(int page)
    {
        PageStatus = $"Current page: {page}, page size: {PageSize}";
    }
}
