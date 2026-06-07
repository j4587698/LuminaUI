using System.Reflection;

namespace LuminaUI.Mcp;

internal static class PackageVersionCatalog
{
    public static string LuminaUIVersion { get; } = ReadMetadata("LuminaUIVersion");
    public static string DiagnosticsVersion { get; } = ReadMetadata("LuminaUIDiagnosticsVersion");
    public static string DiagnosticsMcpVersion { get; } = ReadMetadata("LuminaUIDiagnosticsMcpVersion");

    public static string? Normalize(string? version) =>
        string.IsNullOrWhiteSpace(version) ? null : version.Trim();

    public static string AddPackageCommand(string project, string packageName) =>
        $"dotnet add \"{project}\" package {packageName}";

    public static string DotnetToolInstallCommand(string packageName, string commandName) =>
        $"dotnet tool install --global {packageName} # exposes {commandName}";

    private static string ReadMetadata(string key)
    {
        return typeof(PackageVersionCatalog).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key.Equals(key, StringComparison.Ordinal))
            ?.Value
            ?.Trim() ?? "latest";
    }
}
