using System;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public sealed record LuminaImageLoadOptions(
    LuminaImageCacheMode CacheMode,
    TimeSpan CacheDuration,
    string? CacheDirectory,
    int DecodePixelWidth = 0,
    int DecodePixelHeight = 0);
