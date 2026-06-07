using System.Text.Json.Nodes;
using Avalonia.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Threading;

public sealed class AvaloniaUiThreadInvoker : IUiThreadInvoker
{
    private readonly Func<bool> _checkAccess;
    private readonly Func<Func<CancellationToken, Task<DiagnosticResponse>>, CancellationToken, Task<DiagnosticResponse>> _dispatchAsync;

    public AvaloniaUiThreadInvoker()
        : this(
            () => Dispatcher.UIThread.CheckAccess(),
            (operation, cancellationToken) => Dispatcher.UIThread
                .InvokeAsync(
                    () => operation(cancellationToken),
                    DispatcherPriority.Normal)
                .WaitAsync(cancellationToken))
    {
    }

    internal AvaloniaUiThreadInvoker(
        Func<bool> checkAccess,
        Func<Func<CancellationToken, Task<DiagnosticResponse>>, CancellationToken, Task<DiagnosticResponse>> dispatchAsync)
    {
        _checkAccess = checkAccess ?? throw new ArgumentNullException(nameof(checkAccess));
        _dispatchAsync = dispatchAsync ?? throw new ArgumentNullException(nameof(dispatchAsync));
    }

    public async Task<DiagnosticResponse> InvokeAsync(
        DiagnosticRequest request,
        Func<CancellationToken, Task<DiagnosticResponse>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(operation);

        var timeoutMs = request.TimeoutMs > 0
            ? request.TimeoutMs
            : LuminaDiagnosticsProtocol.DefaultTimeoutMs;

        using var timeoutSignal = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSignal.CancelAfter(timeoutMs);

        try
        {
            if (_checkAccess())
                return await operation(timeoutSignal.Token).ConfigureAwait(false);

            return await _dispatchAsync(operation, timeoutSignal.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutSignal.IsCancellationRequested)
        {
            return CreateTimeoutResponse(request, timeoutMs, cancellationToken.IsCancellationRequested);
        }
        catch (TimeoutException)
        {
            return CreateTimeoutResponse(request, timeoutMs, wasCanceled: false);
        }
    }

    private static DiagnosticResponse CreateTimeoutResponse(
        DiagnosticRequest request,
        int timeoutMs,
        bool wasCanceled)
    {
        var message = wasCanceled
            ? $"UI thread operation for '{request.Method}' was canceled."
            : $"UI thread operation for '{request.Method}' timed out after {timeoutMs} ms.";

        return DiagnosticResponse.Fail(
            request.Id,
            DiagnosticErrorCode.UiThreadTimeout,
            message,
            new JsonObject
            {
                ["method"] = request.Method,
                ["timeoutMs"] = timeoutMs
            });
    }
}
