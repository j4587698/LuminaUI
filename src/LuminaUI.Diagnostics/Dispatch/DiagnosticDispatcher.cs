using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Dispatch;

public sealed class DiagnosticDispatcher
{
    private readonly IReadOnlyDictionary<string, IDiagnosticToolHandler> _handlers;

    public DiagnosticDispatcher(IEnumerable<IDiagnosticToolHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _handlers = handlers.ToDictionary(
            handler => handler.Method,
            StringComparer.Ordinal);
    }

    public async Task<DiagnosticResponse> DispatchAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Method))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.InvalidRequest,
                "Diagnostic request method is required.");
        }

        if (!_handlers.TryGetValue(request.Method, out var handler))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnknownMethod,
                $"Unknown diagnostic method '{request.Method}'.");
        }

        try
        {
            return await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UiThreadTimeout,
                $"Diagnostic method '{request.Method}' was canceled.");
        }
        catch (Exception ex)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.InternalError,
                $"Diagnostic method '{request.Method}' failed: {ex.Message}");
        }
    }
}
