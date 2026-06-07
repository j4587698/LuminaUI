using System.Globalization;
using Avalonia.Logging;

namespace LuminaUI.Diagnostics.Binding;

public sealed class BindingErrorLogSink : ILogSink, IDisposable
{
    private readonly BindingErrorStore _store;
    private readonly ILogSink? _inner;
    private readonly bool _installed;
    private bool _disposed;

    public BindingErrorLogSink(
        BindingErrorStore store,
        ILogSink? inner = null,
        bool installed = false)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _inner = inner;
        _installed = installed;
    }

    public static BindingErrorLogSink Install(BindingErrorStore store)
    {
        var previous = Logger.Sink;
        var sink = new BindingErrorLogSink(store, previous, installed: true);
        Logger.Sink = sink;
        return sink;
    }

    public bool IsEnabled(
        LogEventLevel level,
        string area) =>
        IsBindingWarningOrError(level, area)
        || (_inner?.IsEnabled(level, area) ?? false);

    public void Log(
        LogEventLevel level,
        string area,
        object? source,
        string messageTemplate)
    {
        Capture(level, area, source, messageTemplate);
        _inner?.Log(level, area, source, messageTemplate);
    }

    public void Log(
        LogEventLevel level,
        string area,
        object? source,
        string messageTemplate,
        params object?[] propertyValues)
    {
        Capture(level, area, source, FormatMessage(messageTemplate, propertyValues));
        _inner?.Log(level, area, source, messageTemplate, propertyValues);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_installed && ReferenceEquals(Logger.Sink, this))
            Logger.Sink = _inner;
    }

    private void Capture(
        LogEventLevel level,
        string area,
        object? source,
        string message)
    {
        if (!IsBindingWarningOrError(level, area))
            return;

        _store.Add(
            new BindingErrorEntry(
                DateTimeOffset.UtcNow,
                level.ToString(),
                area,
                source?.GetType().FullName ?? "",
                message));
    }

    private static bool IsBindingWarningOrError(
        LogEventLevel level,
        string area) =>
        string.Equals(area, LogArea.Binding, StringComparison.Ordinal)
        && level >= LogEventLevel.Warning;

    private static string FormatMessage(
        string messageTemplate,
        object?[] propertyValues)
    {
        if (propertyValues.Length == 0)
            return messageTemplate;

        try
        {
            return string.Format(CultureInfo.InvariantCulture, messageTemplate, propertyValues);
        }
        catch (FormatException)
        {
            return $"{messageTemplate} | {string.Join(", ", propertyValues.Select(value => value?.ToString()))}";
        }
    }
}
