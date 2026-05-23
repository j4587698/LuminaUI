using Avalonia.Media;

namespace LuminaUI.Theming;

public sealed record LuminaThemeAccentPreset(string Name, Color Color)
{
    public string Hex => LuminaThemeManager.ToHex(Color);
}
