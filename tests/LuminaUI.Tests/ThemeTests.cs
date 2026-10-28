using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Runtime checks on the theme itself. The suite's application registers only
/// <see cref="LuminaTheme"/>, so these fail if the embedded base theme ever stops providing the
/// control themes Lumina patches.
/// </summary>
public class ThemeTests
{
    /// <summary>
    /// Control types Lumina declares as
    /// <c>&lt;ControlTheme x:Key="{x:Type X}" BasedOn="{StaticResource {x:Type X}}"&gt;</c>.
    /// Each one needs the base theme to have registered the same key first.
    /// </summary>
    private static readonly Type[] PatchedControlTypes =
    [
        typeof(Window),
        typeof(Button),
        typeof(TextBox),
        typeof(CheckBox),
        typeof(RadioButton),
        typeof(ToggleSwitch),
        typeof(ComboBox),
        typeof(ComboBoxItem),
        typeof(NumericUpDown),
        typeof(AutoCompleteBox),
        typeof(Slider),
        typeof(ProgressBar),
        typeof(ScrollViewer),
        typeof(ScrollBar),
        typeof(ListBox),
        typeof(ListBoxItem),
        typeof(TreeView),
        typeof(TreeViewItem),
        typeof(Menu),
        typeof(MenuItem),
        typeof(Separator),
        typeof(GridSplitter),
        typeof(Expander),
        typeof(ToolTip),
        typeof(SplitView),
        typeof(TransitioningContentControl),
        typeof(PathIcon),
        typeof(AdornerLayer),
        typeof(PopupRoot),
        typeof(OverlayPopupHost),
    ];

    /// <summary>Representative semantic tokens from the light/dark dictionaries.</summary>
    private static readonly string[] SemanticTokenSamples =
    [
        "LuminaBackgroundBrush",
        "LuminaSurfaceBrush",
        "LuminaNavigationPaneBrush",
        "LuminaTextPrimaryBrush",
        "LuminaTextSecondaryBrush",
        "LuminaPrimaryBrush",
        "LuminaBorderDefaultBrush",
        "LuminaDividerBrush",
    ];

    [AvaloniaFact]
    public void EmbeddedBaseThemeProvidesEveryPatchedControlTheme()
    {
        // Looked up on the embedded base theme itself: the application would also find Lumina's
        // own ControlTheme under the same type key, so an app-level lookup passes even when the
        // base theme is gone.
        LuminaTheme lumina = Application.Current!.Styles.OfType<LuminaTheme>().Single();
        IResourceProvider baseTheme = Assert.IsAssignableFrom<IResourceProvider>(lumina[0]);
        List<string> missing = [];
        List<string> notPatched = [];

        foreach (Type type in PatchedControlTypes)
        {
            if (!baseTheme.TryGetResource(type, ThemeVariant.Light, out object? baseValue) || baseValue is not ControlTheme)
            {
                missing.Add(type.Name);
            }
        }

        // Types whose Lumina theme is declared as BasedOn="{StaticResource {x:Type Self}}" must
        // resolve to a theme that really builds on the base one. Read from the XAML so the list
        // cannot drift (Button, for instance, is a full replacement based on LuminaButton).
        string[] selfBased = SelfBasedControlThemeNames();
        Assert.Contains("Window", selfBased);

        foreach (string name in selfBased)
        {
            Type type = ResolveControlType(name);
            if (!baseTheme.TryGetResource(type, ThemeVariant.Light, out object? baseValue) || baseValue is not ControlTheme baseControlTheme)
            {
                missing.Add(name);
                continue;
            }

            if (!Application.Current!.TryFindResource(type, out object? resolved)
                || resolved is not ControlTheme resolvedTheme
                || !InheritsFrom(resolvedTheme, baseControlTheme))
            {
                notPatched.Add(name);
            }
        }

        Assert.True(
            missing.Count == 0 && notPatched.Count == 0,
            "LuminaTheme embeds the Fluent base theme so that its BasedOn=\"{StaticResource "
            + "{x:Type Foo}}\" declarations resolve."
            + Environment.NewLine
            + "Missing from the base theme: " + string.Join(", ", missing)
            + Environment.NewLine
            + "Resolved theme does not build on the base theme: " + string.Join(", ", notPatched));
    }

    private static string[] SelfBasedControlThemeNames()
    {
        var pattern = new System.Text.RegularExpressions.Regex(@"<ControlTheme\b[^>]*>");
        var key = new System.Text.RegularExpressions.Regex(@"x:Key=""\{x:Type (\w+)\}""");
        var basedOn = new System.Text.RegularExpressions.Regex(@"BasedOn=""\{StaticResource \{x:Type (\w+)\}\}""");
        var names = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string file in Directory.EnumerateFiles(Path.Combine(RepoPaths.CoreThemeDirectory, "Themes"), "*.axaml"))
        {
            foreach (System.Text.RegularExpressions.Match tag in pattern.Matches(File.ReadAllText(file)))
            {
                var keyMatch = key.Match(tag.Value);
                var basedOnMatch = basedOn.Match(tag.Value);
                if (keyMatch.Success && basedOnMatch.Success && keyMatch.Groups[1].Value == basedOnMatch.Groups[1].Value)
                {
                    names.Add(keyMatch.Groups[1].Value);
                }
            }
        }

