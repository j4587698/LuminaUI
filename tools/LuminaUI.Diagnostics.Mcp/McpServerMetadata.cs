using System.Reflection;

namespace LuminaUI.Diagnostics.Mcp;

internal static class McpServerMetadata
{
    public const string Name = "LuminaUI.Diagnostics.Mcp";
    public static string Version { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var informationalVersion = typeof(McpServerMetadata).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
            return "0.0.0";

        var metadataSeparator = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return metadataSeparator > 0
            ? informationalVersion[..metadataSeparator]
            : informationalVersion;
    }
}
