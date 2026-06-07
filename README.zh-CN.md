# LuminaUI

[English README](README.md)

LuminaUI 是一套面向 Avalonia 12 的组件库，覆盖桌面、浏览器和移动端风格的应用壳层。它提供统一的视觉体系、可复用控件、导航基础设施、覆盖层、本地化支持，以及 ColorPicker、DataGrid、TreeDataGrid 的可选主题集成。

NuGet 包当前支持 `.NET 8`、`.NET 9` 和 `.NET 10`。Demo 宿主保持 `.NET 10`。Avalonia 版本为 `12.0.4`。

## 包结构

| 包名 | 用途 |
| --- | --- |
| `LuminaUI` | 核心主题、语义资源、控件、Shell、导航、覆盖层、本地化和动效辅助 API。 |
| `LuminaUI.ColorPicker` | Avalonia ColorPicker 和 ColorView 的 Lumina 主题集成。 |
| `LuminaUI.DataGrid` | Avalonia DataGrid 的 Lumina 主题集成。 |
| `LuminaUI.TreeDataGrid` | TreeDataGrid.Avalonia 的 Lumina 主题集成。 |
| `LuminaUI.Diagnostics.Abstractions` | LuminaUI diagnostics 包和工具共享的协议契约。 |
| `LuminaUI.Diagnostics` | Avalonia 应用侧命名管道 diagnostics host。 |
| `LuminaUI.Diagnostics.Mcp` | live diagnostics MCP stdio server 的 dotnet tool 包。 |

## 功能概览

- 浅色和深色主题字典，包含语义色、边框、表面、玻璃效果和阴影 Token。
- 应用壳层基础设施：`LuminaWindow`、`LuminaTopView`、`LuminaShell`、`LuminaPage`、对话框、底部 Sheet 和轻提示表面。
- 基础控件：卡片、玻璃表面、分组框、图片、头像、徽章、加载状态、骨架屏、空状态和禁用容器。
- 操作控件：按钮、切换按钮、重复按钮、按钮组、下拉按钮、拆分按钮、命令栏和弹出确认。
- 导航与布局：导航视图、导航页、抽屉页、标签页容器、标签条、标签页、轮播、面包屑、拆分视图和过渡内容。
- 数据录入与展示：文本输入、验证码输入、表单、选择控件、选择器、日期范围、范围滑块、分页、描述列表、属性列表、时间线、步骤条、索引列表和联动分类。
- 支持应用层覆盖的本地化资源。
- 共享 Demo 项目，并提供 Desktop、Browser、Android 和 iOS 宿主。

## 快速开始

引用需要的包或项目后，在 `App.axaml` 中注册主题。

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

    <!-- 可选包 -->
    <luminaColorPicker:LuminaColorPickerTheme />
    <luminaDataGrid:LuminaDataGridTheme />
    <luminaTreeDataGrid:LuminaTreeDataGridTheme />
  </Application.Styles>
</Application>
```

在页面中使用 Lumina 命名空间下的控件：

```xml
<lumina:LuminaCard Classes="Glass" Padding="16">
  <StackPanel Spacing="8">
    <TextBlock Classes="SectionTitle" Text="概览" />
    <Button Classes="Primary" Content="保存" />
  </StackPanel>
</lumina:LuminaCard>
```

## 目录结构

```text
src/
  LuminaUI/              核心控件、主题资源、本地化、Shell 和服务。
  LuminaUI.ColorPicker/  可选 ColorPicker 主题包。
  LuminaUI.DataGrid/     可选 DataGrid 主题包。
  LuminaUI.TreeDataGrid/ 可选 TreeDataGrid 主题包。
  LuminaUI.Diagnostics.Abstractions/
                          共享 diagnostics 协议契约。
  LuminaUI.Diagnostics/  应用侧 diagnostics host。

demo/
  LuminaUI.Demo/         共享 Demo 视图和 ViewModel。
  LuminaUI.Demo.Desktop/ 桌面端宿主和诊断入口。
  LuminaUI.Demo.Browser/ 浏览器宿主。
  LuminaUI.Demo.Android/ Android 宿主。
  LuminaUI.Demo.iOS/     iOS 宿主。

tools/
  LuminaUI.Mcp/          组件知识和文档 MCP 服务。
  LuminaUI.Diagnostics.Mcp/
                          live app diagnostics MCP stdio 服务。
