using System.Reflection;
using System.Xml.Linq;
using LuminaUI.Localization;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Guards the localized strings that ship inside the control templates. A missing entry does not
/// throw — the text just renders empty — so it has to be caught here.
/// </summary>
public class LocalizationTests
{
    private const string NeutralCulture = "LuminaUIStrings.resx";
    private const string ChineseCulture = "LuminaUIStrings.zh-CN.resx";

    [Fact]
    public void EveryKeyConstantExistsInEveryCulture()
    {
        string directory = Path.Combine(RepoPaths.LocalizationDirectory, "Resources");

        Dictionary<string, string> neutral = ReadResx(Path.Combine(directory, NeutralCulture));
        Dictionary<string, string> chinese = ReadResx(Path.Combine(directory, ChineseCulture));

        List<string> problems = [];

        foreach (string key in DeclaredKeys())
        {
            if (!neutral.TryGetValue(key, out string? neutralValue))
            {
                problems.Add($"{key} missing from {NeutralCulture}");
            }
            else if (string.IsNullOrWhiteSpace(neutralValue))
            {
                problems.Add($"{key} is empty in {NeutralCulture}");
            }

            if (!chinese.TryGetValue(key, out string? chineseValue))
            {
                problems.Add($"{key} missing from {ChineseCulture}");
            }
            else if (string.IsNullOrWhiteSpace(chineseValue))
            {
                problems.Add($"{key} is empty in {ChineseCulture}");
            }
        }

        Assert.True(
            problems.Count == 0,
            "Every key declared on LuminaLocalizationKeys must have a non-empty translation in every "
            + "shipped culture:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void CulturesDeclareTheSameKeys()
    {
        string directory = Path.Combine(RepoPaths.LocalizationDirectory, "Resources");

        HashSet<string> neutral = [.. ReadResx(Path.Combine(directory, NeutralCulture)).Keys];
        HashSet<string> chinese = [.. ReadResx(Path.Combine(directory, ChineseCulture)).Keys];

        string[] onlyInNeutral = [.. neutral.Except(chinese).Order()];
        string[] onlyInChinese = [.. chinese.Except(neutral).Order()];

        Assert.True(
            onlyInNeutral.Length == 0 && onlyInChinese.Length == 0,
            "The culture files drifted apart."
            + Environment.NewLine
            + $"Only in {NeutralCulture}: {string.Join(", ", onlyInNeutral)}"
            + Environment.NewLine
            + $"Only in {ChineseCulture}: {string.Join(", ", onlyInChinese)}");
    }

    private static IEnumerable<string> DeclaredKeys() =>
        typeof(LuminaLocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true } && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);

    private static Dictionary<string, string> ReadResx(string path)
    {
        // Direct children only: the embedded xsd schema also contains <data>-named elements.
        return XDocument
            .Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }
}
