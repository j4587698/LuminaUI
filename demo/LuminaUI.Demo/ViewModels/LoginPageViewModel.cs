using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class LoginPageViewModel : ObservableObject
{
    private readonly Action? _loginRequested;

    [ObservableProperty]
    private string _email = "admin@lumina.dev";

    [ObservableProperty]
    private string _password = "lumina";

    [ObservableProperty]
    private bool _rememberMe = true;

    public LoginPageViewModel()
    {
    }

    public LoginPageViewModel(Action? loginRequested)
    {
        _loginRequested = loginRequested;
    }

    [RelayCommand]
    private void Login()
    {
        _loginRequested?.Invoke();
    }
}
