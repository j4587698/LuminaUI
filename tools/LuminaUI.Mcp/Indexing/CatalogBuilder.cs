using System.Diagnostics;
using LuminaUI.Mcp.Models;

namespace LuminaUI.Mcp.Indexing;

public sealed class CatalogBuilder
{
    private readonly string _repositoryRoot;
    private readonly CSharpExtractor _csharpExtractor = new();
    private readonly AxamlExtractor _axamlExtractor = new();
    private readonly ShowcaseMapper _showcaseMapper = new();
    private readonly ILogger<CatalogBuilder> _logger;

    public CatalogBuilder(string repositoryRoot, ILogger<CatalogBuilder> logger)
    {
        _repositoryRoot = repositoryRoot;
        _logger = logger;
    }

    public CatalogData Build()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Starting catalog build from {Root}", _repositoryRoot);

        var catalog = new CatalogData
        {
            LibraryVersion = ReadLibraryVersion()
        };

        // Extract components from C# source files
        var srcPath = Path.Combine(_repositoryRoot, "src");
        if (Directory.Exists(srcPath))
        {
            ExtractFromDirectory(srcPath, catalog);
        }

        // Extract examples from demo
        var demoViewsPath = Path.Combine(_repositoryRoot, "demo", "LuminaUI.Demo", "Views");
        if (Directory.Exists(demoViewsPath))
        {
            _logger.LogInformation("Building examples from {Path}", demoViewsPath);
            foreach (var missingFile in _showcaseMapper.FindMissingFiles(demoViewsPath))
                _logger.LogWarning("Showcase mapping references missing file: {File}", missingFile);

            catalog.Examples = _showcaseMapper.BuildExamples(demoViewsPath);
            _logger.LogInformation("Found {Count} examples", catalog.Examples.Count);
        }

        // Merge duplicate components (same name from different assemblies)
        catalog.Components = MergeComponents(catalog.Components);
        catalog.Enums = MergeEnums(catalog.Enums);
        catalog.Examples = MergeExamples(catalog.Examples);

        // Enrich components with style classes from theme files
        EnrichFromThemes(srcPath, catalog);

        // Extract design tokens
        ExtractDesignTokens(srcPath, catalog);

        sw.Stop();
        _logger.LogInformation("Catalog build complete: {Components} components, {Enums} enums, {Examples} examples, {Tokens} tokens in {Elapsed}ms",
            catalog.Components.Count, catalog.Enums.Count, catalog.Examples.Count, catalog.DesignTokens.Count, sw.ElapsedMilliseconds);

