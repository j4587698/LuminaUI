using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Localization;
using LuminaUI.Theming;

namespace LuminaUI.Demo.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private bool _isSyncingLanguage;
    private bool _isSyncingThemeMode;

    public SettingsPageViewModel()
    {
        SyncThemeModeControls();
        SyncAccentControls();
        SyncLanguageControls(resetStatus: true);
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
        LuminaThemeManager.ThemeModeChanged += OnThemeModeChanged;
    }

    [ObservableProperty]
    private string _mobileSettingsStatusText = string.Empty;

    [ObservableProperty]
    private string _accentHexText = string.Empty;

    [ObservableProperty]
    private string _accentStatusText = string.Empty;

    [ObservableProperty]
    private IBrush? _accentPreviewBrush;

    [ObservableProperty]
    private string[] _languageItems = [];

    [ObservableProperty]
    private int _selectedLanguageIndex;

    [ObservableProperty]
    private string[] _themeModeItems = [];

    [ObservableProperty]
    private int _selectedThemeModeIndex;

    [ObservableProperty]
    private bool? _mobileNotificationsEnabled = true;

    [ObservableProperty]
    private string _nicknameText = "Lumina Admin";

    [ObservableProperty]
    private string _storageLimitText = "2.4 GB";

    [RelayCommand]
    private void ToggleTheme()
    {
        LuminaThemeManager.ToggleThemeVariant();
        SandboxWindowActions.RefreshThemeAndWindowMaterial();
    }

    [RelayCommand]
    private void ApplyAccent()
    {
        ApplyAccentColor(AccentHexText);
    }

    [RelayCommand]
    private void ApplyAccentPreset(string? accent)
    {
        ApplyAccentColor(accent);
    }

    [RelayCommand]
    private void SelectMobileSetting(object? header)
    {
        MobileSettingsStatusText = LuminaLocalization.Format(SandboxLocalization.SettingsTappedFormat, header);
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        if (_isSyncingLanguage)
        {
            return;
        }

        var cultures = LuminaLocalization.SupportedCultures;
        if (value < 0 || value >= cultures.Count)
        {
            return;
        }

        LuminaLocalization.SetCulture(cultures[value]);
    }

    partial void OnSelectedThemeModeIndexChanged(int value)
    {
        if (_isSyncingThemeMode)
        {
            return;
        }

        LuminaThemeManager.SetThemeMode(value switch
        {
            1 => LuminaThemeMode.Light,
            2 => LuminaThemeMode.Dark,
            _ => LuminaThemeMode.System
        });
        SandboxWindowActions.RefreshThemeAndWindowMaterial();
    }

    private void OnLanguageChanged(object? sender, System.EventArgs e)
    {
        SyncThemeModeControls();
        SyncLanguageControls(resetStatus: true);
    }

    private void OnThemeModeChanged(object? sender, System.EventArgs e)
    {
        SyncThemeModeControls();
    }

    private void ApplyAccentColor(string? value)
    {
        if (!LuminaThemeManager.TrySetAccentColor(value))
        {
            AccentStatusText = LuminaLocalization.Get(SandboxLocalization.SettingsInvalidColor);
            return;
        }

        SyncAccentControls();
        SandboxWindowActions.RefreshThemeAndWindowMaterial();
    }

    private void SyncAccentControls()
    {
        var accent = LuminaThemeManager.CurrentAccentColor;
        var hex = LuminaThemeManager.ToHex(accent);

        AccentHexText = hex;
        AccentStatusText = hex;
        AccentPreviewBrush = new SolidColorBrush(accent);
    }

    private void SyncThemeModeControls()
    {
        _isSyncingThemeMode = true;
        ThemeModeItems =
        [
            LuminaLocalization.Get(SandboxLocalization.SettingsThemeModeSystem),
            LuminaLocalization.Get(SandboxLocalization.SettingsThemeModeLight),
            LuminaLocalization.Get(SandboxLocalization.SettingsThemeModeDark)
        ];
        SelectedThemeModeIndex = LuminaThemeManager.CurrentThemeMode switch
        {
            LuminaThemeMode.Light => 1,
            LuminaThemeMode.Dark => 2,
            _ => 0
        };
        _isSyncingThemeMode = false;
    }

    private void SyncLanguageControls(bool resetStatus)
    {
        _isSyncingLanguage = true;
        var cultures = LuminaLocalization.SupportedCultures;
        LanguageItems = cultures.Select(GetCultureDisplayName).ToArray();
        SelectedLanguageIndex = FindCultureIndex(cultures, LuminaLocalization.CurrentCulture);
        _isSyncingLanguage = false;

        if (resetStatus)
        {
            MobileSettingsStatusText = LuminaLocalization.Get(SandboxLocalization.SettingsTapHint);
        }
    }

    private static string GetCultureDisplayName(CultureInfo culture)
    {
        return culture.Name switch
        {
            "en-US" => LuminaLocalization.Get(SandboxLocalization.LanguageEnglish),
            "zh-CN" => LuminaLocalization.Get(SandboxLocalization.LanguageChinese),
            _ => culture.NativeName
        };
    }

    private static int FindCultureIndex(IReadOnlyList<CultureInfo> cultures, CultureInfo culture)
    {
        for (var i = 0; i < cultures.Count; i++)
        {
            if (cultures[i].Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        for (var i = 0; i < cultures.Count; i++)
        {
            if (cultures[i].TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }
}
