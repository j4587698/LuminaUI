using System;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Extensions;

public static class LuminaControlExtensions
{
    public static T FindRequiredControl<T>(this Control control, string name)
        where T : Control
    {
        ArgumentNullException.ThrowIfNull(control);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Control name cannot be empty.", nameof(name));
        }

        return control.FindControl<T>(name)
               ?? throw new InvalidOperationException($"Required control '{name}' of type '{typeof(T).Name}' was not found.");
    }

    public static T FindRequired<T>(this INameScope nameScope, string name)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(nameScope);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template part name cannot be empty.", nameof(name));
        }

        return nameScope.Find(name) as T
               ?? throw new InvalidOperationException($"Required template part '{name}' of type '{typeof(T).Name}' was not found.");
    }

    public static object FindRequiredResource(this IResourceHost resourceHost, object key)
    {
        ArgumentNullException.ThrowIfNull(resourceHost);
        ArgumentNullException.ThrowIfNull(key);

        return resourceHost.FindResource(key)
               ?? throw new InvalidOperationException($"Required resource '{key}' was not found.");
    }
}