        return [.. names];
    }

    private static Type ResolveControlType(string name) =>
        typeof(Window).Assembly.GetTypes().FirstOrDefault(type => type.Name == name && typeof(StyledElement).IsAssignableFrom(type))
        ?? typeof(StyledElement).Assembly.GetTypes().FirstOrDefault(type => type.Name == name && typeof(StyledElement).IsAssignableFrom(type))
        ?? throw new InvalidOperationException($"Cannot resolve control type '{name}' referenced by a Lumina ControlTheme.");

    private static bool InheritsFrom(ControlTheme theme, ControlTheme ancestor)
    {
        for (ControlTheme? current = theme; current != null; current = current.BasedOn)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    [AvaloniaFact]
    public void LuminaThemeEmbedsFluentWhenTheHostHasNoBaseTheme()
    {
        // The suite's application registers no base theme of its own.
        LuminaTheme theme = Application.Current!.Styles.OfType<LuminaTheme>().Single();

        Assert.True(theme.IncludesBaseTheme);
        Assert.IsType<FluentTheme>(theme[0]);
    }

    [AvaloniaFact]
    public void LuminaThemeReusesAnExistingBaseThemeInsteadOfEmbeddingASecondCopy()
    {
        // The application already has a base theme at this point (the one its LuminaTheme
        // embedded), exactly like a host that declares <FluentTheme /> ahead of LuminaTheme. The
        // new instance must not stack another Fluent copy on top - and must still load, which
        // means its BasedOn="{StaticResource {x:Type Foo}}" keys resolved against the app.
        var theme = new LuminaTheme();

        Assert.False(theme.IncludesBaseTheme);
        Assert.DoesNotContain(theme, style => style is FluentTheme);
    }

    [AvaloniaFact]
    public void HostBaseThemeDensityIsNotOverriddenByLuminaTheme()
    {
        // Regression: when LuminaTheme always embedded Fluent, its copy sat after the host's own
        // <FluentTheme DensityStyle="Compact" /> and won every resource lookup, silently resetting
        // density (and palettes) to the defaults.
        const string Key = "TextControlThemePadding";
        var compactOnly = new Styles { new FluentTheme { DensityStyle = DensityStyle.Compact } };
        var normalOnly = new Styles { new FluentTheme() };
        var host = new Styles { new FluentTheme { DensityStyle = DensityStyle.Compact }, new LuminaTheme() };

        Assert.True(compactOnly.TryGetResource(Key, ThemeVariant.Light, out object? compactValue));
        Assert.True(normalOnly.TryGetResource(Key, ThemeVariant.Light, out object? normalValue));
        Assert.NotEqual(normalValue, compactValue);

        Assert.True(host.TryGetResource(Key, ThemeVariant.Light, out object? resolved));
        Assert.Equal(compactValue, resolved);
    }

    [AvaloniaFact]
    public void FocusRingFollowsTheAccentColor()
    {
        Color original = LuminaUI.Theming.LuminaThemeManager.CurrentAccentColor;
        try
        {
            LuminaUI.Theming.LuminaThemeManager.SetAccentColor(Color.Parse("#E11D48"));

            Assert.True(Application.Current!.TryFindResource("LuminaPrimaryBrush", out object? primary));
            Assert.True(Application.Current!.TryFindResource("LuminaBorderFocusBrush", out object? focus));
            // The focus ring is documented as "matches LuminaPrimary" but used to stay blue.
            Assert.Equal(((ISolidColorBrush)primary!).Color, ((ISolidColorBrush)focus!).Color);
        }
        finally
        {
            LuminaUI.Theming.LuminaThemeManager.SetAccentColor(original);
        }
    }

    [AvaloniaFact]
    public void SemanticTokensResolveInBothVariants()
    {
        List<string> missing = [];

        foreach (ThemeVariant variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            foreach (string key in SemanticTokenSamples)
            {
                if (!Application.Current!.TryFindResource(key, variant, out object? value) || value is null)
                {
                    missing.Add($"{key} ({variant})");
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            "These semantic tokens did not resolve. A DynamicResource that cannot resolve leaves the "
            + "setter without effect instead of throwing:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, missing));
    }

    [AvaloniaFact]
    public void ThemeVariantsResolveDifferentColors()
    {
        Assert.True(
            Application.Current!.TryFindResource("LuminaBackgroundBrush", ThemeVariant.Light, out object? light),
            "LuminaBackgroundBrush is missing from the light theme dictionary.");
        Assert.True(
            Application.Current!.TryFindResource("LuminaBackgroundBrush", ThemeVariant.Dark, out object? dark),
            "LuminaBackgroundBrush is missing from the dark theme dictionary.");

        Assert.IsAssignableFrom<ISolidColorBrush>(light);
        Assert.IsAssignableFrom<ISolidColorBrush>(dark);
        Assert.NotEqual(((ISolidColorBrush)light!).Color, ((ISolidColorBrush)dark!).Color);
    }
}
