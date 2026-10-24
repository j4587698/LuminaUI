using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Static contracts over the XAML sources. These never instantiate Avalonia, so they are fast and
/// they fail with a precise file/line, which is what makes a broken resource key cheap to fix.
/// </summary>
public class ResourceContractTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly Regex ResourceReference = new(
        @"\{(?:Dynamic|Static)Resource\s+(?<key>[A-Za-z_][A-Za-z0-9_.]*)",
        RegexOptions.Compiled);

    private static readonly Regex ThemeFileInclude = new(
        @"avares://LuminaUI/Themes/(?<file>[A-Za-z0-9_.]+\.axaml)",
        RegexOptions.Compiled);

    /// <summary>
    /// Legacy public entry points kept for consumers that reference them directly. They are not
    /// mounted by <c>LuminaTheme.axaml</c> on purpose, so the orphan check must not flag them.
    /// </summary>
    private static readonly string[] LegacyEntryPoints =
    [
        "Colors.axaml",
        "Index.axaml",
        "TopView.axaml",
    ];

    [Fact]
    public void EveryReferencedLuminaKeyIsDefined()
    {
        Dictionary<string, List<string>> defined = CollectDefinitions(RepoPaths.CoreThemeDirectory);
        List<string> missing = [];

        foreach ((string key, string location) in CollectReferences(RepoPaths.CoreThemeDirectory, "Lumina"))
        {
            if (!defined.ContainsKey(key))
            {
                missing.Add($"{key} referenced at {location}");
            }
        }

        Assert.True(
            missing.Count == 0,
            "These Lumina resource keys are used in XAML but never defined. A DynamicResource that "
            + "cannot resolve is silent at runtime — the setter simply does nothing:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, missing.Distinct()));
    }

    [Fact]
    public void LightAndDarkDefineTheSameKeys()
    {
        string themeDirectory = Path.Combine(RepoPaths.CoreThemeDirectory, "Themes");

        HashSet<string> light = CollectDefinitionsOf(Path.Combine(themeDirectory, "Light.axaml"));
        HashSet<string> dark = CollectDefinitionsOf(Path.Combine(themeDirectory, "Dark.axaml"));

        string[] onlyInLight = [.. light.Except(dark).Order()];
        string[] onlyInDark = [.. dark.Except(light).Order()];

        Assert.True(
            onlyInLight.Length == 0 && onlyInDark.Length == 0,
            "Light.axaml and Dark.axaml must declare the same key set, otherwise a control looks "
            + "right in one theme variant and silently loses its styling in the other."
            + Environment.NewLine
            + $"Only in Light.axaml: {string.Join(", ", onlyInLight)}"
            + Environment.NewLine
            + $"Only in Dark.axaml: {string.Join(", ", onlyInDark)}");
    }

    [Fact]
    public void EveryThemeFileIsMountedByLuminaTheme()
    {
        string themeDirectory = Path.Combine(RepoPaths.CoreThemeDirectory, "Themes");
        string themeEntryPoint = Path.Combine(RepoPaths.CoreThemeDirectory, "LuminaTheme.axaml");

        HashSet<string> mounted =
        [
            .. ThemeFileInclude
                .Matches(File.ReadAllText(themeEntryPoint))
                .Select(match => match.Groups["file"].Value),
        ];

        string[] orphans =
        [
            .. Directory
                .GetFiles(themeDirectory, "*.axaml")
                .Select(Path.GetFileName)
                .Where(name => name is not null)
                .Select(name => name!)
                .Where(name => !mounted.Contains(name))
                .Where(name => !LegacyEntryPoints.Contains(name))
                .Order(),
        ];

        Assert.True(
            orphans.Length == 0,
            "These theme files exist under Themes/ but LuminaTheme.axaml never includes them, so "
            + "none of their control themes are ever applied:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, orphans));
    }

    private static HashSet<string> CollectDefinitionsOf(string file)
    {
        HashSet<string> keys = [];

        foreach (XElement element in Load(file).Descendants())
        {
            XAttribute? key = element.Attribute(Xaml + "Key");

            if (key is not null && !key.Value.StartsWith('{'))
            {
                keys.Add(key.Value);
            }
        }

        return keys;
    }

    private static Dictionary<string, List<string>> CollectDefinitions(string root)
    {
        Dictionary<string, List<string>> definitions = new(StringComparer.Ordinal);

        foreach (string file in Directory.GetFiles(root, "*.axaml", SearchOption.AllDirectories))
        {
            foreach (XElement element in Load(file).Descendants())
            {
                XAttribute? key = element.Attribute(Xaml + "Key");

                if (key is null || key.Value.StartsWith('{'))
                {
                    continue;
                }

                if (!definitions.TryGetValue(key.Value, out List<string>? locations))
                {
                    locations = [];
                    definitions[key.Value] = locations;
                }

                locations.Add(Describe(file, key));
            }
        }

        return definitions;
    }

    private static IEnumerable<(string Key, string Location)> CollectReferences(string root, string prefix)
    {
        foreach (string file in Directory.GetFiles(root, "*.axaml", SearchOption.AllDirectories))
        {
            foreach (XElement element in Load(file).Descendants())
            {
                foreach (XAttribute attribute in element.Attributes())
                {
                    foreach (Match match in ResourceReference.Matches(attribute.Value))
                    {
                        string key = match.Groups["key"].Value;

                        if (key.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            yield return (key, Describe(file, attribute));
                        }
                    }
                }
            }
        }
    }

    private static XDocument Load(string file)
    {
        using FileStream stream = File.OpenRead(file);

        return XDocument.Load(stream, LoadOptions.SetLineInfo);
    }

    private static string Describe(string file, XObject node)
    {
        int line = node is IXmlLineInfo info ? info.LineNumber : 0;
        string relative = Path.GetRelativePath(RepoPaths.Root, file).Replace('\\', '/');

        return $"{relative}:{line}";
    }
}
