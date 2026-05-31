using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace LuminaUI.Mcp.Tools;

[McpServerToolType]
public sealed class LuminaMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly CatalogStore _store;

    public LuminaMcpTools(CatalogStore store)
    {
        _store = store;
    }

    [McpServerTool]
    [Description("Search LuminaUI components, enums, examples, and design tokens by natural language query. Returns matching items ranked by relevance.")]
    public string lumina_search(
        [Description("Search query, e.g. 'button loading', 'pagination', 'color picker', 'card elevation'")] string query,
        [Description("Limit results per category (default 5)")] int limit = 5)
    {
        var results = new Dictionary<string, object>();

        var components = _store.SearchComponents(query, limit);
        if (components.Count > 0)
            results["components"] = components.Select(c => new { c.Name, c.BaseType, c.Category, c.Description });

        var enums = _store.SearchEnums(query, limit);
        if (enums.Count > 0)
            results["enums"] = enums.Select(e => new { e.Name, e.Namespace, ValueCount = e.Values.Count });

        var examples = _store.SearchExamples(query, limit);
        if (examples.Count > 0)
            results["examples"] = examples.Select(e => new { e.ComponentName, e.ShowcasePage });

        var tokens = _store.SearchTokens(query, limit);
        if (tokens.Count > 0)
            results["tokens"] = tokens.Select(t => new { t.Name, t.Category, t.LightValue, t.DarkValue });

        if (results.Count == 0)
            return $"No results found for '{query}'. Try broader terms like 'button', 'card', 'input', 'navigation', 'loading', 'dialog'.";

        var matchCount = components.Count + enums.Count + examples.Count + tokens.Count;
        return ToJson(new
        {
            summary = $"Found {matchCount} LuminaUI catalog matches for '{query}'.",
            query,
            results
        });
    }

    [McpServerTool]
    [Description("List all LuminaUI components, optionally filtered by category. Categories: Action, DataEntry, DataDisplay, Feedback, Navigation, Layout, Animation, Effect, Utility, General.")]
    public string lumina_list_components(
        [Description("Optional category filter")] string? category = null,
        [Description("Max results (default 50)")] int limit = 50)
    {
        var components = _store.ListComponents(category, limit);
        if (components.Count == 0)
            return $"No components found{(category is not null ? $" in category '{category}'" : "")}.";

        var result = components.Select(c => new
        {
            c.Name,
            c.BaseType,
            c.Category,
            PropertyCount = c.Properties.Count,
            StyleClasses = c.StyleClasses.Count
        });

        return ToJson(new
        {
            summary = $"Found {components.Count} LuminaUI components{(category is not null ? $" in category '{category}'" : "")}.",
            components = result
        });
    }

    [McpServerTool]
    [Description("Get detailed information about a specific LuminaUI component, including its properties, style classes, and usage hints.")]
    public string lumina_get_component(
        [Description("Component name, e.g. 'LuminaCard', 'LuminaButton', 'LuminaPagination'")] string name)
    {
        var component = _store.GetComponent(name);
        if (component is null)
        {
            // Try partial match
            var all = _store.ListComponents(limit: 500);
            var similar = all.Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).Take(5).ToList();
            if (similar.Count > 0)
                return $"Component '{name}' not found. Did you mean: {string.Join(", ", similar.Select(c => c.Name))}?";
            return $"Component '{name}' not found. Use lumina_list_components to see all available components.";
        }

        var result = new
        {
            component.Name,
            component.Namespace,
            component.Assembly,
            component.BaseType,
            component.Category,
            component.Description,
            AxamlUsage = $"xmlns:lumina=\"{component.AxamlNamespace}\"",
            Properties = component.Properties.Select(p => new
            {
                p.Name,
                p.PropertyType,
                p.Kind,
                p.DefaultValue,
                p.IsTwoWay,
                XamlExample = p.Kind == "Attached"
                    ? $"lumina:{component.Name}.{p.Name}=\"...\""
                    : $"{p.Name}=\"...\""
            }),
            StyleClasses = component.StyleClasses,
            component.ThemeResources
        };

        return ToJson(new
        {
            summary = $"Component '{component.Name}' with {component.Properties.Count} properties and {component.StyleClasses.Count} style classes.",
            component = result
        });
    }

    [McpServerTool]
    [Description("Get the showcase example code for a LuminaUI component. Returns AXAML, code-behind, and ViewModel source code.")]
    public string lumina_get_example(
        [Description("Component name or showcase page name, e.g. 'LuminaCard', 'ButtonShowcasePage', 'Pagination'")] string name)
    {
        // Try direct component name lookup
        var example = _store.GetExample(name);

        // Try with "Lumina" prefix removed
        if (example is null && !name.StartsWith("Lumina"))
            example = _store.GetExample($"Lumina{name}");

        // Try showcase page lookup
        if (example is null)
        {
            var examples = _store.GetExamplesByShowcase(name);
            example = examples.FirstOrDefault();
        }

        // Try search
        if (example is null)
        {
            var searchResults = _store.SearchExamples(name, 1);
            example = searchResults.FirstOrDefault();
        }

        if (example is null)
            return $"No example found for '{name}'. Use lumina_search to find available examples.";

        var result = new
        {
            example.ComponentName,
            example.ShowcasePage,
            AxamlSource = TruncateSource(example.AxamlSource, 8000),
            CodeBehind = TruncateSource(example.CodeBehindSource, 4000),
            ViewModel = TruncateSource(example.ViewModelSource, 4000),
            Note = "Source code may be truncated. Use the full showcase page as reference."
        };

        return ToJson(new
        {
            summary = $"Example for '{example.ComponentName}' from {example.ShowcasePage}.",
            example = result
        });
    }

    [McpServerTool]
    [Description("Get LuminaUI design tokens (colors, brushes, shadows, typography, spacing). Useful for theming and styling.")]
    public string lumina_get_tokens(
        [Description("Optional category filter: Color, Brush, Shadow, Typography, Size, Shape, Spacing, Surface, Border, Primary, Semantic, Other")] string? category = null,
        [Description("Optional search query within tokens")] string? query = null)
    {
        if (query is not null)
        {
            var searchResults = _store.SearchTokens(query, 50);
            if (searchResults.Count == 0)
                return $"No tokens found matching '{query}'.";
            return ToJson(new
            {
                summary = $"Found {searchResults.Count} LuminaUI design tokens matching '{query}'.",
                tokens = searchResults
            });
        }

        if (category is not null)
        {
            var tokens = _store.GetTokensByCategory(category);
            if (tokens.Count == 0)
                return $"No tokens found in category '{category}'.";
            return ToJson(new
            {
                summary = $"Found {tokens.Count} LuminaUI design tokens in category '{category}'.",
                tokens
            });
        }

        var allTokens = _store.GetAllTokens();
        if (allTokens.Count == 0)
            return "No design tokens indexed. The catalog may need to be rebuilt.";

        var grouped = allTokens.GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Select(t => new { t.Name, t.LightValue, t.DarkValue, t.Description }));

        return ToJson(new
        {
            summary = $"Found {allTokens.Count} LuminaUI design tokens grouped by category.",
            tokensByCategory = grouped
        });
    }

    [McpServerTool]
    [Description("Get installation and setup instructions for LuminaUI in an Avalonia project.")]
    public string lumina_get_installation(
        [Description("Optional: specific package to install. Options: 'LuminaUI', 'LuminaUI.ColorPicker', 'LuminaUI.DataGrid', 'LuminaUI.TreeDataGrid'")] string? package = null)
    {
        var version = _store.CurrentVersion?.LibraryVersion ?? "latest";

        var result = new
        {
            Prerequisites = new[]
            {
                "Avalonia 12.0 or later",
                ".NET 8.0 or later",
                "An existing Avalonia project"
            },
            Packages = new[]
            {
                new { Name = "LuminaUI", Description = "Core controls, themes, and shell", Required = true },
                new { Name = "LuminaUI.ColorPicker", Description = "Color picker controls", Required = false },
                new { Name = "LuminaUI.DataGrid", Description = "DataGrid controls", Required = false },
                new { Name = "LuminaUI.TreeDataGrid", Description = "TreeDataGrid controls", Required = false }
            },
            Installation = package is null
                ? "dotnet add package LuminaUI"
                : $"dotnet add package {package}",
            AppAxaml = """
                <Application xmlns="https://github.com/avaloniaui"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:lumina="clr-namespace:LuminaUI;assembly=LuminaUI"
                             xmlns:luminaColorPicker="using:LuminaUI.ColorPicker"
                             xmlns:luminaDataGrid="using:LuminaUI.DataGrid"
                             xmlns:luminaTreeDataGrid="using:LuminaUI.TreeDataGrid"
                             x:Class="YourApp.App">
                    <Application.Styles>
                        <FluentTheme />
                        <lumina:LuminaTheme />
                        <luminaColorPicker:LuminaColorPickerTheme />
                        <luminaDataGrid:LuminaDataGridTheme />
                        <luminaTreeDataGrid:LuminaTreeDataGridTheme />
                    </Application.Styles>
                </Application>
                """,
            ComponentUsage = """
                <Window xmlns="https://github.com/avaloniaui"
                        xmlns:lumina="clr-namespace:LuminaUI.Controls;assembly=LuminaUI">
                    <lumina:LuminaCard Padding="16">
                        <StackPanel Spacing="8">
                            <TextBlock Text="Hello LuminaUI" Classes="SectionTitle" />
                            <Button Content="Click me" Classes="Primary" />
                        </StackPanel>
                    </lumina:LuminaCard>
                </Window>
                """,
            Notes = new[]
            {
                "Use Classes=\"Primary|Success|Warning|Danger|Outline|Ghost\" for button variants",
                "Use Classes=\"Small|Large\" for size variants",
                "LuminaCard supports IsElevated=\"True\" and Classes=\"Glass\"",
                "Theme tokens use DynamicResource, e.g. {DynamicResource LuminaPrimaryBrush}",
                "All components follow Avalonia styling rules (Selectors, not Triggers)"
            },
            CatalogVersion = version
        };

        return ToJson(new
        {
            summary = $"LuminaUI installation instructions for {package ?? "the core package"}.",
            installation = result
        });
    }

    [McpServerTool]
    [Description("Get the current catalog status including version, generation time, and counts.")]
    public string lumina_catalog_status()
    {
        var version = _store.CurrentVersion;
        if (version is null)
            return "Catalog not initialized. POST /admin/reindex to build the catalog.";

        return ToJson(new
        {
            summary = $"LuminaUI catalog is ready with {version.ComponentCount} components.",
            version.LibraryVersion,
            version.SourceCommit,
            version.GeneratedAt,
            version.ComponentCount,
            Status = "ready"
        });
    }

    private static string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string? TruncateSource(string? source, int maxLength)
    {
        if (source is null) return null;
        if (source.Length <= maxLength) return source;
        return source[..maxLength] + "\n// ... (truncated)";
    }
}