        return catalog;
    }

    private void ExtractFromDirectory(string directory, CatalogData catalog)
    {
        var csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
        _logger.LogInformation("Scanning {Count} C# files in {Dir}", csFiles.Length, directory);

        foreach (var file in csFiles)
        {
            // Skip generated files, obj, bin directories
            if (file.Contains(Path.Combine("obj", "")) || file.Contains(Path.Combine("bin", "")))
                continue;

            try
            {
                var content = File.ReadAllText(file);

                var components = _csharpExtractor.ExtractComponents(file, content);
                catalog.Components.AddRange(components);

                var enums = _csharpExtractor.ExtractEnums(file, content);
                catalog.Enums.AddRange(enums);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to extract from {File}: {Error}", file, ex.Message);
            }
        }

        _logger.LogInformation("Found {Components} components, {Enums} enums", catalog.Components.Count, catalog.Enums.Count);
    }

    private void EnrichFromThemes(string srcPath, CatalogData catalog)
    {
        var themeFiles = Directory.GetFiles(srcPath, "*.axaml", SearchOption.AllDirectories)
            .Where(f => f.Contains(Path.Combine("Themes", "")) || f.Contains("Theme.axaml"))
            .ToArray();

        _logger.LogInformation("Enriching from {Count} theme files", themeFiles.Length);

        var componentLookup = catalog.Components.ToDictionary(c => c.Name, c => c);

        foreach (var file in themeFiles)
        {
            if (file.Contains(Path.Combine("obj", "")) || file.Contains(Path.Combine("bin", "")))
                continue;

            try
            {
                var content = File.ReadAllText(file);
                var stylesMap = _axamlExtractor.ExtractComponentStyles(content);

                foreach (var (typeName, classes) in stylesMap)
                {
                    if (componentLookup.TryGetValue(typeName, out var component))
                    {
                        foreach (var cls in classes)
                        {
                            if (!component.StyleClasses.Contains(cls))
                                component.StyleClasses.Add(cls);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to enrich from {File}: {Error}", file, ex.Message);
            }
        }
    }

    private void ExtractDesignTokens(string srcPath, CatalogData catalog)
    {
        var tokenFiles = new[]
        {
            Path.Combine(srcPath, "LuminaUI", "Themes", "Light.axaml"),
            Path.Combine(srcPath, "LuminaUI", "Themes", "Dark.axaml"),
            Path.Combine(srcPath, "LuminaUI", "Themes", "Foundation.axaml"),
            Path.Combine(srcPath, "LuminaUI", "Themes", "Colors.axaml"),
        };

        var tokensByName = new Dictionary<string, DesignToken>();

        foreach (var file in tokenFiles)
        {
            if (!File.Exists(file)) continue;

            try
            {
                var content = File.ReadAllText(file);
                var tokens = _axamlExtractor.ExtractDesignTokens(file, content);

                foreach (var token in tokens)
                {
                    if (tokensByName.TryGetValue(token.Name, out var existing))
                    {
                        if (token.LightValue is not null) existing.LightValue = token.LightValue;
                        if (token.DarkValue is not null) existing.DarkValue = token.DarkValue;
                        if (token.Description is not null) existing.Description = token.Description;
                    }
                    else
                    {
                        tokensByName[token.Name] = token;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to extract tokens from {File}: {Error}", file, ex.Message);
            }
        }

        catalog.DesignTokens = tokensByName.Values.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
        _logger.LogInformation("Found {Count} design tokens", catalog.DesignTokens.Count);
    }

    private static List<ComponentInfo> MergeComponents(List<ComponentInfo> components)
    {
        var merged = new Dictionary<string, ComponentInfo>();

        foreach (var comp in components)
        {
            if (merged.TryGetValue(comp.Name, out var existing))
            {
                // Merge properties
                foreach (var prop in comp.Properties)
                {
                    if (!existing.Properties.Any(p => p.Name == prop.Name))
                        existing.Properties.Add(prop);
                }

                // Merge style classes
                foreach (var cls in comp.StyleClasses)
                {
                    if (!existing.StyleClasses.Contains(cls))
                        existing.StyleClasses.Add(cls);
                }

                // Prefer non-empty values
                if (comp.Description is not null && existing.Description is null)
                    existing.Description = comp.Description;
                if (comp.Category is not null && existing.Category is null)
                    existing.Category = comp.Category;
            }
            else
            {
                merged[comp.Name] = comp;
            }
        }

        return merged.Values.OrderBy(c => c.Name).ToList();
    }

    private static List<EnumInfo> MergeEnums(List<EnumInfo> enums)
    {
        var merged = new Dictionary<string, EnumInfo>();
        foreach (var e in enums)
        {
            if (!merged.ContainsKey(e.Name))
                merged[e.Name] = e;
        }
        return merged.Values.OrderBy(e => e.Name).ToList();
    }

    private static List<ExampleInfo> MergeExamples(List<ExampleInfo> examples)
    {
        var merged = new Dictionary<string, ExampleInfo>();
        foreach (var ex in examples)
        {
            var key = $"{ex.ComponentName}|{ex.ShowcasePage}";
            if (!merged.ContainsKey(key))
                merged[key] = ex;
        }
        return merged.Values.ToList();
    }

    private string ReadLibraryVersion()
    {
        try
        {
            var propsPath = Path.Combine(_repositoryRoot, "Directory.Build.props");
            if (File.Exists(propsPath))
            {
                var content = File.ReadAllText(propsPath);
                var match = System.Text.RegularExpressions.Regex.Match(content, @"<LuminaUIVersion>([^<]+)</LuminaUIVersion>");
                if (match.Success) return match.Groups[1].Value;
            }
        }
        catch { }
        return "unknown";
    }
}
