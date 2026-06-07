using System.Collections;
using System.Text.Json.Nodes;
using LuminaUI.Diagnostics.Abstractions;
using LuminaUI.Diagnostics.Mcp.Targeting;
using LuminaUI.Diagnostics.Mcp.Transport;

namespace LuminaUI.Diagnostics.Mcp.Tools;

public sealed class ToolForwarder
{
    private readonly Func<string, DiagnosticRequest, CancellationToken, Task<DiagnosticResponse>> _sendAsync;

    public ToolForwarder(PipeDiagnosticClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _sendAsync = client.SendAsync;
    }

    public ToolForwarder(Func<string, DiagnosticRequest, CancellationToken, Task<DiagnosticResponse>> sendAsync)
    {
        _sendAsync = sendAsync ?? throw new ArgumentNullException(nameof(sendAsync));
    }

    public async Task<string> ForwardAsync(
        string toolName,
        JsonObject parameters,
        int? pid,
        string? pipe,
        int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return DiagnosticJson.SerializeResponse(DiagnosticResponse.Fail(
                Guid.NewGuid().ToString("N"),
                DiagnosticErrorCode.InvalidRequest,
                "Tool name is required."));
        }

        var target = TargetResolver.Resolve(new TargetOptions(pid, pipe, timeoutMs));
        var requestId = Guid.NewGuid().ToString("N");
        if (!target.Success)
        {
            return DiagnosticJson.SerializeResponse(DiagnosticResponse.Fail(
                requestId,
                target.Error!.Code,
                target.Error.Message,
                target.Error.Details));
        }

        var request = new DiagnosticRequest(
            requestId,
            toolName,
            parameters,
            target.TimeoutMs);

        var response = await _sendAsync(target.DiagnosticsPipeName!, request, cancellationToken).ConfigureAwait(false);
        return DiagnosticJson.SerializeResponse(response);
    }

    public static JsonObject Parameters(params (string Name, object? Value)[] values)
    {
        var parameters = new JsonObject();
        foreach (var (name, value) in values)
        {
            if (string.IsNullOrWhiteSpace(name) || value is null)
                continue;

            parameters[name] = ToJsonNode(value);
        }

        return parameters;
    }

    private static JsonNode? ToJsonNode(object value)
    {
        if (value is JsonNode node)
            return node.DeepClone();

        if (value is IEnumerable enumerable && value is not string)
        {
            var array = new JsonArray();
            foreach (var item in enumerable)
            {
                if (item is not null)
                    array.Add(ToJsonNode(item));
            }

            return array;
        }

        return JsonValue.Create(value);
    }
}
