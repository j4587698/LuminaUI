using Avalonia.Logging;

namespace LuminaUI.Tests;

/// <summary>
/// Temporarily replaces Avalonia's log sink and records every Warning/Error/Fatal entry.
/// </summary>
/// <remarks>
/// Most template and binding mistakes do not throw: a binding to a missing property, a template
/// part of the wrong type or an invalid property value is logged at Warning and the control
/// silently renders wrong. Capturing the log turns those into test failures. Missing
/// <c>DynamicResource</c> keys are not logged by Avalonia; <see cref="ResourceContractTests"/>
/// covers those statically.
/// </remarks>
internal sealed class LogCapture : ILogSink, IDisposable
{
    private readonly ILogSink? _previous;
    private readonly List<string> _entries = [];

    private LogCapture()
    {
        _previous = Logger.Sink;
        Logger.Sink = this;
    }

    public IReadOnlyList<string> Entries => _entries;

    public static LogCapture Start() => new();

    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Record(level, area, source, messageTemplate, []);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
        Record(level, area, source, messageTemplate, propertyValues);

    public void Dispose() => Logger.Sink = _previous;

    private void Record(LogEventLevel level, string area, object? source, string template, object?[] values)
    {
        if (level < LogEventLevel.Warning)
        {
            return;
        }

        string details = values.Length == 0 ? string.Empty : " [" + string.Join("; ", values) + "]";
        lock (_entries)
        {
            _entries.Add($"{level} {area} ({source?.GetType().Name ?? "-"}): {template}{details}");
        }
    }
}
