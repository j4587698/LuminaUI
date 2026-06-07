using System.Text.Json.Nodes;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics;

public sealed class DiagnosticsPingHandler : IDiagnosticToolHandler
{
    public string Method => LuminaDiagnosticsToolNames.Ping;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            DiagnosticResponse.Ok(
                request.Id,
                new JsonObject
                {
                    ["name"] = LuminaDiagnosticsProtocol.Name,
                    ["version"] = LuminaDiagnosticsProtocol.Version,
                    ["processId"] = Environment.ProcessId,
                    ["timestampUtc"] = DateTimeOffset.UtcNow.ToString("O")
                }));
}
