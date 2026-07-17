using System;
using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Core.SplashScreen;
using AndroidX.Core.View;
using Avalonia.Android;
using LuminaUI.Demo;
using LuminaUI.Theming;

namespace LuminaUI.Demo.Android;

[Activity(
    Label = "LuminaUI Demo",
    Icon = "@mipmap/appicon",
    Theme = "@style/LuminaUI.LaunchTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustNothing,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        DemoPlatformServices.ExitApplication = FinishAndRemoveTask;
        LuminaThemeManager.ThemeModeChanged += OnThemeModeChanged;
        ApplySystemBarAppearance();
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        Window?.DecorView.Post(ApplySystemBarAppearance);
    }

    protected override void OnDestroy()
    {
        LuminaThemeManager.ThemeModeChanged -= OnThemeModeChanged;
        DemoPlatformServices.ExitApplication = null;
        base.OnDestroy();
    }

    private void OnThemeModeChanged(object? sender, EventArgs e)
    {
        ApplySystemBarAppearance();
    }

    private void ApplySystemBarAppearance()
    {
        if (Window is not { } window)
        {
            return;
        }

        bool useDarkIcons = !LuminaThemeManager.IsDarkThemeActive;
        var controller = new WindowInsetsControllerCompat(window, window.DecorView)
        {
            AppearanceLightStatusBars = useDarkIcons,
            AppearanceLightNavigationBars = useDarkIcons
        };
    }
}
