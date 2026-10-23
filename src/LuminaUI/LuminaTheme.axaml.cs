using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using LuminaUI.Controls;

namespace LuminaUI;

/// <summary>
/// The Lumina visual system. Add this single entry to <see cref="Application.Styles"/>; it already
/// contains the Fluent base theme that Lumina's control themes layer on top of.
/// </summary>
public class LuminaTheme : Styles
{
    public LuminaTheme()
    {
        AvaloniaXamlLoader.Load(this);

        // The theme is instantiated while the application's XAML is being loaded
        // (Application.Initialize), which is the only window during which an application-level
        // native menu can still be picked up by Avalonia's macOS menu exporter. Installing the
        // default Lumina application menu here makes the "About <App>" entry work automatically,
        // without each app having to register it manually. No-op on non-macOS platforms.
        LuminaWindow.InstallDefaultApplicationMenu();
    }
}
