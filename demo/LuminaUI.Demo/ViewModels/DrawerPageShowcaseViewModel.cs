using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class DrawerPageShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private DrawerPlacement _drawerPlacement = DrawerPlacement.Left;

    [ObservableProperty]
    private bool _isDrawerOpen = true;

    [ObservableProperty]
    private double _drawerLength = 220.0;

    [RelayCommand]
    private void SetDrawerPlacement(DrawerPlacement placement)
    {
        DrawerPlacement = placement;
        IsDrawerOpen = true;
    }

    [RelayCommand]
    private void CloseDrawer()
    {
        IsDrawerOpen = false;
    }
}
