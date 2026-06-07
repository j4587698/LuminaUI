using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Transport;

public sealed class PipeDiagnosticsServer : IDisposable
{
    private readonly object _gate = new();
    private readonly string _pipeName;
    private readonly DiagnosticDispatcher _dispatcher;
    private readonly int _defaultTimeoutMs;
    private CancellationTokenSource? _stopSignal;
    private Task? _runTask;

    public PipeDiagnosticsServer(
        string pipeName,
        DiagnosticDispatcher dispatcher,
        int defaultTimeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
            throw new ArgumentException("Pipe name is required.", nameof(pipeName));

        _pipeName = pipeName.Trim();
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _defaultTimeoutMs = defaultTimeoutMs > 0 ? defaultTimeoutMs : LuminaDiagnosticsProtocol.DefaultTimeoutMs;
    }

    public bool IsRunning { get; private set; }

    public void Start()
    {
        lock (_gate)
        {
            if (IsRunning)
                return;

            var stopSignal = new CancellationTokenSource();
            _stopSignal = stopSignal;
            _runTask = Task.Run(() => RunAsync(stopSignal.Token));
            IsRunning = true;
        }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? stopSignal;
        Task? runTask;
        lock (_gate)
        {
            if (!IsRunning)
                return;

            stopSignal = _stopSignal;
            runTask = _runTask;
            _stopSignal = null;
            _runTask = null;
            IsRunning = false;
        }

        stopSignal?.Cancel();

        if (runTask is not null)
        {
            try
            {
                await runTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        stopSignal?.Dispose();
    }

    public async Task<string> ProcessLineAsync(
        string line,
        CancellationToken cancellationToken = default)
    {
        if (!DiagnosticJson.TryDeserializeRequest(line, out var request, out var error))
        {
            return DiagnosticJson.SerializeFailure(
                "",
                error!.Code,
                error.Message,
                error.Details);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request!.TimeoutMs > 0 ? request.TimeoutMs : _defaultTimeoutMs);

        try
        {
            var response = await _dispatcher.DispatchAsync(request, timeout.Token).ConfigureAwait(false);
            return DiagnosticJson.SerializeResponse(response);
        }
        catch (JsonException ex)
        {
            return DiagnosticJson.SerializeFailure(
                request.Id,
                DiagnosticErrorCode.SerializationFailure,
                $"Diagnostic response JSON was invalid: {ex.Message}");
        }
        catch (Exception ex)
        {
            return DiagnosticJson.SerializeFailure(
                request.Id,
                DiagnosticErrorCode.InternalError,
                $"Diagnostic request failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await ProcessClientAsync(pipe, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
            }
        }
    }

    private async Task ProcessClientAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break;

            var response = await ProcessLineAsync(line, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(response.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
