using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class DeleteProjectDialogContentViewModel : ObservableObject
{
    private readonly Action? _closeRequested;

    public DeleteProjectDialogContentViewModel(Action? closeRequested = null)
    {
        _closeRequested = closeRequested;
    }

    [RelayCommand]
    private void Close()
    {
        _closeRequested?.Invoke();
    }
}
