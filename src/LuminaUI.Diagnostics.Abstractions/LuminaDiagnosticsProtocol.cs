using System.Reflection;

namespace LuminaUI.Diagnostics.Abstractions;

public static class LuminaDiagnosticsProtocol
{
    public const string Name = "LuminaUI.Diagnostics";
    public static string Version { get; } = ResolveVersion();
    public const int DefaultTimeoutMs = 30_000;

    private static string ResolveVersion()
    {
        var informationalVersion = typeof(LuminaDiagnosticsProtocol).Assembly
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
