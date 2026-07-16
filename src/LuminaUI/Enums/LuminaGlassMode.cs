using System;

namespace LuminaUI.Enums;

public enum LuminaGlassMode
{
    Off,
    Pseudo,
    AcrylicCached,
    AcrylicDynamic,

    [Obsolete("Use AcrylicDynamic instead.")]
    Acrylic = AcrylicDynamic
}
