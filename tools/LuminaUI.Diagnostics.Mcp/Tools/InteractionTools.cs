using System.ComponentModel;
using LuminaUI.Diagnostics.Abstractions;
using ModelContextProtocol.Server;

namespace LuminaUI.Diagnostics.Mcp.Tools;

[McpServerToolType]
public sealed class InteractionTools
{
    [McpServerTool(Name = LuminaDiagnosticsToolNames.ClickControl, ReadOnly = false, Destructive = false),
     Description("Click or command-invoke a target control.")]
    public static Task<string> ClickControl(
        ToolForwarder forwarder,
        [Description("Control identifier such as #Name, TypeName, or TypeName[index].")] string controlId,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.ClickControl,
            ToolForwarder.Parameters(("controlId", controlId)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.SetProperty, ReadOnly = false, Destructive = false),
     Description("Set a supported Avalonia property on a target control.")]
    public static Task<string> SetProperty(
        ToolForwarder forwarder,
        [Description("Control identifier such as #Name, TypeName, or TypeName[index].")] string controlId,
        [Description("Property name to set.")] string propertyName,
        [Description("String value converted by the diagnostics host.")] string value,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.SetProperty,
            ToolForwarder.Parameters(
                ("controlId", controlId),
                ("propertyName", propertyName),
                ("value", value)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.InputText, ReadOnly = false, Destructive = false),
     Description("Set text on a TextBox target or the first TextBox child inside a target control.")]
    public static Task<string> InputText(
        ToolForwarder forwarder,
        [Description("Control identifier for a TextBox or parent containing a TextBox.")] string controlId,
        [Description("Text to input.")] string text,
        [Description("Whether to press Enter after setting text.")] bool pressEnter = false,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.InputText,
            ToolForwarder.Parameters(
                ("controlId", controlId),
                ("text", text),
                ("pressEnter", pressEnter)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.InvokeCommand, ReadOnly = false, Destructive = false),
     Description("Invoke an ICommand exposed by the target control DataContext.")]
    public static Task<string> InvokeCommand(
        ToolForwarder forwarder,
        [Description("Control identifier whose DataContext contains the command.")] string controlId,
        [Description("ICommand property name on the DataContext.")] string commandName,
        [Description("Optional command parameter.")] string? parameter = null,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the operation to complete.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default) =>
        forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.InvokeCommand,
            ToolForwarder.Parameters(
                ("controlId", controlId),
                ("commandName", commandName),
                ("parameter", parameter)),
            pid,
            pipe,
            timeoutMs,
            cancellationToken);

    [McpServerTool(Name = LuminaDiagnosticsToolNames.WaitForProperty, ReadOnly = false, Destructive = false),
     Description("Poll a control, Avalonia property, CLR property, or DataContext property until it reaches an expected value.")]
    public static Task<string> WaitForProperty(
        ToolForwarder forwarder,
        [Description("Property name to watch.")] string propertyName,
        [Description("Expected value as a string.")] string expectedValue,
        [Description("Optional control identifier. If omitted, diagnostics host uses the main window.")] string? controlId = null,
        [Description("Polling interval in milliseconds.")] int pollIntervalMs = 500,
        [Description("Target process ID.")] int? pid = null,
        [Description("Explicit LuminaUI.Diagnostics pipe name. Takes precedence over pid.")] string? pipe = null,
        [Description("Maximum time in milliseconds to wait for the property to match.")] int timeoutMs = LuminaDiagnosticsProtocol.DefaultTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        var transportTimeoutMs = timeoutMs > 0
            ? timeoutMs + 5_000
            : timeoutMs;

        return forwarder.ForwardAsync(
            LuminaDiagnosticsToolNames.WaitForProperty,
            ToolForwarder.Parameters(
                ("controlId", controlId),
                ("propertyName", propertyName),
                ("expectedValue", expectedValue),
                ("timeoutMs", timeoutMs),
                ("pollIntervalMs", pollIntervalMs)),
            pid,
            pipe,
            transportTimeoutMs,
            cancellationToken);
    }
}
