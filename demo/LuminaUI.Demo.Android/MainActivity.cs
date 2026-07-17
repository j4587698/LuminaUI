using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.SplashScreen;
using Avalonia.Android;
using LuminaUI.Demo;

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
    }

    protected override void OnDestroy()
    {
        DemoPlatformServices.ExitApplication = null;
        base.OnDestroy();
    }
}
