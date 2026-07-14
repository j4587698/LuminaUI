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

## 功能概览

- 浅色和深色主题字典，包含语义色、边框、表面、玻璃效果和阴影 Token。
- 应用壳层基础设施：`LuminaWindow`、`LuminaShell`、`LuminaOverlayHost`、`LuminaPage`、对话框、底部 Sheet 和轻提示表面。
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

如果应用使用 `LuminaShell`，推荐直接把 Shell 作为根视图。`LuminaShell` 内部承载 `LuminaOverlayHost`，负责采集平台 safe area、应用到 Shell chrome，并为对话框、底部 Sheet、抽屉和轻提示提供局部覆盖层。

```xml
<lumina:LuminaShell ShellKey="App"
                    DefaultPageTitle="仪表盘">
  <lumina:LuminaShell.MenuContent>
    <lumina:LuminaNavigationView />
  </lumina:LuminaShell.MenuContent>

  <lumina:LuminaPage Header="仪表盘">
    <TextBlock Text="内容" />
  </lumina:LuminaPage>
</lumina:LuminaShell>
```

仅在不使用 `LuminaShell`、但仍需要全局覆盖层或平台安全区处理时，才直接使用 `LuminaOverlayHost`。

## 目录结构

```text
src/
  LuminaUI/              核心控件、主题资源、本地化、Shell 和服务。
  LuminaUI.ColorPicker/  可选 ColorPicker 主题包。
  LuminaUI.DataGrid/     可选 DataGrid 主题包。
  LuminaUI.TreeDataGrid/ 可选 TreeDataGrid 主题包。

demo/
  LuminaUI.Demo/         共享 Demo 视图和 ViewModel。
  LuminaUI.Demo.Desktop/ 桌面端宿主。
  LuminaUI.Demo.Browser/ 浏览器宿主。
  LuminaUI.Demo.Android/ Android 宿主。
  LuminaUI.Demo.iOS/     iOS 宿主。
```

## 诊断（MCP & DevTools）

LuminaUI.Diagnostics 是一个独立的[仓库](https://github.com/j4587698/LuminaUI.Diagnostics)，通过命名管道协议提供实时应用诊断能力，包含 MCP stdio server（用于 opencode 等 AI 工具）和可选的 F12 DevTools 窗口。

### 快速接入

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

以上启动诊断 Host（命名管道服务器）并默认注册 F12 打开 DevTools。仅需 MCP 时，可通过 `o.EnableDevTools = false` 关闭 DevTools。

### MCP 工具

```bash
# 全局安装
dotnet tool install -g LuminaUI.Diagnostics.Mcp
lumina-mcp

# 或 .NET 10+ 免安装运行
dnx lumina-mcp
```

MCP 客户端配置：

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

完整文档请查看 [LuminaUI.Diagnostics](https://github.com/j4587698/LuminaUI.Diagnostics) 仓库。

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
```

## 版本策略

主控件包统一使用 `LuminaUIVersion`，定义在 `Directory.Build.props` 中。

## 许可证

LuminaUI 使用 MIT License 发布。详见 [LICENSE](LICENSE)。
