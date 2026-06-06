using System.ComponentModel;
using LuminaUI.Diagnostics.Abstractions;
using ModelContextProtocol.Server;

namespace LuminaUI.Diagnostics.Mcp.Tools;

[McpServerToolType]
public sealed class InspectionTools
{
    [McpServerTool(Name = LuminaDiagnosticsToolNames.ListWindows, ReadOnly = true, Destructive = false),
     Description("List windows in the target LuminaUI.Diagnostics-enabled Avalonia application.")]
    public static Task<DiagnosticResponse> ListWindows(
        ToolForwarder forwarder,
        [Description("Target process ID. When provided, LuminaUI.Diagnostics uses the default pipe name lumina-ui-diagnostics-{pid}.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.ListWindows,
            [],
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetVisualTree, ReadOnly = true, Destructive = false),
     Description("Get a bounded visual tree for a target window or control.")]
    public static Task<DiagnosticResponse> GetVisualTree(
        ToolForwarder forwarder,
        [Description("Window index from list_windows.")] int windowIndex = 0,
        [Description("Optional control identifier such as #Name, TypeName, or TypeName[index].")] string? controlId = null,
        [Description("Maximum traversal depth.")] int maxDepth = 10,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetVisualTree,
            ToolForwarder.Parameters(
                ("windowIndex", windowIndex),
                ("controlId", controlId),
                ("maxDepth", maxDepth)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetLogicalTree, ReadOnly = true, Destructive = false),
     Description("Get a bounded logical tree for a target window or control.")]
    public static Task<DiagnosticResponse> GetLogicalTree(
        ToolForwarder forwarder,
        [Description("Window index from list_windows.")] int windowIndex = 0,
        [Description("Optional control identifier such as #Name, TypeName, or TypeName[index].")] string? controlId = null,
        [Description("Maximum traversal depth.")] int maxDepth = 10,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetLogicalTree,
            ToolForwarder.Parameters(
                ("windowIndex", windowIndex),
                ("controlId", controlId),
                ("maxDepth", maxDepth)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.FindControl, ReadOnly = true, Destructive = false),
     Description("Search controls by name, type, or displayed text across target windows.")]
    public static Task<DiagnosticResponse> FindControl(
        ToolForwarder forwarder,
        [Description("Name filter.")] string? name = null,
        [Description("Type name filter, such as Button or TextBox.")] string? typeName = null,
        [Description("Displayed text filter.")] string? text = null,
        [Description("Maximum number of matches to return.")] int maxResults = 20,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.FindControl,
            ToolForwarder.Parameters(
                ("name", name),
                ("typeName", typeName),
                ("text", text),
                ("maxResults", maxResults)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetFocusedElement, ReadOnly = true, Destructive = false),
     Description("Get the currently focused element in the target application.")]
    public static Task<DiagnosticResponse> GetFocusedElement(
        ToolForwarder forwarder,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetFocusedElement,
            [],
            pid,
            pipe,
            timeoutMs,
            cancellationToken);
}
