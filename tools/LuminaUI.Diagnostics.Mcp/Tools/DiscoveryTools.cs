using System.ComponentModel;
using LuminaUI.Diagnostics.Abstractions;
using ModelContextProtocol.Server;

namespace LuminaUI.Diagnostics.Mcp.Tools;

[McpServerToolType]
public sealed class DiscoveryTools
{
    [McpServerTool(Name = LuminaDiagnosticsToolNames.DiscoverApps, ReadOnly = true, Destructive = false),
     Description("Reports how to target LuminaUI.Diagnostics-enabled Avalonia applications without discovery files.")]
    public static DiscoveryResult DiscoverApps() =>
        new(
            SupportsDiscoveryFiles: false,
            DiagnosticsPipeNamePattern: $"{DiagnosticsPipeName.Prefix}-{{pid}}",
            RequiredTargetParameters: ["pid", "pipe"],
            Message: "LuminaUI.Diagnostics does not create discovery files. Provide a pid to use the default pipe name, or provide an explicit pipe name.",
            ProtocolName: LuminaDiagnosticsProtocol.Name,
            ProtocolVersion: LuminaDiagnosticsProtocol.Version);
}

public sealed record DiscoveryResult(
    bool SupportsDiscoveryFiles,
    string DiagnosticsPipeNamePattern,
    IReadOnlyList<string> RequiredTargetParameters,
    string Message,
    string ProtocolName,
    string ProtocolVersion);
