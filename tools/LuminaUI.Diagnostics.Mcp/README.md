# LuminaUI Diagnostics MCP 工具

`LuminaUI.Diagnostics.Mcp` 是独立的 dotnet tool 包，用 stdio MCP server 把 MCP 客户端连接到运行中的 Avalonia 应用。

它只负责 live diagnostics，不索引文档。组件知识、示例、设计令牌、API 和包安装信息由独立的 `DotNetCatalog.Mcp` 服务提供。

## 应用侧接入

应用需要引用 `LuminaUI.Diagnostics`，并在 `AppBuilder` 上启用 diagnostics：

```csharp
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

默认命名管道格式：

```text
lumina-ui-diagnostics-{pid}
```

## 运行工具

```powershell
dotnet tool install --global LuminaUI.Diagnostics.Mcp
lumina-mcp
```

本仓库开发时也可以直接运行项目：

```powershell
dotnet run --project tools/LuminaUI.Diagnostics.Mcp/LuminaUI.Diagnostics.Mcp.csproj
```

## 目标选择

大多数面向应用的 tools 都支持 `pid` 或 `pipe` 参数：

- `pid`：根据进程 ID 自动拼出默认命名管道。
- `pipe`：直接指定完整命名管道名。
- `timeoutMs`：单次请求超时时间，默认 30000 毫秒。

## 常用 Tools

- `discover_apps`：发现启用 LuminaUI diagnostics 的运行中应用。
- `list_windows`：列出 Avalonia 窗口。
- `get_visual_tree` / `get_logical_tree`：读取控件树。
- `find_control`：按名称、类型或文本搜索控件。
- `get_control_properties`：读取控件属性。
- `get_data_context`：读取 DataContext。
- `get_binding_errors`：读取绑定错误。
- `take_screenshot`：截取窗口或控件截图。
- `click_control` / `input_text` / `set_property` / `invoke_command`：执行基础交互。
