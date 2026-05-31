using System.Text.RegularExpressions;
using System.Xml.Linq;
using LuminaUI.Mcp.Models;

namespace LuminaUI.Mcp.Indexing;

public sealed class AxamlExtractor
{
    private static readonly XNamespace AvaloniaNs = "https://github.com/avaloniaui";
    private static readonly XNamespace XNs = "http://schemas.microsoft.com/winfx/2006/xaml";

    public List<string> ExtractStyleClasses(string axamlContent)
    {
        var classes = new List<string>();
        var matches = Regex.Matches(axamlContent, @"Selector=""[^""]*\.([\w]+)""");
        foreach (Match m in matches)
        {
            if (m.Groups[1].Success)
                classes.Add(m.Groups[1].Value);
        }
        return classes.Distinct().Order().ToList();
    }

    public Dictionary<string, List<string>> ExtractComponentStyles(string axamlContent)
    {
        var result = new Dictionary<string, List<string>>();

        try
        {
            var doc = XDocument.Parse(axamlContent);
            var root = doc.Root;
            if (root is null) return result;

            var ns = root.GetDefaultNamespace();

            foreach (var controlTheme in root.Descendants(ns + "ControlTheme"))
            {
                var targetType = controlTheme.Attribute("TargetType")?.Value ?? "";
                var targetTypeClean = CleanTypeName(targetType);

                if (string.IsNullOrEmpty(targetTypeClean)) continue;

                var classes = new List<string>();
                foreach (var style in controlTheme.Descendants(ns + "Style"))
                {
                    var selector = style.Attribute("Selector")?.Value ?? "";
                    var classMatches = Regex.Matches(selector, @"\.([\w]+)");
                    foreach (Match m in classMatches)
                    {
                        if (m.Groups[1].Success)
                            classes.Add(m.Groups[1].Value);
                    }
                }

                if (classes.Count > 0)
                    result[targetTypeClean] = classes.Distinct().Order().ToList();
            }
        }
        catch
        {
            // AXAML may not be valid XML in all cases; extract via regex fallback
            var matches = Regex.Matches(axamlContent, @"TargetType=""([^""]+)""");
            foreach (Match m in matches)
            {
                var typeName = CleanTypeName(m.Groups[1].Value);
                if (!string.IsNullOrEmpty(typeName) && !result.ContainsKey(typeName))
                    result[typeName] = [];
            }
        }

        return result;
    }

    public List<DesignToken> ExtractDesignTokens(string filePath, string content)
    {
        var tokens = new List<DesignToken>();

        try
        {
            var doc = XDocument.Parse(content);
            var root = doc.Root;
            if (root is null) return tokens;

            var ns = root.GetDefaultNamespace();
            var isDark = filePath.Contains("Dark.axaml");
            var isLight = filePath.Contains("Light.axaml");
            var isFoundation = filePath.Contains("Foundation.axaml") || filePath.Contains("Colors.axaml");

            if (isDark || isLight || isFoundation)
            {
                foreach (var element in root.Descendants())
                {
                    var key = element.Attribute(XNs + "Key")?.Value;
                    if (string.IsNullOrEmpty(key)) continue;

                    string? value = null;
                    if (element.Name.LocalName == "Color")
                        value = element.Value.Trim();
                    else if (element.Name.LocalName == "SolidColorBrush")
                        value = element.Attribute("Color")?.Value;
                    else if (element.Name.LocalName == "BoxShadow")
                        value = element.Value.Trim();
                    else if (element.Name.LocalName == "FontFamily")
                        value = element.Value.Trim();
                    else if (element.Name.LocalName == "Thickness")
                        value = element.Value.Trim();
                    else if (element.Name.LocalName == "CornerRadius")
                        value = element.Value.Trim();
                    else if (element.Name.LocalName == "Double" || element.Name.LocalName == "FontWeight")
                        value = element.Value.Trim();

                    if (value is not null)
                    {
                        tokens.Add(new DesignToken
                        {
                            Name = key,
                            Category = InferTokenCategory(key),
                            LightValue = isLight || isFoundation ? value : null,
                            DarkValue = isDark ? value : null,
                            Description = DescribeToken(key)
                        });
                    }
                }
            }
        }
        catch
        {
            // Fallback: extract via regex
            var matches = Regex.Matches(content, @"x:Key=""([^"")]+)""[^>]*>([^<]*)<");
            foreach (Match m in matches)
            {
                tokens.Add(new DesignToken
                {
                    Name = m.Groups[1].Value,
                    Category = InferTokenCategory(m.Groups[1].Value),
                    LightValue = filePath.Contains("Light") || filePath.Contains("Foundation") ? m.Groups[2].Value.Trim() : null,
                    DarkValue = filePath.Contains("Dark") ? m.Groups[2].Value.Trim() : null
                });
            }
        }

        return tokens;
    }

