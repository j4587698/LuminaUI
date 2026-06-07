using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Transport;

namespace LuminaUI.Diagnostics;

public sealed class LuminaUIDiagnosticsHost : IDisposable
{
    private readonly object _gate = new();
    private readonly DiagnosticDispatcher _dispatcher;
    private readonly DiagnosticsServices? _services;
    private readonly LuminaUIDiagnosticsOptions _options;
    private PipeDiagnosticsServer? _server;

    public LuminaUIDiagnosticsHost(
        LuminaUIDiagnosticsOptions? options = null,
        IEnumerable<IDiagnosticToolHandler>? handlers = null)
    {
        _options = options ?? new LuminaUIDiagnosticsOptions();
        if (handlers is null)
        {
            _services = DiagnosticsServiceFactory.CreateDefault();
            _dispatcher = _services.Dispatcher;
        }
        else
        {
            _dispatcher = new DiagnosticDispatcher(handlers);
        }

        DiagnosticsPipeName = _options.ResolveDiagnosticsPipeName();
        DefaultTimeoutMs = _options.DefaultTimeoutMs > 0
            ? _options.DefaultTimeoutMs
            : throw new ArgumentOutOfRangeException(nameof(options), "Default timeout must be positive.");
    }

    public string DiagnosticsPipeName { get; }

    public int DefaultTimeoutMs { get; }

    public bool IsRunning { get; private set; }

    public void Start()
    {
        lock (_gate)
        {
            if (IsRunning)
                return;

            _server = new PipeDiagnosticsServer(DiagnosticsPipeName, _dispatcher, DefaultTimeoutMs);
            _server.Start();
            IsRunning = true;
        }
    }

    public void Stop()
    {
        PipeDiagnosticsServer? server;
        lock (_gate)
        {
            if (!IsRunning)
                return;

            server = _server;
            _server = null;
            IsRunning = false;
        }

        server?.Dispose();
    }

    public void Dispose()
    {
        Stop();
        _services?.Dispose();
    }
}
