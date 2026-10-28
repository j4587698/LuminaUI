using System.Text.RegularExpressions;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Guards the demo's own localization resources (<c>Sandbox*.resx</c>). The demo is the
/// library's showcase; a missing translation there renders an empty label, nothing throws.
/// </summary>
public partial class DemoLocalizationTests
{
    private static string ResourcesDirectory => Path.Combine(RepoPaths.DemoDirectory, "Localization", "Resources");

    public static TheoryData<string> ResourceSets =>
        [.. Directory
            .EnumerateFiles(ResourcesDirectory, "*.resx")
            .Select(Path.GetFileName)
            .Where(name => name!.Count(character => character == '.') == 1)
            .Select(name => Path.GetFileNameWithoutExtension(name!))
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(ResourceSets))]
    public void CulturesMatch(string baseName)
    {
        Dictionary<string, string> neutral = LocalizationTests.ReadResx(Path.Combine(ResourcesDirectory, baseName + ".resx"));
        Dictionary<string, string> chinese = LocalizationTests.ReadResx(Path.Combine(ResourcesDirectory, baseName + ".zh-CN.resx"));

        List<string> problems = [];
        problems.AddRange(neutral.Keys.Except(chinese.Keys).Order().Select(key => $"{key}: missing from zh-CN"));
        problems.AddRange(chinese.Keys.Except(neutral.Keys).Order().Select(key => $"{key}: only in zh-CN"));

        foreach ((string key, string value) in neutral)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                problems.Add($"{key}: empty in the neutral culture");
            }

            if (!chinese.TryGetValue(key, out string? translated))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(translated))
            {
                problems.Add($"{key}: empty in zh-CN");
            }

            // A format string whose placeholders differ between cultures throws (or drops a
            // value) only when that language is active.
            string expected = string.Join(",", Placeholders(value));
            string actual = string.Join(",", Placeholders(translated));
            if (expected != actual)
            {
                problems.Add($"{key}: placeholders {{{expected}}} vs zh-CN {{{actual}}}");
            }
        }

        Assert.True(problems.Count == 0, $"{baseName}:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void EveryReferencedKeyExists()
    {
        // Demo keys may live in any Sandbox*.resx, or be core library keys.
        HashSet<string> known = new(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(ResourcesDirectory, "*.resx").Where(path => Path.GetFileName(path).Count(character => character == '.') == 1))
        {
            known.UnionWith(LocalizationTests.ReadResx(file).Keys);
        }

        known.UnionWith(LocalizationTests.ReadResx(Path.Combine(RepoPaths.LocalizationDirectory, "Resources", "LuminaUIStrings.resx")).Keys);

        var referenced = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(RepoPaths.DemoDirectory, "*.axaml", SearchOption.AllDirectories))
        {
            foreach (Match match in LocMarkup().Matches(File.ReadAllText(file)))
            {
                referenced.Add(match.Groups["key"].Value);
            }
        }

        Assert.True(referenced.Count > 100, $"Only {referenced.Count} {{l10n:Loc}} references found; the scan is probably broken.");

        // Keys used from code-behind and view models. Read from source so the test project does not
        // have to reference the demo (and all of its dependencies).
        string constants = File.ReadAllText(Path.Combine(RepoPaths.DemoDirectory, "SandboxLocalization.cs"));
        foreach (Match match in ConstantPattern().Matches(constants))
        {
            string value = match.Groups["value"].Value;
            if (!value.Contains('{'))
            {
                referenced.Add(value);
            }
        }

        string[] missing = [.. referenced.Where(key => !known.Contains(key))];
        Assert.True(missing.Length == 0, "Referenced localization keys with no resource entry:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    private static IEnumerable<int> Placeholders(string value) =>
        PlaceholderPattern().Matches(value).Select(match => int.Parse(match.Groups[1].Value)).Distinct().Order();

    [GeneratedRegex(@"(?<!\{)\{(\d+)(?:[,:][^}]*)?\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"const\s+string\s+\w+\s*=\s*""(?<value>[^""]+)""")]
    private static partial Regex ConstantPattern();

    [GeneratedRegex(@"\{l10n:Loc\s+(?:Key=)?(?<key>[\w.]+)")]
    private static partial Regex LocMarkup();
}
