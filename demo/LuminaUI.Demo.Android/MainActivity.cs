using Android.App;
using Android.Content.PM;
using Android.Views;
using Avalonia.Android;

namespace LuminaUI.Demo.Android;

[Activity(
    Label = "LuminaUI Demo",
    Theme = "@style/LuminaUI.Theme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity;
