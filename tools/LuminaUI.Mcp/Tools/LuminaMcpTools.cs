using System.ComponentModel;
using System.Text.Json;
using LuminaUI.Mcp.Models;
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
            AxamlUsage = $"xmlns:lumina=\"{component.AxamlNamespace ?? "clr-namespace:LuminaUI.Controls;assembly=LuminaUI"}\"",
            Properties = component.Properties.Select(p => new
            {
                p.Name,
                p.PropertyType,
                p.Kind,
                p.DefaultValue,
                p.IsTwoWay,
                p.Description,
                EnumValues = GetEnumValues(p.PropertyType),
                XamlExample = p.Kind == "Attached"
                    ? $"lumina:{component.Name}.{p.Name}=\"...\""
                    : $"{p.Name}=\"...\""
            }),
            StyleClasses = component.StyleClasses,
            component.ThemeResources,
            Usage = BuildUsageGuide(component)
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
        {
            var component = _store.GetComponent(name)
                ?? (!name.StartsWith("Lumina", StringComparison.OrdinalIgnoreCase)
                    ? _store.GetComponent($"Lumina{name}")
                    : null);

            if (component is not null)
            {
                return ToJson(new
                {
                    summary = $"No showcase source found for '{name}'. Returning component usage guidance instead.",
                    example = new
                    {
                        ComponentName = component.Name,
                        ShowcasePage = (string?)null,
                        Usage = BuildUsageGuide(component),
                        Note = "This is generated or curated usage guidance. Call lumina_get_component for the full property table."
                    }
                });
            }

            return $"No example found for '{name}'. Use lumina_search to find available examples.";
        }

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
    [Description("Get installation and setup instructions for LuminaUI packages, including application-side Diagnostics setup.")]
    public string lumina_get_installation(
        [Description("Optional: specific package to install. Options: 'LuminaUI', 'LuminaUI.ColorPicker', 'LuminaUI.DataGrid', 'LuminaUI.TreeDataGrid', 'LuminaUI.Diagnostics'")] string? package = null)
    {
        if (IsDiagnosticsPackage(package))
            return lumina_get_diagnostics_setup();

        var version = ResolveLuminaPackageVersion();
        var packageName = string.IsNullOrWhiteSpace(package) ? "LuminaUI" : package.Trim();

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
                new { Name = "LuminaUI.TreeDataGrid", Description = "TreeDataGrid controls", Required = false },
                new { Name = "LuminaUI.Diagnostics", Description = "Application-side live diagnostics host for AI-assisted runtime inspection", Required = false }
            },
            Version = new
            {
                CurrentCatalogOrBuild = version,
                Source = _store.CurrentVersion is null
                    ? "Docs MCP server build metadata from Directory.Build.props"
                    : "Current catalog LibraryVersion read from the indexed repository Directory.Build.props",
                InstallPolicy = "Default install commands do not pin a version. NuGet resolves the latest stable package unless the user explicitly asks for a specific version."
            },
            Installation = PackageVersionCatalog.AddPackageCommand("<path-to-app.csproj>", packageName),
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
    [Description("Explain how an AI agent should install LuminaUI.Diagnostics into a target Avalonia application.")]
    public string lumina_get_diagnostics_setup(
        [Description("Optional target project path or name. Use this to make commands concrete for the user's app project.")] string? project = null)
    {
        var projectArgument = string.IsNullOrWhiteSpace(project)
            ? "<path-to-app.csproj>"
            : project.Trim();
        var diagnosticsVersion = PackageVersionCatalog.DiagnosticsVersion;
        var diagnosticsMcpVersion = PackageVersionCatalog.DiagnosticsMcpVersion;

        var result = new
        {
            Purpose = "Enable application-side diagnostics so a preinstalled LuminaUI.Diagnostics.Mcp tool can inspect the running Avalonia app.",
            SeparationOfConcerns = new
            {
                PreinstalledTool = "LuminaUI.Diagnostics.Mcp is a stdio MCP dotnet tool and can be installed in the AI/client environment ahead of time.",
                TargetApplicationChange = "The target Avalonia app still needs the LuminaUI.Diagnostics package and AppBuilder opt-in. This is the part an AI agent should edit in the user's project.",
                NotRequiredInApp = "Do not add LuminaUI.Diagnostics.Mcp to the app project. The app only references LuminaUI.Diagnostics."
            },
            Versions = new
            {
                DiagnosticsPackage = diagnosticsVersion,
                DiagnosticsPackageSource = "Docs MCP server build metadata from Directory.Build.Diagnostics.props: LuminaUIDiagnosticsVersion.",
                DiagnosticsMcpTool = diagnosticsMcpVersion,
                DiagnosticsMcpToolSource = "Docs MCP server build metadata from Directory.Build.Diagnostics.props: LuminaUIDiagnosticsMcpVersion.",
                Policy = "Default install commands do not pin a version. NuGet resolves the latest stable package/tool unless the user explicitly asks for a specific version."
            },
            Prerequisites = new[]
            {
                "Avalonia 12.x application",
                ".NET 8 or later for the app-side diagnostics package",
                "A desktop-style Avalonia host when using window/control-tree inspection"
            },
            InstallPackage = new
            {
                NuGet = PackageVersionCatalog.AddPackageCommand(projectArgument, "LuminaUI.Diagnostics"),
                RepositoryProjectReference = "When developing inside the LuminaUI repository, reference src/LuminaUI.Diagnostics/LuminaUI.Diagnostics.csproj instead of a NuGet package."
            },
            PreinstalledTool = new
            {
                Package = "LuminaUI.Diagnostics.Mcp",
                Command = "lumina-ui-diagnostics-mcp",
                InstallCommand = PackageVersionCatalog.DotnetToolInstallCommand("LuminaUI.Diagnostics.Mcp", "lumina-ui-diagnostics-mcp"),
                Note = "This tool belongs in the AI/client environment, not in the target app project."
            },
            ProgramCsPatch = new
            {
                AddUsing = "using LuminaUI.Diagnostics;",
                AppBuilderExample = """
                    public static AppBuilder BuildAvaloniaApp()
                        => AppBuilder.Configure<App>()
                            .UsePlatformDetect()
                            .UseSkia()
                            .UseHarfBuzz()
                            .WithInterFont()
                            .LogToTrace()
                            .UseLuminaUIDiagnostics();
                    """,
                Placement = "Call UseLuminaUIDiagnostics() on the AppBuilder chain before StartWithClassicDesktopLifetime(args)."
            },
            OptionalConfiguration = new
            {
                Example = """
                    .UseLuminaUIDiagnostics(options =>
                    {
                        options.DiagnosticsPipeName = "my-app-diagnostics";
                        options.DefaultTimeoutMs = 30000;
                        options.StartImmediately = true;
                    });
                    """,
                DefaultPipe = "lumina-ui-diagnostics-{pid}",
                Recommendation = "Keep the default pipe name unless the client needs a stable custom pipe."
            },
            Verify = new[]
            {
                $"dotnet build \"{projectArgument}\"",
                "Run the target app.",
                "From the preinstalled LuminaUI.Diagnostics.Mcp tool, call discover_apps for targeting guidance.",
                "Call list_windows with pid or pipe to confirm the diagnostics host is reachable."
            },
            CommonMistakes = new[]
            {
                "Installing LuminaUI.Diagnostics.Mcp into the target app instead of installing only LuminaUI.Diagnostics.",
                "Adding the package but forgetting UseLuminaUIDiagnostics() in Program.cs.",
                "Trying to inspect an app before it has started its Avalonia desktop lifetime.",
                "Using diagnostics MCP for documentation questions. Use LuminaUI.Mcp for components, examples, tokens, and installation guidance."
            }
        };

        return ToJson(new
        {
            summary = "Application-side LuminaUI.Diagnostics setup instructions for AI-assisted installation.",
            diagnosticsSetup = result
        });
    }

    [McpServerTool]
    [Description("List and explain LuminaUI MCP tools, including when to use the documentation MCP and when to use the diagnostics MCP.")]
    public string lumina_list_mcp_tools(
        [Description("Optional area filter: all, docs, diagnostics, diagnostics-setup, components, examples, tokens, installation, status.")] string? area = "all")
    {
        var normalizedArea = NormalizeArea(area);
        var includeDocs = normalizedArea is "all" or "docs" or "diagnostics" or "diagnostics-setup" or "components" or "examples" or "tokens" or "installation" or "status";
        var includeDiagnostics = normalizedArea is "all" or "diagnostics";

        var sections = new Dictionary<string, object>();

        if (includeDocs)
        {
            sections["documentationMcp"] = new
            {
                name = "LuminaUI.Mcp",
                transport = "HTTP",
                endpoint = "http://localhost:3001/mcp",
                purpose = "查询 LuminaUI 组件知识、示例源码、设计 token、安装方式和 catalog 状态。",
                scope = "静态文档和源码 catalog。它不连接运行中的 Avalonia 应用。",
                tools = GetDocumentationToolGuide(normalizedArea)
            };
        }

        if (includeDiagnostics)
        {
            sections["diagnosticsMcp"] = new
            {
                name = "LuminaUI.Diagnostics.Mcp",
                transport = "stdio dotnet tool",
                command = "lumina-ui-diagnostics-mcp",
                purpose = "连接已启用 LuminaUI.Diagnostics 的运行中 Avalonia 应用，做窗口、控件树、属性、绑定错误、截图和基础交互检查。",
                scope = "live app diagnostics。它不负责组件文档和安装说明。",
                appSetup = new
                {
                    package = "LuminaUI.Diagnostics",
                    appBuilder = "AppBuilder.Configure<App>().UsePlatformDetect().UseLuminaUIDiagnostics();",
                    defaultPipe = "lumina-ui-diagnostics-{pid}"
                },
                tools = DiagnosticsToolGuide
            };
        }

        if (sections.Count == 0)
        {
            return ToJson(new
            {
                summary = $"Unknown area '{area}'. Use all, docs, diagnostics, components, examples, tokens, installation, or status.",
                availableAreas = new[] { "all", "docs", "diagnostics", "diagnostics-setup", "components", "examples", "tokens", "installation", "status" }
            });
        }

        return ToJson(new
        {
            summary = "LuminaUI MCP tool guide. Use lumina_find_mcp when you want a task-specific recommendation.",
            area = normalizedArea,
            sections
        });
    }

    [McpServerTool]
    [Description("Recommend the right LuminaUI MCP endpoint and tool sequence for a user task.")]
    public string lumina_find_mcp(
        [Description("Describe the task, e.g. 'find button example', 'inspect live window visual tree', 'why binding failed', 'which token controls border color'.")] string task,
        [Description("Include a suggested step-by-step tool workflow.")] bool includeWorkflow = true)
    {
        if (string.IsNullOrWhiteSpace(task))
            return "Task is required. Describe what you want to do with LuminaUI or a running Avalonia app.";

        var normalizedTask = task.Trim();
        var docsScore = Score(normalizedTask, DocumentationKeywords);
        var diagnosticsScore = Score(normalizedTask, DiagnosticsKeywords);
        var isDiagnosticsSetupTask = IsDiagnosticsSetupTask(normalizedTask);
        var useDiagnostics = !isDiagnosticsSetupTask && diagnosticsScore > 0 && diagnosticsScore >= docsScore;
        var useDocs = docsScore > 0 || !useDiagnostics;
        var useBoth = docsScore > 0 && diagnosticsScore > 0;

        var recommendation = isDiagnosticsSetupTask
            ? "diagnostics-setup"
            : useBoth
            ? "both"
            : useDiagnostics
                ? "diagnostics"
                : "docs";

        var suggestedTools = recommendation switch
        {
            "diagnostics-setup" => new[]
            {
                "lumina_get_diagnostics_setup",
                "lumina_get_installation(package: \"LuminaUI.Diagnostics\")",
                "After modifying the target app: LuminaUI.Diagnostics.Mcp: discover_apps",
                "After running the target app: LuminaUI.Diagnostics.Mcp: list_windows"
            },
            "both" => new[]
            {
                "lumina_search",
                "lumina_get_component",
                "lumina_get_example",
                "lumina_get_diagnostics_setup if the target app has not opted in yet",
                "LuminaUI.Diagnostics.Mcp: discover_apps",
                "LuminaUI.Diagnostics.Mcp: list_windows",
                "LuminaUI.Diagnostics.Mcp: find_control/get_visual_tree/get_control_properties"
            },
            "diagnostics" => new[]
            {
                "LuminaUI.Diagnostics.Mcp: discover_apps",
                "LuminaUI.Diagnostics.Mcp: list_windows",
                "LuminaUI.Diagnostics.Mcp: get_visual_tree 或 find_control",
                "LuminaUI.Diagnostics.Mcp: get_control_properties/get_data_context/get_binding_errors",
                "LuminaUI.Diagnostics.Mcp: take_screenshot 或基础交互 tools"
            },
            _ => new[]
            {
                "lumina_search",
                "lumina_list_components",
                "lumina_get_component",
                "lumina_get_example",
                "lumina_get_tokens",
                "lumina_get_installation",
                "lumina_get_diagnostics_setup when the task is to enable app-side diagnostics"
            }
        };

        var workflow = includeWorkflow
            ? BuildWorkflow(recommendation)
            : Array.Empty<string>();

        return ToJson(new
        {
            summary = $"Recommended LuminaUI MCP route for: {normalizedTask}",
            recommendation,
            reason = BuildRecommendationReason(recommendation, docsScore, diagnosticsScore),
            endpoints = new
            {
                docs = new
                {
                    name = "LuminaUI.Mcp",
                    endpoint = "http://localhost:3001/mcp",
                    useWhen = "需要组件、属性、示例、token、安装说明或 catalog 状态。"
                },
                diagnostics = new
                {
                    name = "LuminaUI.Diagnostics.Mcp",
                    command = "lumina-ui-diagnostics-mcp",
                    useWhen = "需要检查运行中的 Avalonia 应用、窗口、控件树、属性、绑定错误、截图或交互。"
                }
            },
            suggestedTools,
            workflow
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

    private string ResolveLuminaPackageVersion() =>
        PackageVersionCatalog.Normalize(_store.CurrentVersion?.LibraryVersion)
        ?? PackageVersionCatalog.LuminaUIVersion;

    private object[]? GetEnumValues(string propertyType)
    {
        var enumInfo = _store.GetEnum(NormalizeTypeName(propertyType));
        if (enumInfo is null)
            return null;

        return enumInfo.Values
            .Select(value => (object)new
            {
                value.Name,
                value.Value,
                value.Description
            })
            .ToArray();
    }

    private static string NormalizeTypeName(string typeName)
    {
        var normalized = typeName.Trim();
        if (normalized.StartsWith("global::", StringComparison.Ordinal))
            normalized = normalized["global::".Length..];

        if (normalized.EndsWith("?", StringComparison.Ordinal))
            normalized = normalized[..^1];

        if (normalized.StartsWith("Nullable<", StringComparison.Ordinal) && normalized.EndsWith(">", StringComparison.Ordinal))
            normalized = normalized["Nullable<".Length..^1];

        var lastDot = normalized.LastIndexOf('.');
        return lastDot >= 0 ? normalized[(lastDot + 1)..] : normalized;
    }

    private static ComponentUsageGuide BuildUsageGuide(ComponentInfo component)
    {
        if (component.BaseType.Contains("AvaloniaObject", StringComparison.Ordinal))
        {
            return new ComponentUsageGuide(
                "generated-model",
                null,
                $"var item = new {component.Name}\n{{\n    // Set model properties here.\n}};",
                [
                    "这是数据/选项模型，不是视觉控件。通常不要把它当作 AXAML 根元素使用。",
                    "优先通过父组件的 ItemsSource、Options 或对应数据源间接使用它。",
                    "属性表仍可用于了解绑定字段和状态字段。"
                ],
                ["lumina_get_component", "lumina_get_example"]);
        }

        return component.Name switch
        {
            "LuminaWindow" => new ComponentUsageGuide(
                "curated",
                """
                <lumina:LuminaWindow xmlns="https://github.com/avaloniaui"
                                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                     xmlns:lumina="clr-namespace:LuminaUI.Controls;assembly=LuminaUI"
                                     x:Class="YourApp.MainWindow"
                                     Title="Your App"
                                     Width="1100"
                                     Height="760"
                                     WindowStartupLocation="CenterScreen"
                                     WindowChromeMode="Extended"
                                     ExtendedSystemButtonMode="Auto"
                                     UseWindowGlass="True"
                                     ShowPinButton="True"
                                     ShowFullScreenButton="True">
                    <lumina:LuminaWindow.TitleBarLeftContent>
                        <TextBlock Text="Your App" FontWeight="SemiBold" />
                    </lumina:LuminaWindow.TitleBarLeftContent>

                    <lumina:LuminaTopView TopViewKey="Root">
                        <Grid>
                            <!-- App content -->
                        </Grid>
                    </lumina:LuminaTopView>
                </lumina:LuminaWindow>
                """,
                null,
                [
                    "LuminaWindow is a Window replacement for desktop hosts. Use it as the root AXAML element of MainWindow.",
                    "WindowChromeMode values are Lumina, Extended, and Platform.",
                    "ExtendedSystemButtonMode values are Auto, Native, and Lumina.",
                    "Use TitleBarLeftContent, TitleBarContent, and TitleBarRightContent to customize the title bar.",
                    "Use OverlayContent for window-level overlay content; use LuminaTopView for in-app dialog, toast, and bottom-sheet hosting."
                ],
                ["lumina_get_component", "lumina_get_example", "lumina_get_tokens"]),

            "LuminaTopView" => new ComponentUsageGuide(
                "curated",
                """
                <UserControl xmlns="https://github.com/avaloniaui"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:lumina="clr-namespace:LuminaUI.Controls;assembly=LuminaUI"
                             x:Class="YourApp.Views.RootView">
                    <lumina:LuminaTopView x:Name="RootTopView"
                                          TopViewKey="Root">
                        <Grid>
                            <lumina:LuminaShell ShellKey="App">
                                <!-- Navigation and pages -->
                            </lumina:LuminaShell>
                        </Grid>
                    </lumina:LuminaTopView>
                </UserControl>
                """,
                """
                var topView = LuminaTopView.GetTopView("Root");
                topView?.ShowToast(new TextBlock { Text = "Saved" });
                topView?.ShowDialog(dialogContent);
                topView?.ShowBottomSheet(sheetContent);
                """,
                [
                    "LuminaTopView is an overlay host for the visual root. It is not a Window replacement.",
                    "TopViewKey lets code or view models locate a specific host with LuminaTopView.GetTopView(key).",
                    "Use DialogContent and IsDialogOpen for dialog state, ToastContent or ShowToast for toast state, and BottomSheetContent with IsBottomSheetOpen for bottom sheets.",
                    "ApplySafeAreaPadding defaults to true and is intended for mobile or edge-to-edge layouts."
                ],
                ["lumina_get_component", "lumina_get_example", "lumina_get_diagnostics_setup"]),

            "LuminaBlurBackground" => new ComponentUsageGuide(
                "curated",
                """
                <lumina:LuminaBlurBackground xmlns="https://github.com/avaloniaui"
                                             xmlns:lumina="clr-namespace:LuminaUI.Controls;assembly=LuminaUI"
                                             BlurRadius="44"
                                             EdgeThickness="1">
                    <StackPanel Spacing="8">
                        <TextBlock Text="Blurred panel" FontWeight="SemiBold" />
                        <TextBlock Text="Content goes here." />
                    </StackPanel>
                </lumina:LuminaBlurBackground>
                """,
                null,
                [
                    "这是可直接使用的模糊背景容器。",
                    "TintBrush 和 EdgeBrush 默认来自 LuminaBlurBackgroundTintBrush / LuminaBlurBackgroundEdgeBrush 主题资源。",
                    "适合局部面板背景；窗口级玻璃效果优先使用 LuminaWindow.UseWindowGlass。"
                ],
                ["lumina_get_component", "lumina_get_tokens"]),

            "LuminaVirtualizingWrapPanel" => new ComponentUsageGuide(
                "low-level",
                """
                <ItemsControl xmlns="https://github.com/avaloniaui"
                              xmlns:lumina="clr-namespace:LuminaUI.Controls;assembly=LuminaUI"
                              ItemsSource="{Binding Items}">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <lumina:LuminaVirtualizingWrapPanel MinItemWidth="220"
                                                                EstimatedItemHeight="160"
                                                                HorizontalSpacing="12"
                                                                VerticalSpacing="12" />
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                </ItemsControl>
                """,
                null,
                [
                    "这是低层 ItemsPanel。应用代码通常优先使用 LuminaVirtualizingWrapView。",
                    "只有在需要自定义 ItemsControl.ItemsPanel 时才直接使用它。",
                    "MinItemWidth、MaxItemWidth、ItemWidth、EstimatedItemHeight、MaxColumns 和 CacheLength 控制虚拟化布局。"
                ],
                ["lumina_get_component", "lumina_get_example"]),

            "LuminaTitleBar" => BuildSupportUsage(
                component.Name,
                "LuminaWindow 内部使用的标题栏控件。",
                "应用代码通常通过 LuminaWindow 的 TitleBarLeftContent、TitleBarContent、TitleBarRightContent 和 Show*Button 属性配置标题栏，不直接创建 LuminaTitleBar。"),

            "LuminaButtonPresenter" => BuildSupportUsage(
                component.Name,
                "Button 模板内部使用的内容与图标呈现控件。",
                "应用代码应使用 Button + LuminaButton 附加属性，或使用 LuminaIconButton / LuminaButtonGroup。"),

            "LuminaDateRangeCalendarDayButton" => BuildSupportUsage(
                component.Name,
                "LuminaDateRangeCalendar 内部生成的日期单元格。",
                "应用代码应使用 LuminaDateRangePicker 或 LuminaDateRangeCalendar。"),

            "LuminaLoadingVisual" => BuildSupportUsage(
                component.Name,
                "LuminaLoading 模板内部的绘制控件。",
                "应用代码应优先使用 LuminaLoading 或 LuminaLoadingContainer。"),

            "LuminaRatingItem" => BuildSupportUsage(
                component.Name,
                "LuminaRating 内部生成的评分项。",
                "应用代码应使用 LuminaRating，并通过 Count、Value、AllowHalf、Character 等父组件属性配置。"),

            _ => new ComponentUsageGuide(
                "generated",
                $"""
                <UserControl xmlns="https://github.com/avaloniaui"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:lumina="{component.AxamlNamespace ?? "clr-namespace:LuminaUI.Controls;assembly=LuminaUI"}">
                    <lumina:{component.Name} />
                </UserControl>
                """,
                null,
                [
                    "Generated from component metadata because no curated usage guide is registered for this component.",
                    "Use the Properties table in this response to fill supported AXAML attributes.",
                    "Call lumina_get_example with the component name to check whether a showcase source example exists."
                ],
                ["lumina_get_component", "lumina_get_example"])
        };
    }

    private static ComponentUsageGuide BuildSupportUsage(string componentName, string purpose, string recommendation) =>
        new(
            "support",
            null,
            null,
            [
                purpose,
                recommendation,
                "参数表可以用于理解模板、样式或父组件内部状态；不建议优先把它作为业务页面组件使用。"
            ],
            ["lumina_get_component", "lumina_get_example"]);

    private static string NormalizeArea(string? area) =>
        string.IsNullOrWhiteSpace(area)
            ? "all"
            : area.Trim().ToLowerInvariant();

    private static object[] GetDocumentationToolGuide(string area)
    {
        return DocumentationToolGuide
            .Where(tool => (area is "all" or "docs") || tool.Areas.Contains(area, StringComparer.OrdinalIgnoreCase))
            .Select(tool => new
            {
                tool.Name,
                tool.Purpose,
                tool.UseWhen,
                tool.KeyParameters,
                tool.ExamplePrompts,
                tool.RelatedTools
            })
            .ToArray();
    }

    private static int Score(string task, IReadOnlyList<string> keywords)
    {
        var score = 0;
        foreach (var keyword in keywords)
        {
            if (task.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                score++;
        }

        return score;
    }

    private static bool IsDiagnosticsPackage(string? package)
    {
        if (string.IsNullOrWhiteSpace(package))
            return false;

        var normalizedPackage = package.Trim();
        return normalizedPackage.Equals("LuminaUI.Diagnostics", StringComparison.OrdinalIgnoreCase)
            || normalizedPackage.Equals("Diagnostics", StringComparison.OrdinalIgnoreCase)
            || normalizedPackage.Equals("diagnostics", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDiagnosticsSetupTask(string task) =>
        Score(task, DiagnosticsSetupKeywords) > 0
        && Score(task, DiagnosticsKeywords) > 0;

    private static string[] BuildWorkflow(string recommendation) =>
        recommendation switch
        {
            "diagnostics-setup" =>
            [
                "Call lumina_get_diagnostics_setup to get the exact app-side installation steps.",
                "Edit the target app project: add the LuminaUI.Diagnostics package or project reference.",
                "Edit Program.cs: add using LuminaUI.Diagnostics and call UseLuminaUIDiagnostics() on the AppBuilder chain.",
                "Build and run the target app.",
                "Use the preinstalled LuminaUI.Diagnostics.Mcp tool to call discover_apps and list_windows."
            ],
            "both" =>
            [
                "先用 LuminaUI.Mcp 查询组件或示例，明确应该如何写 XAML、使用哪些属性和 token。",
                "再启动目标 Avalonia 应用，并确保 AppBuilder 调用了 UseLuminaUIDiagnostics()。",
                "用 LuminaUI.Diagnostics.Mcp 的 discover_apps/list_windows 定位进程和窗口。",
                "用 find_control/get_visual_tree/get_control_properties 对照文档结果检查运行时状态。"
            ],
            "diagnostics" =>
            [
                "确认目标应用引用 LuminaUI.Diagnostics 并启用了 UseLuminaUIDiagnostics()。",
                "启动 lumina-ui-diagnostics-mcp。",
                "调用 discover_apps 或直接传 pid/pipe。",
                "按问题类型调用 list_windows、get_visual_tree、find_control、get_control_properties、get_binding_errors 或 take_screenshot。"
            ],
            _ =>
            [
                "先调用 lumina_search 缩小组件、示例或 token 范围。",
                "组件问题调用 lumina_get_component；示例问题调用 lumina_get_example。",
                "主题和样式问题调用 lumina_get_tokens。",
                "安装和接入问题调用 lumina_get_installation。"
            ]
        };

    private static string BuildRecommendationReason(string recommendation, int docsScore, int diagnosticsScore) =>
        recommendation switch
        {
            "diagnostics-setup" => $"The task is about installing or enabling app-side LuminaUI.Diagnostics. Use the documentation MCP first. docsScore={docsScore}, diagnosticsScore={diagnosticsScore}.",
            "both" => $"任务同时包含文档查询和运行时检查信号。docsScore={docsScore}, diagnosticsScore={diagnosticsScore}.",
            "diagnostics" => $"任务更像运行时应用检查。docsScore={docsScore}, diagnosticsScore={diagnosticsScore}.",
            _ => $"任务更像组件知识或文档查询。docsScore={docsScore}, diagnosticsScore={diagnosticsScore}."
        };

    private static readonly ToolGuide[] DocumentationToolGuide =
    [
        new(
            "lumina_search",
            "搜索 LuminaUI 组件、枚举、示例和设计 token。",
            "不知道准确组件名、想按自然语言查控件、样式、示例或 token 时使用。",
            ["query", "limit"],
            ["查找 loading button", "有没有 pagination 示例", "搜索 border color token"],
            ["lumina_get_component", "lumina_get_example", "lumina_get_tokens"],
            ["components", "examples", "tokens"]),
        new(
            "lumina_list_components",
            "列出 LuminaUI 组件，可按分类过滤。",
            "需要浏览组件清单、确认分类或批量比较组件时使用。",
            ["category", "limit"],
            ["列出 Action 分类组件", "有哪些 Navigation 控件"],
            ["lumina_get_component", "lumina_search"],
            ["components"]),
        new(
            "lumina_get_component",
            "获取单个组件的命名空间、属性、样式类、资源和 XAML 用法提示。",
            "已经知道组件名，想写 XAML 或确认属性/样式类时使用。",
            ["name"],
            ["解释 LuminaCard", "LuminaPagination 有哪些属性"],
            ["lumina_get_example", "lumina_get_tokens"],
            ["components"]),
        new(
            "lumina_get_example",
            "获取 showcase 示例源码，包括 AXAML、code-behind 和 ViewModel。",
            "需要可复制的真实用法、绑定示例或组合方式时使用。",
            ["name"],
            ["获取 ButtonShowcasePage 示例", "查 LuminaCard 示例"],
            ["lumina_get_component", "lumina_search"],
            ["examples"]),
        new(
            "lumina_get_tokens",
            "查看颜色、画刷、阴影、排版、尺寸、形状和间距等设计 token。",
            "做主题、样式、颜色、边框、表面或暗色模式适配时使用。",
            ["category", "query"],
            ["查 Primary token", "有哪些 Brush token", "搜索 border token"],
            ["lumina_search", "lumina_get_component"],
            ["tokens"]),
        new(
            "lumina_get_installation",
            "获取 LuminaUI 包安装和 App.axaml 接入说明。",
            "新项目接入、确认可选包或排查基础安装方式时使用。",
            ["package"],
            ["如何安装 LuminaUI", "DataGrid 包怎么接入"],
            ["lumina_get_diagnostics_setup", "lumina_catalog_status"],
            ["installation"]),
        new(
            "lumina_get_diagnostics_setup",
            "Explains how an AI agent should add LuminaUI.Diagnostics to a target Avalonia app.",
            "Use this when diagnostics MCP is already installed but the target app still needs package/reference and Program.cs changes.",
            ["project"],
            ["install LuminaUI.Diagnostics into my app", "enable diagnostics for an Avalonia desktop app"],
            ["lumina_get_installation", "lumina_find_mcp"],
            ["installation", "diagnostics", "diagnostics-setup"]),
        new(
            "lumina_catalog_status",
            "查看当前 catalog 的版本、commit、生成时间和组件数量。",
            "怀疑 catalog 为空、过期或查询结果不符合预期时使用。",
            [],
            ["catalog 是否 ready", "当前索引版本是什么"],
            ["lumina_search"],
            ["status"]),
        new(
            "lumina_list_mcp_tools",
            "列出并解释 LuminaUI 文档 MCP 和 diagnostics MCP 的工具。",
            "需要理解有哪些 MCP tools、参数、使用场景和工具边界时使用。",
            ["area"],
            ["解释所有 LuminaUI MCP tools", "只看 diagnostics tools"],
            ["lumina_find_mcp"],
            ["status"]),
        new(
            "lumina_find_mcp",
            "根据任务描述推荐使用文档 MCP、diagnostics MCP 或两者组合。",
            "不知道应该查文档、查 catalog，还是连接运行中的应用做诊断时使用。",
            ["task", "includeWorkflow"],
            ["我要查按钮示例该用哪个 MCP", "我要看运行中窗口的控件树该用哪个 MCP"],
            ["lumina_list_mcp_tools"],
            ["status"])
    ];

    private static readonly object[] DiagnosticsToolGuide =
    [
        new
        {
            Name = "discover_apps",
            Purpose = "说明如何定位启用 LuminaUI diagnostics 的运行中应用。",
            UseWhen = "不知道 pipe 名，或需要确认默认命名规则时使用。",
            KeyParameters = Array.Empty<string>(),
            ExamplePrompts = new[] { "发现 diagnostics app", "如何连接正在运行的 LuminaUI 应用" }
        },
        new
        {
            Name = "list_windows",
            Purpose = "列出目标应用窗口。",
            UseWhen = "连接进程后第一步确认窗口数量、标题、大小和状态。",
            KeyParameters = new[] { "pid", "pipe", "timeoutMs" },
            ExamplePrompts = new[] { "列出 pid 1234 的窗口", "当前应用有没有主窗口" }
        },
        new
        {
            Name = "get_visual_tree / get_logical_tree / find_control",
            Purpose = "读取控件树或搜索控件。",
            UseWhen = "需要定位控件、检查模板生成元素、确认可见性和 bounds 时使用。",
            KeyParameters = new[] { "pid", "pipe", "windowIndex", "controlId", "name", "typeName", "text" },
            ExamplePrompts = new[] { "查找文本为 Save 的按钮", "读取主窗口 visual tree" }
        },
        new
        {
            Name = "get_control_properties / get_data_context / get_binding_errors / get_applied_styles / get_resources",
            Purpose = "读取运行时属性、DataContext、绑定错误、样式和资源。",
            UseWhen = "排查绑定、样式、DataContext、资源解析或属性状态不符合预期时使用。",
            KeyParameters = new[] { "pid", "pipe", "controlId", "propertyNames" },
            ExamplePrompts = new[] { "检查按钮 IsEnabled", "查看绑定错误", "读取控件 DataContext" }
        },
        new
        {
            Name = "take_screenshot",
            Purpose = "截取窗口或控件截图。",
            UseWhen = "需要视觉确认、排查布局错位或截图回传给客户端时使用。",
            KeyParameters = new[] { "pid", "pipe", "windowIndex", "controlId" },
            ExamplePrompts = new[] { "截取主窗口", "截取某个控件截图" }
        },
        new
        {
            Name = "click_control / input_text / set_property / invoke_command / wait_for_property / scroll",
            Purpose = "执行基础交互和状态等待。",
            UseWhen = "需要点击、输入、调用命令、滚动、设置调试属性或等待异步 UI 状态时使用。",
            KeyParameters = new[] { "pid", "pipe", "controlId", "text", "propertyName", "value", "commandName" },
            ExamplePrompts = new[] { "点击保存按钮", "输入搜索词", "等待 IsBusy=false" }
        }
    ];

    private static readonly string[] DocumentationKeywords =
    [
        "component",
        "组件",
        "控件",
        "example",
        "示例",
        "xaml",
        "axaml",
        "token",
        "主题",
        "样式",
        "install",
        "安装",
        "接入",
        "package",
        "属性",
        "usage",
        "用法",
        "docs",
        "文档"
    ];

    private static readonly string[] DiagnosticsSetupKeywords =
    [
        "install",
        "installation",
        "setup",
        "enable",
        "add package",
        "reference",
        "program.cs",
        "appbuilder",
        "UseLuminaUIDiagnostics",
        "安装",
        "接入",
        "启用",
        "引用",
        "配置",
        "怎么装",
        "如何装"
    ];

    private static readonly string[] DiagnosticsKeywords =
    [
        "running",
        "live",
        "运行中",
        "窗口",
        "window",
        "visual tree",
        "logical tree",
        "控件树",
        "binding",
        "绑定",
        "screenshot",
        "截图",
        "click",
        "点击",
        "input",
        "输入",
        "data context",
        "datacontext",
        "样式没生效",
        "属性不对",
        "调试",
        "diagnostics",
        "诊断",
        "pipe",
        "pid"
    ];

    private sealed record ToolGuide(
        string Name,
        string Purpose,
        string UseWhen,
        string[] KeyParameters,
        string[] ExamplePrompts,
        string[] RelatedTools,
        string[] Areas);

    private sealed record ComponentUsageGuide(
        string Source,
        string? Xaml,
        string? CSharp,
        string[] Notes,
        string[] RelatedTools);
}
