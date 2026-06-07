using System.ComponentModel;
using LuminaUI.Diagnostics.Abstractions;
using ModelContextProtocol.Server;

namespace LuminaUI.Diagnostics.Mcp.Tools;

[McpServerToolType]
public sealed class PropertyTools
{
    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetControlProperties, ReadOnly = true, Destructive = false),
     Description("Get Avalonia and selected CLR properties for a target control.")]
    public static Task<string> GetControlProperties(
        ToolForwarder forwarder,
        [Description("Control identifier such as #Name, TypeName, or TypeName[index].")] string controlId,
        [Description("Optional property names to filter the response.")] string[]? propertyNames = null,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetControlProperties,
            ToolForwarder.Parameters(
                ("controlId", controlId),
                ("propertyNames", propertyNames is { Length: > 0 } ? propertyNames : null)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetDataContext, ReadOnly = true, Destructive = false),
     Description("Get DataContext type and public property summaries for a target control.")]
    public static Task<string> GetDataContext(
        ToolForwarder forwarder,
        [Description("Optional control identifier. If omitted, diagnostics host uses the main window.")] string? controlId = null,
        [Description("Optional collection property to expand.")] string? expandProperty = null,
        [Description("Maximum collection items to include when expanding a property.")] int maxItems = 50,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetDataContext,
            ToolForwarder.Parameters(
                ("controlId", controlId),
                ("expandProperty", expandProperty),
                ("maxItems", maxItems)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetBindingErrors, ReadOnly = true, Destructive = false),
     Description("Get binding errors captured by the target diagnostics host.")]
    public static Task<string> GetBindingErrors(
        ToolForwarder forwarder,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetBindingErrors,
            [],
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetAppliedStyles, ReadOnly = true, Destructive = false),
     Description("Get style classes, pseudo-classes, and style setter summaries for a target control.")]
    public static Task<string> GetAppliedStyles(
        ToolForwarder forwarder,
        [Description("Control identifier such as #Name, TypeName, or TypeName[index].")] string controlId,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetAppliedStyles,
            ToolForwarder.Parameters(("controlId", controlId)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.GetResources, ReadOnly = true, Destructive = false),
     Description("Get application or control resource summaries from the target diagnostics host.")]
    public static Task<string> GetResources(
        ToolForwarder forwarder,
        [Description("Optional control identifier. If omitted, returns application resources.")] string? controlId = null,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.GetResources,
            ToolForwarder.Parameters(("controlId", controlId)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);
}
