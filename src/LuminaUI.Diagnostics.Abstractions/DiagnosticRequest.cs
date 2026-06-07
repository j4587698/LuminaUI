using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LuminaUI.Diagnostics.Abstractions;

public sealed record DiagnosticRequest
{
    public DiagnosticRequest()
    {
    }

    public DiagnosticRequest(
        string id,
        string method,
        JsonObject? parameters = null,
        int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs)
    {
        Id = id;
        Method = method;
        Parameters = parameters ?? [];
        TimeoutMs = timeoutMs;
    }

    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("method")]
    public string Method { get; init; } = "";

    [JsonPropertyName("params")]
    public JsonObject Parameters { get; init; } = [];

    [JsonPropertyName("timeoutMs")]
    public int TimeoutMs { get; init; } = LuminaDiagnosticsProtocol.DefaultTimeoutMs;
}
