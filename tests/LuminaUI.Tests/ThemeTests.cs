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
        List<string> missing = [];

        foreach (Type type in PatchedControlTypes)
        {
            if (!Application.Current!.TryFindResource(type, out object? value) || value is not ControlTheme)
            {
                missing.Add(type.Name);
            }
        }

        Assert.True(
            missing.Count == 0,
            "LuminaTheme embeds the Fluent base theme so that its BasedOn=\"{StaticResource "
            + "{x:Type Foo}}\" declarations resolve. No ControlTheme was found for these types, "
            + "which means the base theme is missing, registered too late, or no longer provides "
            + "them:"
            + Environment.NewLine
            + string.Join(", ", missing));
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
