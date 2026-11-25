using System;

namespace LuminaUI.Enums;

public enum LuminaGlassMode
{
    Off,
    Pseudo,
    [Obsolete("Use AcrylicDynamic instead; AcrylicCached now uses live backdrop blur.")]
    AcrylicCached,
    AcrylicDynamic,

    [Obsolete("Use AcrylicDynamic instead.")]
    Acrylic = AcrylicDynamic
}
