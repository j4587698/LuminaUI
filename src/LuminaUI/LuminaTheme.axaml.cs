using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using LuminaUI.Controls;

namespace LuminaUI;

/// <summary>
/// The Lumina visual system. Add this single entry to <see cref="Application.Styles"/>; it brings
/// the Fluent base theme that Lumina's control themes layer on top of.
/// </summary>
/// <remarks>
/// When the application already registered a base theme before <see cref="LuminaTheme"/> (for
/// example <c>&lt;FluentTheme DensityStyle="Compact" /&gt;</c> with custom palettes), that theme
/// is used as-is and no second Fluent copy is embedded, so its density and palette settings are
/// preserved.
/// </remarks>
public class LuminaTheme : Styles
{
    public LuminaTheme()
    {
        // Must happen before the XAML below is loaded: its BasedOn="{StaticResource {x:Type Foo}}"
        // lookups resolve against the first child of this collection, or - when the host already
        // has a base theme - fall through to the application's resources.
        IncludesBaseTheme = !HostProvidesBaseTheme();
        if (IncludesBaseTheme)
        {
            Add(new FluentTheme());
        }

        AvaloniaXamlLoader.Load(this);

        // The theme is instantiated while the application's XAML is being loaded
        // (Application.Initialize), which is the only window during which an application-level
        // native menu can still be picked up by Avalonia's macOS menu exporter. Installing the
        // default Lumina application menu here makes the "About <App>" entry work automatically,
        // without each app having to register it manually. No-op on non-macOS platforms.
        LuminaWindow.InstallDefaultApplicationMenu();
    }

    /// <summary>
    /// Gets whether this instance embeds its own Fluent base theme. <c>false</c> when the host
    /// application had already registered a base theme when this theme was created.
    /// </summary>
    public bool IncludesBaseTheme { get; }

    /// <summary>
    /// A base theme registers a <see cref="ControlTheme"/> under the <see cref="Window"/> type
    /// key. Application XAML adds each <c>Application.Styles</c> entry as soon as it is created,
    /// so a theme declared ahead of LuminaTheme is already visible here.
    /// </summary>
    private static bool HostProvidesBaseTheme()
    {
        Application? application = Application.Current;
        return application != null
            && application.TryGetResource(typeof(Window), null, out object? theme)
            && theme is ControlTheme;
    }
}
