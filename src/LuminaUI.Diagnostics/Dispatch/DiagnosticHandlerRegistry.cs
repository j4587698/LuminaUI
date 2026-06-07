namespace LuminaUI.Diagnostics.Dispatch;

public sealed class DiagnosticHandlerRegistry
{
    private readonly List<IDiagnosticToolHandler> _handlers = [];
    private readonly HashSet<string> _methods = new(StringComparer.Ordinal);

    public IReadOnlyList<IDiagnosticToolHandler> Handlers => _handlers.AsReadOnly();

    public DiagnosticHandlerRegistry Add(IDiagnosticToolHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (string.IsNullOrWhiteSpace(handler.Method))
            throw new ArgumentException("Diagnostic handler method is required.", nameof(handler));

        if (!_methods.Add(handler.Method))
            throw new InvalidOperationException($"Diagnostic handler method '{handler.Method}' is already registered.");

        _handlers.Add(handler);
        return this;
    }

    public DiagnosticHandlerRegistry AddRange(IEnumerable<IDiagnosticToolHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        foreach (var handler in handlers)
            Add(handler);

        return this;
    }

    public DiagnosticDispatcher CreateDispatcher() =>
        new(_handlers);
}
