namespace LuminaUI.Theming;

/// <summary>
/// Defines density modes for LuminaUI controls.
/// </summary>
public enum LuminaDensityMode
{
    /// <summary>
    /// Standard comfortable desktop sizing (default, ~36px height).
    /// </summary>
    Normal,

    /// <summary>
    /// High information density for data-heavy desktop interfaces (~28px height).
    /// </summary>
    Compact,

    /// <summary>
    /// Spacious touch-friendly or prominent display sizing (~44-48px height).
    /// </summary>
    Loose
}