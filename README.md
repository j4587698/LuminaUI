# LuminaUI

[中文说明](README.zh-CN.md)

LuminaUI is an Avalonia 12 component library for desktop, browser, and mobile-style application shells. It provides a shared visual system, reusable controls, navigation primitives, overlays, localization support, and optional themed integrations for ColorPicker, DataGrid, and TreeDataGrid.

The NuGet packages target `.NET 8`, `.NET 9`, and `.NET 10`. Demo hosts remain on `.NET 10`. The current Avalonia version is `12.0.4`.

## Packages

| Package | Purpose |
| --- | --- |
| `LuminaUI` | Core theme, semantic resources, controls, shell, navigation, overlays, localization, and motion helpers. |
| `LuminaUI.ColorPicker` | Lumina theme integration for Avalonia ColorPicker and ColorView. |
| `LuminaUI.DataGrid` | Lumina theme integration for Avalonia DataGrid. |
| `LuminaUI.TreeDataGrid` | Lumina theme integration for TreeDataGrid.Avalonia. |

## Features

- Light and dark theme dictionaries with semantic color, border, surface, glass, and shadow tokens.
- Application shell primitives: `LuminaWindow`, `LuminaShell`, `LuminaOverlayHost`, `LuminaPage`, dialogs, bottom sheets, and toast surfaces.
- Foundation controls: cards, glass surfaces, group boxes, images, avatars, badges, loading states, skeletons, empty states, and disabled containers.
- Action controls: buttons, toggle buttons, repeat buttons, button groups, drop-down buttons, split buttons, command bars, and pop confirmations.
- Navigation and layout: navigation view, navigation pages, drawer pages, tab pages, tab strips, tab controls, carousel, breadcrumb, split view, and transitioning content.
- Data entry and display: text inputs, OTP input, forms, selection controls, pickers, date range picker, range slider, pagination, descriptions, properties, timeline, steps, indexed lists, and linked categories.
- Localization resources with application-level override support.
- Shared demo project with Desktop, Browser, Android, and iOS hosts.

## Quick Start

Reference the packages or projects you need, then register the theme in `App.axaml`.

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:lumina="clr-namespace:LuminaUI;assembly=LuminaUI"
             xmlns:luminaColorPicker="using:LuminaUI.ColorPicker"
             xmlns:luminaDataGrid="using:LuminaUI.DataGrid"
             xmlns:luminaTreeDataGrid="using:LuminaUI.TreeDataGrid">
  <Application.Styles>
    <FluentTheme />
    <lumina:LuminaTheme />

    <!-- Optional packages -->
    <luminaColorPicker:LuminaColorPickerTheme />
    <luminaDataGrid:LuminaDataGridTheme />
    <luminaTreeDataGrid:LuminaTreeDataGridTheme />
  </Application.Styles>
</Application>
```

Use controls with the Lumina namespace:

```xml
<lumina:LuminaCard Classes="Glass" Padding="16">
  <StackPanel Spacing="8">
    <TextBlock Classes="SectionTitle" Text="Overview" />
    <Button Classes="Primary" Content="Save" />
  </StackPanel>
</lumina:LuminaCard>
```

For applications that use `LuminaShell`, make the shell the root view. `LuminaShell` hosts an internal `LuminaOverlayHost` that collects platform safe-area insets, applies them to shell chrome, and scopes dialogs, bottom sheets, drawers, and toasts.

```xml
<lumina:LuminaShell ShellKey="App"
                    DefaultPageTitle="Dashboard"
                    FooterBackground="{DynamicResource LuminaSurfaceBrush}">
  <lumina:LuminaShell.MenuContent>
    <lumina:LuminaNavigationView />
  </lumina:LuminaShell.MenuContent>

  <lumina:LuminaShell.FooterContent>
    <TextBlock Text="Persistent footer" Padding="16,12" />
  </lumina:LuminaShell.FooterContent>

  <lumina:LuminaShell.OverlayContent>
    <Border HorizontalAlignment="Stretch"
            VerticalAlignment="Bottom"
            Margin="16">
      <TextBlock Text="Persistent non-modal overlay" />
    </Border>
  </lumina:LuminaShell.OverlayContent>

  <lumina:LuminaPage Header="Dashboard">
    <TextBlock Text="Content" />
  </lumina:LuminaPage>
