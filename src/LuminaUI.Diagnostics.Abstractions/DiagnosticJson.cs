using System.Text.Json;
using System.Text.Json.Nodes;

namespace LuminaUI.Diagnostics.Abstractions;

public static class DiagnosticJson
{
    public static JsonSerializerOptions Options { get; } = new(DiagnosticJsonContext.Default.Options)
    {
        MaxDepth = 256
    };

    private static DiagnosticJsonContext Context { get; } = new(Options);

    public static string SerializeRequest(DiagnosticRequest request) =>
        JsonSerializer.Serialize(request, Context.DiagnosticRequest);

    public static string SerializeResponse(DiagnosticResponse response) =>
        JsonSerializer.Serialize(response, Context.DiagnosticResponse);

    public static DiagnosticRequest DeserializeRequest(string json) =>
        JsonSerializer.Deserialize(json, Context.DiagnosticRequest)
        ?? throw new JsonException("Diagnostic request JSON produced a null value.");

    public static DiagnosticResponse DeserializeResponse(string json) =>
        JsonSerializer.Deserialize(json, Context.DiagnosticResponse)
        ?? throw new JsonException("Diagnostic response JSON produced a null value.");

    public static bool TryDeserializeRequest(
        string json,
        out DiagnosticRequest? request,
        out DiagnosticError? error)
    {
        try
        {
            request = DeserializeRequest(json);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            request = null;
            error = new DiagnosticError(DiagnosticErrorCode.InvalidRequest, $"Invalid request JSON: {ex.Message}");
            return false;
        }
    }

    public static string SerializeFailure(string id, DiagnosticErrorCode code, string message, JsonObject? details = null)
    {
        try
        {
            return SerializeResponse(DiagnosticResponse.Fail(id, code, message, details));
        }
        catch (Exception ex)
        {
            var safeMessage = $"Serialization failure: {ex.Message}";
            return "{\"id\":\"" + Escape(id) + "\",\"success\":false,\"data\":null,\"error\":{\"code\":\"SerializationFailure\",\"message\":\"" + Escape(safeMessage) + "\"}}";
        }
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
