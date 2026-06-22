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
| `LuminaUI.Diagnostics.Abstractions` | Shared protocol contracts for LuminaUI diagnostics packages and tools. |
| `LuminaUI.Diagnostics` | Application-side named-pipe diagnostics host for Avalonia apps. |
| `LuminaUI.Diagnostics.Mcp` | Dotnet tool package for the live diagnostics MCP stdio server. |

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
                    DefaultPageTitle="Dashboard">
  <lumina:LuminaShell.MenuContent>
    <lumina:LuminaNavigationView />
  </lumina:LuminaShell.MenuContent>

  <lumina:LuminaPage Header="Dashboard">
    <TextBlock Text="Content" />
  </lumina:LuminaPage>
</lumina:LuminaShell>
```

Use `LuminaOverlayHost` directly only when an application does not use `LuminaShell` but still needs global overlays or platform safe-area handling.

## Repository Layout

```text
src/
  LuminaUI/              Core controls, theme resources, localization, shell, and services.
  LuminaUI.ColorPicker/  Optional ColorPicker theme package.
  LuminaUI.DataGrid/     Optional DataGrid theme package.
  LuminaUI.TreeDataGrid/ Optional TreeDataGrid theme package.
  LuminaUI.Diagnostics.Abstractions/
                          Shared diagnostics protocol contracts.
  LuminaUI.Diagnostics/  Application-side diagnostics host.

demo/
  LuminaUI.Demo/         Shared demo views and view models.
  LuminaUI.Demo.Desktop/ Desktop host and diagnostics entry point.
  LuminaUI.Demo.Browser/ Browser host.
  LuminaUI.Demo.Android/ Android host.
  LuminaUI.Demo.iOS/     iOS host.

tools/
  LuminaUI.Diagnostics.Mcp/
                          Live app diagnostics MCP stdio server.
```

Documentation, component, API, and package indexing is handled by the separate `DotNetCatalog` project. This repository only keeps the live diagnostics MCP tool, which connects to running Avalonia applications that opt in with `LuminaUI.Diagnostics`.

## Versioning

The repository uses separate version domains so tool packages are not forced to follow the main control library release cadence:

- `LuminaUIVersion`: main controls and optional themed control packages.
- `LuminaUIDiagnosticsVersion`: diagnostics protocol contracts and application-side host.
- `LuminaUIDiagnosticsMcpVersion`: live diagnostics MCP dotnet tool.

`LuminaUIVersion` lives in `Directory.Build.props`. Diagnostics versions live in `Directory.Build.Diagnostics.props`.

Only bump the version for the package whose public API, protocol, or behavior changed. Main control library changes do not require synchronized MCP tool version bumps.

## Diagnostics MCP

Application-side setup:

```csharp
using Avalonia;
using LuminaUI.Diagnostics;

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
#if DEBUG
        .UseLuminaUIDiagnostics()
#endif
        ;
```

Keep diagnostics behind `#if DEBUG` unless the app is an internal diagnostic build. Also make the package reference Debug-only so release publish output does not include the diagnostics package:

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <PackageReference Include="LuminaUI.Diagnostics" Version="<resolved-version>" />
</ItemGroup>
```

If `dotnet add package LuminaUI.Diagnostics` was used first, keep the generated version and move that `PackageReference` into the conditional `ItemGroup`.

The diagnostics host listens on a deterministic pipe name:

```text
lumina-ui-diagnostics-{pid}
```

The dotnet tool package exposes the stdio MCP server command:

```bash
lumina-mcp
```

Use the separate `DotNetCatalog.Mcp` service for documentation, component, API, and package catalog queries.

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
dotnet pack src/LuminaUI.Diagnostics.Abstractions/LuminaUI.Diagnostics.Abstractions.csproj -c Release
dotnet pack src/LuminaUI.Diagnostics/LuminaUI.Diagnostics.csproj -c Release
dotnet pack tools/LuminaUI.Diagnostics.Mcp/LuminaUI.Diagnostics.Mcp.csproj -c Release
```

## Release

NuGet publishing and GitHub Releases are split by version domain:

- `LuminaUI`: publishes the main control packages from `LuminaUIVersion`.
- `LuminaUI Diagnostics MCP`: publishes `LuminaUI.Diagnostics.Abstractions`, `LuminaUI.Diagnostics`, and `LuminaUI.Diagnostics.Mcp` from `LuminaUIDiagnosticsVersion` and `LuminaUIDiagnosticsMcpVersion`.

1. Set the repository secret `NUGET_API_KEY` to a NuGet.org API key.
2. Optional: set `GH_RELEASE_TOKEN` to a GitHub token with `Contents: Read and write` if the default Actions `GITHUB_TOKEN` cannot create releases in this repository.
3. Update the matching version property in `Directory.Build.props` or `Directory.Build.Diagnostics.props` for the packages included in the release.
4. Push the change to `master` or `main`.

```bash
git add Directory.Build.props Directory.Build.Diagnostics.props
git commit -m "Release 0.1.0"
git push origin master
```

If the matching tag does not already exist, the selected workflow builds the relevant packable projects, publishes `.nupkg` and `.snupkg` files to NuGet, creates a release tag, and publishes a GitHub Release. The main workflow also builds the Desktop, Browser, Android, and iOS simulator demo release assets.

Release notes are generated by GitHub from merged PRs and commits. The first release starts at `0.1.0`.

## Development Notes

- Keep reusable styling in theme dictionaries and prefer semantic Lumina resources over hard-coded colors.
- Keep optional integrations in their own packages so applications can depend only on the controls they use.
- Prefer MVVM commands and bindable properties in demo pages; use code-behind only for view composition and platform glue.
- When changing visible text, update both English and `zh-CN` resources.
- Validate UI changes with the desktop demo before packaging.

## License

LuminaUI is released under the MIT License. See [LICENSE](LICENSE).