    public string? ExtractThemeResourceKey(string axamlContent, string elementName)
    {
        var match = Regex.Match(axamlContent, $@"x:Key=""({elementName}[^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    public List<string> ExtractResourceIncludes(string axamlContent)
    {
        var includes = new List<string>();
        var matches = Regex.Matches(axamlContent, @"Source=""avares://([^""]+)""");
        foreach (Match m in matches)
        {
            if (m.Groups[1].Success)
                includes.Add(m.Groups[1].Value);
        }
        return includes;
    }

    private static string CleanTypeName(string typeName)
    {
        // Handle "lumina:LuminaCard" -> "LuminaCard", "local:LuminaCard" -> "LuminaCard", etc.
        var colonIndex = typeName.LastIndexOf(':');
        var clean = colonIndex >= 0 ? typeName[(colonIndex + 1)..] : typeName;

        // Remove generic type parameters if any
        var genericIndex = clean.IndexOf('{');
        if (genericIndex > 0) clean = clean[..genericIndex];

        // Remove namespace prefix like "local:" or "lumina:"
        var lastColon = clean.LastIndexOf(':');
        if (lastColon >= 0) clean = clean[(lastColon + 1)..];

        return clean.Trim();
    }

    private static string InferTokenCategory(string key)
    {
        return key switch
        {
            var k when k.Contains("Color") && !k.Contains("Brush") => "Color",
            var k when k.Contains("Brush") => "Brush",
            var k when k.Contains("Shadow") => "Shadow",
            var k when k.Contains("Font") || k.Contains("Text") => "Typography",
            var k when k.Contains("Size") || k.Contains("Width") || k.Contains("Height") => "Size",
            var k when k.Contains("Radius") || k.Contains("Corner") => "Shape",
            var k when k.Contains("Spacing") || k.Contains("Margin") || k.Contains("Padding") => "Spacing",
            var k when k.Contains("Surface") => "Surface",
            var k when k.Contains("Border") => "Border",
            var k when k.Contains("Primary") => "Primary",
            var k when k.Contains("Success") => "Semantic",
            var k when k.Contains("Warning") => "Semantic",
            var k when k.Contains("Danger") => "Semantic",
            _ => "Other"
        };
    }

    private static string? DescribeToken(string key)
    {
        return key switch
        {
            "LuminaBackground" => "Application background layer",
            "LuminaSurface" => "Standard card and content area background",
            "LuminaSurfaceElevated" => "Floating elements background (popovers, dropdowns)",
            "LuminaBorder" => "1px dividers and card outlines",
            "LuminaTextForeground" => "Primary text color (headings, body)",
            "LuminaTextMuted" => "Secondary text color (captions, placeholders)",
            "LuminaPrimaryBrush" => "Primary action color (buttons, active states)",
            "LuminaPrimaryForegroundBrush" => "Text on primary-colored elements",
            "LuminaDangerBrush" => "Destructive actions and errors",
            "LuminaSuccessBrush" => "Success messages and confirmations",
            "LuminaWarningBrush" => "Alerts and pending states",
            _ => null
        };
    }
}
