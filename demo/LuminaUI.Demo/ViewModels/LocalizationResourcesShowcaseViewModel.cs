using System.Collections;
using System.Globalization;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class LocalizationResourcesShowcaseViewModel : ObservableObject
{
    private const string CoreResourceBaseName = "LuminaUI.Localization.Resources.LuminaUIStrings";

    private static readonly ResourceManager CoreResourceManager =
        new(CoreResourceBaseName, typeof(LuminaLocalization).Assembly);

    private readonly Func<string, Task> _copyToClipboardAsync;
    private readonly IAsyncRelayCommand<string?> _copyKeyCommand;

    public LocalizationResourcesShowcaseViewModel()
        : this(_ => Task.CompletedTask)
    {
    }

    public LocalizationResourcesShowcaseViewModel(Func<string, Task> copyToClipboardAsync)
    {
        _copyToClipboardAsync = copyToClipboardAsync;
        _copyKeyCommand = new AsyncRelayCommand<string?>(CopyKeyAsync);
        Refresh();
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private string _keyCountText = string.Empty;

    [ObservableProperty]
    private string _copyStatus = string.Empty;

    [ObservableProperty]
    private bool _hasCopyStatus;

    [ObservableProperty]
    private string _overrideSnippetTitle = string.Empty;

    [ObservableProperty]
    private string _overrideSnippetDescription = string.Empty;

    [ObservableProperty]
    private string _overrideSnippet = string.Empty;

    [ObservableProperty]
    private string _groupHeader = string.Empty;

    [ObservableProperty]
    private string _keyHeader = string.Empty;

    [ObservableProperty]
    private string _englishHeader = string.Empty;

    [ObservableProperty]
    private string _chineseHeader = string.Empty;

    [ObservableProperty]
    private string _currentHeader = string.Empty;

    [ObservableProperty]
    private string _statusHeader = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<LocalizationResourceRow> _rows = [];

    partial void OnCopyStatusChanged(string value)
    {
        HasCopyStatus = !string.IsNullOrWhiteSpace(value);
    }

    private async Task CopyKeyAsync(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        await _copyToClipboardAsync(key);
        CopyStatus = LuminaLocalization.Format(SandboxLocalization.LocalizationResourcesCopiedFormat, key);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var rows = CreateRows();

        Title = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesTitle);
        Subtitle = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesDescription);
        KeyCountText = LuminaLocalization.Format(SandboxLocalization.LocalizationResourcesCountFormat, rows.Count);
        OverrideSnippetTitle = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesSnippetTitle);
        OverrideSnippetDescription = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesSnippetDescription);
        GroupHeader = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesHeaderGroup);
        KeyHeader = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesHeaderKey);
        EnglishHeader = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesHeaderEnglish);
        ChineseHeader = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesHeaderChinese);
        CurrentHeader = LuminaLocalization.Format(
            SandboxLocalization.LocalizationResourcesHeaderCurrentFormat,
            LuminaLocalization.CurrentCulture.Name);
        StatusHeader = LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesHeaderStatus);
        OverrideSnippet = """
                          <data name="Lumina.Common.Done" xml:space="preserve">
                            <value>Done (App Override)</value>
                          </data>
                          """;
        Rows = rows;
    }

    private IReadOnlyList<LocalizationResourceRow> CreateRows()
    {
        var rows = new List<LocalizationResourceRow>();
        foreach (var key in EnumerateCoreResourceKeys())
        {
            var neutralValue = GetCoreValue(key, CultureInfo.GetCultureInfo("en-US"));
            var chineseValue = GetCoreValue(key, CultureInfo.GetCultureInfo("zh-CN"));
            var currentCoreValue = GetCoreValue(key, LuminaLocalization.CurrentCulture);
            var effectiveValue = LuminaLocalization.Get(key);
            var isOverridden = !string.Equals(currentCoreValue, effectiveValue, StringComparison.Ordinal);

            rows.Add(new LocalizationResourceRow(
                GetGroupName(key),
                key,
                neutralValue,
                chineseValue,
                effectiveValue,
                isOverridden,
                isOverridden
                    ? LuminaLocalization.Get(SandboxLocalization.LocalizationResourcesOverridden)
                    : string.Empty,
                _copyKeyCommand));
        }

        return rows
            .OrderBy(row => row.Group, StringComparer.Ordinal)
            .ThenBy(row => row.Key, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateCoreResourceKeys()
    {
        var resourceSet = CoreResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture,
            createIfNotExists: true,
            tryParents: true);

        if (resourceSet == null)
        {
            yield break;
        }

        foreach (DictionaryEntry entry in resourceSet)
        {
            if (entry.Key is string key)
            {
                yield return key;
            }
        }
    }

    private static string GetCoreValue(string key, CultureInfo culture)
    {
        return CoreResourceManager.GetString(key, culture) ?? string.Empty;
    }

    private static string GetGroupName(string key)
    {
        var parts = key.Split('.');
        return parts.Length > 1 ? parts[1] : key;
    }
}

public sealed record LocalizationResourceRow(
    string Group,
    string Key,
    string EnglishValue,
    string ChineseValue,
    string EffectiveValue,
    bool IsOverridden,
    string OverrideStatus,
    ICommand CopyKeyCommand);
