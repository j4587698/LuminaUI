using System;

namespace LuminaUI.Enums;

public enum LuminaBackdropBlurMode
{
    Off,
    [Obsolete("Use Dynamic instead; Cached now uses live backdrop blur.")]
    Cached,
    Dynamic
}
