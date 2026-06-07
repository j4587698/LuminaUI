using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Mcp.Transport;

public sealed class PipeConnection : IAsyncDisposable
{
    private readonly string _pipeName;
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public PipeConnection(string pipeName)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
            throw new ArgumentException("Pipe name is required.", nameof(pipeName));

        _pipeName = pipeName;
    }

    public async Task ConnectAsync(int timeoutMs, CancellationToken cancellationToken = default)
    {
        if (_pipe is not null)
            throw new InvalidOperationException("Pipe connection is already initialized.");

        var timeout = NormalizeTimeout(timeoutMs);
        _pipe = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await _pipe.ConnectAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Reset();
            throw;
        }

        _reader = new StreamReader(_pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        _writer = new StreamWriter(_pipe, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };
    }

    public async Task<DiagnosticResponse> SendAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_reader is null || _writer is null)
            throw new InvalidOperationException("Pipe connection is not connected.");

        var timeoutMs = NormalizeTimeout(request.TimeoutMs);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeoutMs);

        try
        {
            var payload = DiagnosticJson.SerializeRequest(request);
            await _writer.WriteLineAsync(payload.AsMemory(), timeoutSource.Token).ConfigureAwait(false);
            await _writer.FlushAsync(timeoutSource.Token).ConfigureAwait(false);

            var line = await _reader.ReadLineAsync(timeoutSource.Token).ConfigureAwait(false);
            if (line is null)
                throw new IOException("Diagnostic pipe closed before a response was received.");

            return DiagnosticJson.DeserializeResponse(line);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Diagnostic pipe request timed out after {timeoutMs} ms.", ex);
        }
        catch (JsonException)
        {
            throw;
        }
    }

    public void Reset()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _pipe?.Dispose();
        _reader = null;
        _writer = null;
        _pipe = null;
    }

    public ValueTask DisposeAsync()
    {
        Reset();
        return ValueTask.CompletedTask;
    }

    private static int NormalizeTimeout(int timeoutMs) =>
        timeoutMs > 0 ? timeoutMs : LuminaDiagnosticsProtocol.DefaultTimeoutMs;
}
