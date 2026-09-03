using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace LuminaUI.Theming;

public static class LuminaThemeManager
{
    private static readonly Color LightText = Color.Parse("#FAFAFA");
    private static readonly Color DarkText = Color.Parse("#09090B");
    private static Application? _subscribedApplication;

    public static Color CurrentAccentColor { get; private set; } = Color.Parse("#2563EB");
    public static LuminaThemeMode CurrentThemeMode { get; private set; } = LuminaThemeMode.System;
    public static bool IsDarkThemeActive => Application.Current != null && IsDarkTheme(Application.Current);

    public static IReadOnlyList<LuminaThemeAccentPreset> AccentPresets { get; } =
    [
        new("Blue", Color.Parse("#2563EB")),
        new("Indigo", Color.Parse("#4F46E5")),
        new("Violet", Color.Parse("#7C3AED")),
        new("Teal", Color.Parse("#0D9488")),
        new("Emerald", Color.Parse("#16A34A")),
        new("Rose", Color.Parse("#E11D48")),
        new("Orange", Color.Parse("#EA580C"))
    ];

    public static event EventHandler? ThemeModeChanged;

    public static void SetAccentColor(Color accentColor)
    {
        CurrentAccentColor = WithFullAlpha(accentColor);
        Refresh();
    }

    public static bool TrySetAccentColor(string? value)
    {
        if (!TryParseHexColor(value, out var color))
        {
            return false;
        }

        SetAccentColor(color);
        return true;
    }

    public static void ToggleThemeVariant()
    {
        if (Application.Current == null)
        {
            return;
        }

        SetThemeMode(IsDarkTheme(Application.Current)
            ? LuminaThemeMode.Light
            : LuminaThemeMode.Dark);
    }

    public static void SetThemeMode(LuminaThemeMode themeMode)
    {
        CurrentThemeMode = themeMode;
        ApplyRequestedThemeVariant();
        Refresh();
        ThemeModeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Initialize(Application? application = null)
    {
        var app = application ?? Application.Current;
        if (app == null || ReferenceEquals(_subscribedApplication, app))
        {
            return;
        }

        if (_subscribedApplication != null)
        {
            _subscribedApplication.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        }

        _subscribedApplication = app;
        _subscribedApplication.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        ApplyRequestedThemeVariant();
        Refresh();
    }

    private static void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (CurrentThemeMode == LuminaThemeMode.System)
        {
            Refresh();
        }
    }

    private static void ApplyRequestedThemeVariant()
    {
        if (Application.Current == null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = CurrentThemeMode switch
        {
            LuminaThemeMode.Light => ThemeVariant.Light,
            LuminaThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    public static void Refresh()
    {
        if (Application.Current == null)
        {
            return;
        }

        var isDark = IsDarkTheme(Application.Current);
        var palette = CreateAccentPalette(CurrentAccentColor, isDark);
        ApplyPalette(Application.Current.Resources, palette);
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static bool TryParseHexColor(string? value, out Color color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();
        if (text.StartsWith('#'))
        {
            text = text[1..];
        }

        if (text.Length != 6 && text.Length != 8)
        {
            return false;
        }

        try
        {
            color = WithFullAlpha(Color.Parse($"#{text}"));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static LuminaAccentPalette CreateAccentPalette(Color accentColor, bool isDark)
    {
        var accent = WithFullAlpha(accentColor);
        if (isDark)
        {
            return new LuminaAccentPalette(
                Primary: Lighten(accent, 0.10),
                PrimaryHover: accent,
                PrimaryPressed: Darken(accent, 0.16),
                PrimaryForeground: GetReadableForeground(Lighten(accent, 0.10)),
                PrimaryBg: Darken(accent, 0.68),
                NavigationItemSelected: Darken(accent, 0.72),
                NavigationItemSelectedForeground: Lighten(accent, 0.48));
        }

        return new LuminaAccentPalette(
            Primary: accent,
            PrimaryHover: Darken(accent, 0.12),
            PrimaryPressed: Darken(accent, 0.28),
            PrimaryForeground: GetReadableForeground(accent),
            PrimaryBg: Lighten(accent, 0.88),
            NavigationItemSelected: Lighten(accent, 0.76),
            NavigationItemSelectedForeground: Darken(accent, 0.16));
    }

    private static void ApplyPalette(IResourceDictionary resources, LuminaAccentPalette palette)
    {
        SetColor(resources, "SystemAccentColor", palette.Primary);
        SetColor(resources, "LuminaPrimary", palette.Primary);
        SetColor(resources, "LuminaPrimaryHover", palette.PrimaryHover);
        SetColor(resources, "LuminaPrimaryPressed", palette.PrimaryPressed);
        SetColor(resources, "LuminaPrimaryForeground", palette.PrimaryForeground);
        SetColor(resources, "LuminaPrimaryBg", palette.PrimaryBg);
        SetColor(resources, "LuminaNavigationPaneItemSelected", palette.NavigationItemSelected);
        SetColor(resources, "LuminaNavigationPaneItemSelectedForeground", palette.NavigationItemSelectedForeground);

        SetBrush(resources, "LuminaPrimaryBrush", palette.Primary);
        SetBrush(resources, "LuminaPrimaryHoverBrush", palette.PrimaryHover);
        SetBrush(resources, "LuminaPrimaryPressedBrush", palette.PrimaryPressed);
        SetBrush(resources, "LuminaPrimaryForegroundBrush", palette.PrimaryForeground);
        SetBrush(resources, "LuminaPrimaryBgBrush", palette.PrimaryBg);
        SetBrush(resources, "LuminaNavigationPaneItemSelectedBrush", palette.NavigationItemSelected);
        SetBrush(resources, "LuminaNavigationPaneItemSelectedForegroundBrush", palette.NavigationItemSelectedForeground);
    }

    private static void SetColor(IResourceDictionary resources, string key, Color color)
    {
        resources[key] = color;
    }

    private static void SetBrush(IResourceDictionary resources, string key, Color color)
    {
        resources[key] = new SolidColorBrush(color);
    }

    private static bool IsDarkTheme(Application application)
    {
        return application.RequestedThemeVariant == ThemeVariant.Default
            ? application.ActualThemeVariant == ThemeVariant.Dark
            : application.RequestedThemeVariant == ThemeVariant.Dark;
    }

    private static Color WithFullAlpha(Color color)
    {
        return Color.FromArgb(255, color.R, color.G, color.B);
    }

    private static Color Lighten(Color color, double amount)
    {
        return Blend(color, Colors.White, amount);
    }

    private static Color Darken(Color color, double amount)
    {
        return Blend(color, Colors.Black, amount);
    }

    private static Color Blend(Color source, Color target, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            255,
            Mix(source.R, target.R, amount),
            Mix(source.G, target.G, amount),
            Mix(source.B, target.B, amount));
    }

    private static byte Mix(byte source, byte target, double amount)
    {
        return (byte)Math.Round(source + (target - source) * amount);
    }

    private static Color GetReadableForeground(Color background)
    {
        return GetRelativeLuminance(background) > 0.42 ? DarkText : LightText;
    }

    private static double GetRelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var channel = value / 255d;
            return channel <= 0.03928
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) +
               0.7152 * Channel(color.G) +
               0.0722 * Channel(color.B);
    }

    private sealed record LuminaAccentPalette(
        Color Primary,
        Color PrimaryHover,
        Color PrimaryPressed,
        Color PrimaryForeground,
        Color PrimaryBg,
        Color NavigationItemSelected,
        Color NavigationItemSelectedForeground);
}
