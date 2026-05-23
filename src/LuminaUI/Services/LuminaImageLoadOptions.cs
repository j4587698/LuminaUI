using System;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public sealed record LuminaImageLoadOptions(LuminaImageCacheMode CacheMode, TimeSpan CacheDuration, string? CacheDirectory);
