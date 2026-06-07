using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Threading;

public interface IUiThreadInvoker
{
    Task<DiagnosticResponse> InvokeAsync(
        DiagnosticRequest request,
        Func<CancellationToken, Task<DiagnosticResponse>> operation,
        CancellationToken cancellationToken = default);
}
