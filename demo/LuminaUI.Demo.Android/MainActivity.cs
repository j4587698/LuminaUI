using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace LuminaUI.Demo.Android;

[Activity(
    Label = "LuminaUI Demo",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity;
