using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LuminaUI.Diagnostics.Abstractions;

public sealed record DiagnosticResponse
{
    public DiagnosticResponse()
    {
    }

    private DiagnosticResponse(string id, bool success, JsonNode? data, DiagnosticError? error)
    {
        Id = id;
        Success = success;
        Data = data;
        Error = error;
    }

    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public JsonNode? Data { get; init; }

    [JsonPropertyName("error")]
    public DiagnosticError? Error { get; init; }

    public static DiagnosticResponse Ok(string id, JsonNode? data = null) =>
        new(id, success: true, data, error: null);

    public static DiagnosticResponse Fail(string id, DiagnosticErrorCode code, string message, JsonObject? details = null) =>
        new(id, success: false, data: null, error: new DiagnosticError(code, message, details));
}
