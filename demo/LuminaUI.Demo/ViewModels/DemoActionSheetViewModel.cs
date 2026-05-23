using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Demo.ViewModels;

public partial class DemoActionSheetViewModel : ObservableObject
{
    private readonly Action? _closeRequested;

    public DemoActionSheetViewModel(Action? closeRequested = null)
    {
        _closeRequested = closeRequested;
    }

    [RelayCommand]
    private void Select(object? content)
    {
        LuminaToastService.Instance.Show(
            LuminaLocalization.Format(SandboxLocalization.CommonSelectedFormat, content));

        _closeRequested?.Invoke();
    }
}
