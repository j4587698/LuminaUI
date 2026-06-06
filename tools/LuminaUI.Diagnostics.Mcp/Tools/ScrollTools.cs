using System.ComponentModel;
using LuminaUI.Diagnostics.Abstractions;
using ModelContextProtocol.Server;

namespace LuminaUI.Diagnostics.Mcp.Tools;

[McpServerToolType]
public sealed class ScrollTools
{
    [McpServerTool(Name = LuminaDiagnosticsToolNames.Scroll, ReadOnly = false, Destructive = false),
     Description("Read or change ScrollViewer state near a target control.")]
    public static Task<DiagnosticResponse> Scroll(
        ToolForwarder forwarder,
        [Description("Optional ScrollViewer or nearby control identifier.")] string? controlId = null,
        [Description("Absolute horizontal offset.")] double? x = null,
        [Description("Absolute vertical offset.")] double? y = null,
        [Description("Relative horizontal delta.")] double? deltaX = null,
        [Description("Relative vertical delta.")] double? deltaY = null,
        [Description("Delta unit: pixels, lines, or pages.")] string? unit = null,
        [Description("Edge jump: top, bottom, left, right, home, or end.")] string? edge = null,
        [Description("Target control to bring into view.")] string? targetControlId = null,
        [Description("ItemsControl item index to bring into view.")] int? itemIndex = null,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.Scroll,
            ToolForwarder.Parameters(
                ("controlId", controlId),
                ("x", x),
                ("y", y),
                ("deltaX", deltaX),
                ("deltaY", deltaY),
                ("unit", unit),
                ("edge", edge),
                ("targetControlId", targetControlId),
                ("itemIndex", itemIndex)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetScrollableItems, ReadOnly = true, Destructive = false),
     Description("Get virtualization and scroll diagnostics for an ItemsControl.")]
    public static Task<DiagnosticResponse> GetScrollableItems(
        ToolForwarder forwarder,
        [Description("ItemsControl identifier, or a parent containing one.")] string controlId,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetScrollableItems,
            ToolForwarder.Parameters(("controlId", controlId)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);
}
