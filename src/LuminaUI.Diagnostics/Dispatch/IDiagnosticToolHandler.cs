using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Dispatch;

public interface IDiagnosticToolHandler
{
    string Method { get; }

    Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default);
}
