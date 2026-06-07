using System.ComponentModel;
using LuminaUI.Diagnostics.Abstractions;
using ModelContextProtocol.Server;

namespace LuminaUI.Diagnostics.Mcp.Tools;

[McpServerToolType]
public sealed class VisualTools
{
    [McpServerTool(Name = LuminaDiagnosticsToolNames.TakeScreenshot, ReadOnly = true, Destructive = false),
     Description("Capture a PNG screenshot of a target window or control.")]
    public static Task<string> TakeScreenshot(
        ToolForwarder forwarder,
        [Description("Window index from list_windows.")] int windowIndex = 0,
        [Description("Optional control identifier to capture instead of the whole window.")] string? controlId = null,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.TakeScreenshot,
            ToolForwarder.Parameters(
                ("windowIndex", windowIndex),
                ("controlId", controlId)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);
}
