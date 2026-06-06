# LuminaUI Diagnostics MCP 工具

`LuminaUI.Diagnostics.Mcp` 是独立的 dotnet tool 包，用 stdio MCP server 把 MCP 客户端连接到运行中的 Avalonia 应用。

它只负责 live diagnostics，不索引文档。组件知识、示例和设计令牌仍由 `tools/LuminaUI.Mcp` 这个文档 MCP 服务统一提供。

## 应用侧接入

应用需要引用 `LuminaUI.Diagnostics`，并在 `AppBuilder` 上启用 diagnostics：

```csharp
using LuminaUI.Diagnostics;

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseLuminaUIDiagnostics();
```

默认命名管道格式：

```text
lumina-ui-diagnostics-{pid}
```

## 运行工具

```powershell
dotnet tool install --global LuminaUI.Diagnostics.Mcp
lumina-ui-diagnostics-mcp
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
