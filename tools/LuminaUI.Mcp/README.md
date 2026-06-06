# LuminaUI 文档 MCP 服务

这是 LuminaUI 的唯一文档 MCP 入口。它索引控件、枚举、示例和设计令牌，并通过 MCP tools 提供给 VS Code Copilot 以及其他 MCP 客户端。

运行中应用的 live diagnostics 不放在这里处理，请使用独立的 `LuminaUI.Diagnostics.Mcp` dotnet tool 包。

## 本地运行

```powershell
dotnet run --project tools/LuminaUI.Mcp/LuminaUI.Mcp.csproj
```

服务默认监听 `http://localhost:3001`，MCP 端点是 `http://localhost:3001/mcp`。

VS Code 可以直接使用 [.vscode/mcp.json](../../.vscode/mcp.json)：

```json
{
  "servers": {
    "LuminaUI": {
      "type": "http",
      "url": "http://localhost:3001/mcp"
    }
  }
}
```

MCP 客户端不会自动启动服务。请先运行 `Run LuminaUI MCP` task、执行上面的命令，或使用 Docker Compose。

## Docker 运行

```powershell
$env:MCP_ADMIN_PASSWORD = "change-this"
$env:MCP_AUTO_INDEX_ON_EMPTY = "true"
$env:MCP_DEFAULT_REF = "main"
docker compose up --build lumina-mcp
```

可选环境变量：

- `MCP_ADMIN_PASSWORD`：管理端密码。Docker Compose 启动时必须设置。
- `MCP_REMOTE_URL`：reindex 时 clone 的远端仓库，默认 `https://github.com/j4587698/LuminaUI.git`。
- `MCP_AUTO_INDEX_ON_EMPTY`：设为 `true` 后，首次启动且 catalog 为空时自动构建 catalog。
- `MCP_DEFAULT_REF`：自动索引使用的可选 branch 或 tag。

Docker 镜像不会在 build 阶段内置本机源码或预生成 catalog。首次上线可以启用 `MCP_AUTO_INDEX_ON_EMPTY=true`，或登录管理端手动 `Build Catalog`。服务提供两个公开健康端点：

- `/healthz`：进程存活检查。
- `/readyz`：catalog 就绪检查；catalog 为空时返回 503。

## Docker Hub 发布

仓库包含 `.github/workflows/docs-mcp-docker.yml`。当 `Directory.Build.Mcp.props` 中的 `LuminaDocsMcpVersion` 发生变化时，workflow 会构建 `tools/LuminaUI.Mcp/Dockerfile` 并推送 Docker Hub 镜像。

需要配置：

- Repository secret `DOCKERHUB_USERNAME`：Docker Hub 用户名。
- Repository secret `DOCKERHUB_TOKEN`：Docker Hub access token。
- 可选 repository variable `DOCKERHUB_REPOSITORY`：完整镜像名，例如 `j4587698/luminaui-mcp`。不设置时默认使用 `${DOCKERHUB_USERNAME}/luminaui-mcp`。

推送 tag：

- `${LuminaDocsMcpVersion}`
- `latest`

## 构建 Catalog

打开 `http://localhost:3001/admin` 登录，然后点击 `Build Catalog`。默认开发登录信息是 `admin / luminaui`。

Docker/线上部署必须设置 `Admin__Password` 或 `MCP_ADMIN_PASSWORD`，否则服务会拒绝启动。本地开发默认允许 `admin / luminaui`，但非 Development 环境使用默认密码时仍会输出警告。

## MCP Tools

- `lumina_search`：搜索控件、枚举、示例和设计令牌。
- `lumina_list_components`：列出控件，可按分类过滤。
- `lumina_get_component`：获取控件属性和使用提示。
- `lumina_get_example`：获取 showcase 的 AXAML、code-behind 和 ViewModel 源码。
- `lumina_get_tokens`：查看设计令牌。
- `lumina_get_installation`：获取 Avalonia 应用接入指引。
- `lumina_get_diagnostics_setup`：解释 AI 如何把 `LuminaUI.Diagnostics` 安装并接入到目标 Avalonia 程序。
- `lumina_catalog_status`：查看 catalog 版本和生成状态。
- `lumina_list_mcp_tools`：列出并解释 LuminaUI 文档 MCP 和 diagnostics MCP 的 tools、边界和常见用法。
- `lumina_find_mcp`：根据任务描述推荐使用文档 MCP、diagnostics MCP，或两者组合。

## MCP 选择建议

- 查组件、属性、示例、token、安装方式：使用本文档 MCP。
- 目标程序尚未启用 Diagnostics：先用本文档 MCP 的 `lumina_get_diagnostics_setup` 让 AI 修改目标项目。
- 查运行中窗口、控件树、绑定错误、DataContext、截图或执行 UI 交互：使用 `LuminaUI.Diagnostics.Mcp`。
- 既要知道“怎么写”，又要确认“运行时是否正确”：先用本文档 MCP 查组件和示例，再用 diagnostics MCP 检查目标应用。