```

文档 MCP 服务（`tools/LuminaUI.Mcp`）保留为唯一的文档和组件知识入口。diagnostics MCP 工具（`tools/LuminaUI.Diagnostics.Mcp`）独立存在，只连接已通过 `LuminaUI.Diagnostics` opt-in 的运行中 Avalonia 应用。

## 版本策略

仓库内使用独立版本域，避免工具包被主控件库的发布节奏绑住：

- `LuminaUIVersion`：主控件包和可选控件主题包。
- `LuminaUIDiagnosticsVersion`：diagnostics 协议契约和应用侧 host。
- `LuminaUIDiagnosticsMcpVersion`：live diagnostics MCP dotnet tool。
- `LuminaDocsMcpVersion`：文档 MCP 服务。

`LuminaUIVersion` 位于 `Directory.Build.props`。Diagnostics 版本位于 `Directory.Build.Diagnostics.props`。文档 MCP 版本位于 `Directory.Build.Mcp.props`。

只有对应包的公开 API、协议或行为发生变化时才需要升级对应版本。仅主控件库改动时，不需要同步升级 MCP 工具版本。

## Diagnostics MCP

应用侧接入：

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

建议默认把 diagnostics 限制在 `#if DEBUG` 下，除非这是明确的内部诊断构建。同时把包引用也限制为 Debug 条件，避免 Release publish 输出包含 diagnostics 包：

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <PackageReference Include="LuminaUI.Diagnostics" Version="<resolved-version>" />
</ItemGroup>
```

如果先执行了 `dotnet add package LuminaUI.Diagnostics`，保留它生成的版本号，把这条 `PackageReference` 移到带 `Condition` 的 `ItemGroup` 里。

diagnostics host 使用确定性的命名管道：

```text
lumina-ui-diagnostics-{pid}
```

dotnet tool 包提供 stdio MCP server 命令：

```bash
lumina-mcp
```

文档 MCP 仍通过 HTTP 暴露在 `http://localhost:3001/mcp`。

## 构建与运行

还原并构建桌面 Demo 和库项目：

```bash
dotnet restore demo/LuminaUI.Demo.Desktop/LuminaUI.Demo.Desktop.csproj
dotnet build demo/LuminaUI.Demo.Desktop/LuminaUI.Demo.Desktop.csproj
```

运行桌面 Demo：

```bash
dotnet run --project demo/LuminaUI.Demo.Desktop/LuminaUI.Demo.Desktop.csproj
```

安装对应 .NET workload 后，可以运行或构建 Browser、Android 和 iOS 宿主。iOS 宿主需要 macOS 和 Xcode。

```bash
dotnet run --project demo/LuminaUI.Demo.Browser/LuminaUI.Demo.Browser.csproj
dotnet build demo/LuminaUI.Demo.Android/LuminaUI.Demo.Android.csproj
dotnet build demo/LuminaUI.Demo.iOS/LuminaUI.Demo.iOS.csproj
```

安装全部平台 workload 后，可以使用 `dotnet build LuminaUI.slnx` 构建整个工作区。

生成本地 NuGet 包：

```bash
dotnet pack src/LuminaUI/LuminaUI.csproj -c Release
dotnet pack src/LuminaUI.ColorPicker/LuminaUI.ColorPicker.csproj -c Release
dotnet pack src/LuminaUI.DataGrid/LuminaUI.DataGrid.csproj -c Release
dotnet pack src/LuminaUI.TreeDataGrid/LuminaUI.TreeDataGrid.csproj -c Release
dotnet pack src/LuminaUI.Diagnostics.Abstractions/LuminaUI.Diagnostics.Abstractions.csproj -c Release
dotnet pack src/LuminaUI.Diagnostics/LuminaUI.Diagnostics.csproj -c Release
dotnet pack tools/LuminaUI.Diagnostics.Mcp/LuminaUI.Diagnostics.Mcp.csproj -c Release
```

## 发布

NuGet 发布和 GitHub Release 按版本域拆分：

- `LuminaUI`：根据 `LuminaUIVersion` 发布主控件包。
- `LuminaUI Diagnostics MCP`：根据 `LuminaUIDiagnosticsVersion` 和 `LuminaUIDiagnosticsMcpVersion` 发布 `LuminaUI.Diagnostics.Abstractions`、`LuminaUI.Diagnostics` 和 `LuminaUI.Diagnostics.Mcp`。

1. 在仓库 Secret 中配置 `NUGET_API_KEY`，值为 NuGet.org API key。
2. 可选：如果仓库默认的 Actions `GITHUB_TOKEN` 无法创建 Release，在仓库 Secret 中配置 `GH_RELEASE_TOKEN`，权限需要包含 `Contents: Read and write`。
3. 按本次发布涉及的包，更新 `Directory.Build.props`、`Directory.Build.Diagnostics.props` 或 `Directory.Build.Mcp.props` 中对应的版本属性。
4. 将改动推送到 `master` 或 `main`。

```bash
git add Directory.Build.props Directory.Build.Diagnostics.props Directory.Build.Mcp.props
git commit -m "Release 0.1.0"
git push origin master
```

如果匹配的 tag 尚不存在，对应 workflow 会构建相关可打包项目，将 `.nupkg` 和 `.snupkg` 发布到 NuGet，创建 release tag，并发布 GitHub Release。主 workflow 还会构建 Desktop、Browser、Android 和 iOS simulator Demo 发布产物。

Release notes 由 GitHub 根据合并的 PR 和 commit 自动生成。首个版本从 `0.1.0` 开始。

## 开发说明

- 可复用样式应放在主题字典中，优先使用 Lumina 语义资源，避免散落硬编码颜色。
- 可选控件集成保持独立包结构，让应用只引用实际需要的模块。
- Demo 页面优先使用 MVVM 命令和可绑定属性；code-behind 只处理视图组合和平台粘合逻辑。
- 修改可见文本时，同步更新英文和 `zh-CN` 资源。
- 打包前先通过桌面 Demo 验证 UI 变化。

## 许可证

LuminaUI 使用 MIT License 发布。详见 [LICENSE](LICENSE)。
