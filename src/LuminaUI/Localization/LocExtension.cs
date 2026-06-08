using System;
using Avalonia;
using Avalonia.Metadata;

namespace LuminaUI.Localization;

public sealed class LocExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public string? Fallback { get; set; }

    public object ProvideValue(IServiceProvider? serviceProvider)
    {
        return string.IsNullOrWhiteSpace(Key)
            ? Fallback ?? string.Empty
            : LuminaLocalization.Observe(Key, Fallback).ToBinding();
    }
}
