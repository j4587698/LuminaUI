using System.Text.Json;
using System.Text.Json.Nodes;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Mcp.Transport;

public sealed class PipeDiagnosticClient
{
    public async Task<DiagnosticResponse> SendAsync(
        string pipeName,
        DiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.InvalidRequest,
                "Pipe name is required.");
        }

        var normalizedDiagnosticsPipeName = pipeName.Trim();
        var timeoutMs = request.TimeoutMs > 0
            ? request.TimeoutMs
            : LuminaDiagnosticsProtocol.DefaultTimeoutMs;

        await using var connection = new PipeConnection(normalizedDiagnosticsPipeName);

        try
        {
            await connection.ConnectAsync(timeoutMs, cancellationToken).ConfigureAwait(false);
            return await connection.SendAsync(request with { TimeoutMs = timeoutMs }, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            connection.Reset();
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.SerializationFailure,
                $"Diagnostic response JSON was invalid: {ex.Message}",
                CreateDetails(normalizedDiagnosticsPipeName, timeoutMs));
        }
        catch (TimeoutException ex)
        {
            connection.Reset();
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TransportFailure,
                $"Diagnostic pipe operation timed out for '{normalizedDiagnosticsPipeName}': {ex.Message}",
                CreateDetails(normalizedDiagnosticsPipeName, timeoutMs));
        }
        catch (OperationCanceledException ex)
        {
            connection.Reset();
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TransportFailure,
                $"Diagnostic pipe operation was canceled for '{normalizedDiagnosticsPipeName}': {ex.Message}",
                CreateDetails(normalizedDiagnosticsPipeName, timeoutMs));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException or InvalidOperationException)
        {
            connection.Reset();
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TransportFailure,
                $"Diagnostic pipe operation failed for '{normalizedDiagnosticsPipeName}': {ex.Message}",
                CreateDetails(normalizedDiagnosticsPipeName, timeoutMs));
        }
    }

    private static JsonObject CreateDetails(string pipeName, int timeoutMs) =>
        new()
        {
            ["pipeName"] = pipeName,
            ["timeoutMs"] = timeoutMs
        };
}
