# LuminaUI MCP Server

HTTP MCP server for LuminaUI component knowledge. It indexes controls, enums, demo examples, and design tokens, then exposes them through MCP tools for VS Code Copilot and other MCP clients.

## Run Locally

```powershell
dotnet run --project tools/LuminaUI.Mcp/LuminaUI.Mcp.csproj
```

The server listens on `http://localhost:3001` by default and exposes MCP at `http://localhost:3001/mcp`.

VS Code can use [.vscode/mcp.json](../../.vscode/mcp.json):

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

The MCP client does not start the server automatically. Use the `Run LuminaUI MCP` task, run the command above, or use Docker Compose.

## Run With Docker

```powershell
$env:MCP_ADMIN_PASSWORD = "change-this"
docker compose up --build lumina-mcp
```

Optional environment variables:

- `MCP_ADMIN_PASSWORD`: admin UI password.
- `MCP_AUTO_INDEX_ON_EMPTY`: set to `true` to build the catalog on first startup.
- `MCP_DEFAULT_REF`: optional branch or tag used by automatic indexing.

## Build The Catalog

Open `http://localhost:3001/admin`, sign in, then click `Build Catalog`. The default development login is `admin / luminaui`.

For non-development use, always set `Admin__Password` or `MCP_ADMIN_PASSWORD`. The server logs a warning when it runs with the default password outside Development.

## MCP Tools

- `lumina_search`: search components, enums, examples, and design tokens.
- `lumina_list_components`: list components, optionally by category.
- `lumina_get_component`: get component properties and usage hints.
- `lumina_get_example`: get showcase AXAML, code-behind, and ViewModel source.
- `lumina_get_tokens`: inspect design tokens.
- `lumina_get_installation`: get setup guidance for Avalonia apps.
- `lumina_catalog_status`: inspect catalog version and generation state.