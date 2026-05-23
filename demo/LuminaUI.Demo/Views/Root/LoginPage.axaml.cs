using System;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LoginPage : LuminaPage
{
    public LoginPage()
        : this(null)
    {
    }

    public LoginPage(Action? loginRequested)
    {
        InitializeComponent();
        DataContext = new LoginPageViewModel(loginRequested);
    }
}
