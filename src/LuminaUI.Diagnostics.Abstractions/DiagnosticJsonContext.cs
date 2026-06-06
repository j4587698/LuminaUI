using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LuminaUI.Diagnostics.Abstractions;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DiagnosticRequest))]
[JsonSerializable(typeof(DiagnosticResponse))]
[JsonSerializable(typeof(DiagnosticError))]
[JsonSerializable(typeof(DiagnosticErrorCode))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonArray))]
[JsonSerializable(typeof(JsonValue))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
internal partial class DiagnosticJsonContext : JsonSerializerContext
{
}
