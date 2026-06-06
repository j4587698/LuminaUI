using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LuminaUI.Diagnostics.Abstractions;

public sealed record DiagnosticError(
    [property: JsonPropertyName("code")] DiagnosticErrorCode Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("details")] JsonObject? Details = null);