</lumina:LuminaShell>
```

`FooterContent` participates in layout, owns the bottom safe-area inset, and follows shell chrome visibility. `OverlayContent` fills the safe content area above the footer, so its children can position themselves with the standard Avalonia alignment and margin properties without manually calculating safe-area or footer offsets. It remains below shell dialogs, drawers, bottom sheets, and toasts.

Root shells use transparent system-bar backgrounds by default while drawing edge to edge. Set `UseTransparentSystemBars="False"` to preserve a platform-defined system-bar color. Platform hosts should still synchronize system-bar icon brightness when an application theme changes independently of the operating-system theme; the Android demo shows this integration.

Use `LuminaOverlayHost` directly only when an application does not use `LuminaShell` but still needs global overlays or platform safe-area handling.

### System back handling

`LuminaShell` handles system back requests in this order: modal overlays, ordinary control handlers, the shell menu, and the navigation page stack. An ordinary overlay does not need to become a `Page`; register its existing close command directly:

```xml
<Grid IsVisible="{Binding IsPanelOpen}"
      lumina:LuminaBack.IsEnabled="{Binding IsPanelOpen}"
      lumina:LuminaBack.Command="{Binding ClosePanelCommand}">
  <!-- Overlay content -->
</Grid>
```

Handlers at the same priority run from the deepest visual owner outward, then in last-in-first-out order at the same depth. When no handler consumes the request, the shell raises `UnhandledBackRequested`. If the event remains unhandled, the request is passed to the platform, which enables application policies such as “press back again to exit.”

## Repository Layout

```text
src/
  LuminaUI/              Core controls, theme resources, localization, shell, and services.
  LuminaUI.ColorPicker/  Optional ColorPicker theme package.
  LuminaUI.DataGrid/     Optional DataGrid theme package.
  LuminaUI.TreeDataGrid/ Optional TreeDataGrid theme package.

demo/
  LuminaUI.Demo/         Shared demo views and view models.
  LuminaUI.Demo.Desktop/ Desktop host.
  LuminaUI.Demo.Browser/ Browser host.
  LuminaUI.Demo.Android/ Android host.
  LuminaUI.Demo.iOS/     iOS host.
```

## Diagnostics (MCP & DevTools)

LuminaUI.Diagnostics is a separate [repository](https://github.com/j4587698/LuminaUI.Diagnostics) that provides live application diagnostics over a named-pipe protocol, including an MCP stdio server for AI tooling (e.g. opencode) and an optional F12 DevTools window.

### Quick setup

```bash
dotnet add package LuminaUI.Diagnostics
```

```csharp
// Program.cs
using LuminaUI.Diagnostics;

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .UseSkia()
        .UseHarfBuzz()
        .WithInterFont()
        .LogToTrace()
#if DEBUG
        .UseLuminaUIDiagnostics();
#endif
```

This starts the diagnostics host (named-pipe server) and registers F12 to open the DevTools window. Disable DevTools with `o.EnableDevTools = false` if you only need MCP.

### MCP tool

```bash
# install globally
dotnet tool install -g LuminaUI.Diagnostics.Mcp
lumina-mcp

# or .NET 10+ one-shot
dnx lumina-mcp
```

Configure your MCP client:

```json
{
  "servers": {
    "LuminaUI.Diagnostics": {
      "type": "stdio",
      "command": "lumina-mcp"
    }
  }
}
```

See the [LuminaUI.Diagnostics](https://github.com/j4587698/LuminaUI.Diagnostics) repository for full documentation.

## Build and Run

Restore and build the desktop demo and library projects:

```bash
dotnet restore demo/LuminaUI.Demo.Desktop/LuminaUI.Demo.Desktop.csproj
dotnet build demo/LuminaUI.Demo.Desktop/LuminaUI.Demo.Desktop.csproj
```

Run the desktop demo:

```bash
dotnet run --project demo/LuminaUI.Demo.Desktop/LuminaUI.Demo.Desktop.csproj
```

Run the browser, Android, or iOS hosts when the corresponding .NET workloads are installed. The iOS host requires macOS and Xcode.

```bash
dotnet run --project demo/LuminaUI.Demo.Browser/LuminaUI.Demo.Browser.csproj
dotnet build demo/LuminaUI.Demo.Android/LuminaUI.Demo.Android.csproj
dotnet build demo/LuminaUI.Demo.iOS/LuminaUI.Demo.iOS.csproj
```

After all platform workloads are installed, `dotnet build LuminaUI.slnx` builds the full workspace.

Create local packages:

```bash
dotnet pack src/LuminaUI/LuminaUI.csproj -c Release
dotnet pack src/LuminaUI.ColorPicker/LuminaUI.ColorPicker.csproj -c Release
dotnet pack src/LuminaUI.DataGrid/LuminaUI.DataGrid.csproj -c Release
dotnet pack src/LuminaUI.TreeDataGrid/LuminaUI.TreeDataGrid.csproj -c Release
```

## Versioning

The main control packages follow `LuminaUIVersion` in `Directory.Build.props`.

## License

LuminaUI is released under the MIT License. See [LICENSE](LICENSE).
