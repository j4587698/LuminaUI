using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;
using LuminaUI.Theming;

namespace LuminaUI.Demo.ViewModels;

public partial class DesignSystemPageViewModel : ObservableObject
{
    [ObservableProperty]
    private LuminaPageState _pageState = LuminaPageState.Normal;

    [RelayCommand]
    private void ToggleTheme()
    {
        LuminaThemeManager.ToggleThemeVariant();
        SandboxWindowActions.RefreshThemeAndWindowMaterial();
    }

    [RelayCommand]
    private void SetPageState(string? state)
    {
        PageState = state switch
        {
            "Loading" => LuminaPageState.Loading,
            "Empty" => LuminaPageState.Empty,
            "Error" => LuminaPageState.Error,
            _ => LuminaPageState.Normal
        };
    }
}
