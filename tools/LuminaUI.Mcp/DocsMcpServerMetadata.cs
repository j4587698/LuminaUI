using System.Reflection;

namespace LuminaUI.Mcp;

internal static class DocsMcpServerMetadata
{
    public const string Name = "LuminaUI.Mcp";
    public static string Version { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var informationalVersion = typeof(DocsMcpServerMetadata).Assembly
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
