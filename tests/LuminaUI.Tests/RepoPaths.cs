using System.Runtime.CompilerServices;

namespace LuminaUI.Tests;

/// <summary>
/// Locates the repository on disk so the static contract tests can read the XAML and RESX sources
/// directly instead of duplicating their content into the test project.
/// </summary>
internal static class RepoPaths
{
    private const string Marker = "LuminaUI.slnx";

    public static string Root { get; } = FindRoot();

    public static string CoreThemeDirectory => Path.Combine(Root, "src", "LuminaUI");

    public static string LocalizationDirectory => Path.Combine(CoreThemeDirectory, "Localization");

    public static string DemoDirectory => Path.Combine(Root, "demo", "LuminaUI.Demo");

    /// <summary>
    /// Walks up from every plausible starting point until <c>LuminaUI.slnx</c> shows up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The caller file path alone is not a reliable anchor. CI builds pass
    /// <c>/p:ContinuousIntegrationBuild=true</c>, which enables deterministic source paths and
    /// rewrites every path baked into the assembly to the synthetic <c>/_/...</c> root. That
    /// directory does not exist on disk, so walking up from it finds nothing and every
    /// source-scanning test dies with a <see cref="TypeInitializationException"/>.
    /// </para>
    /// <para>
    /// The test output directory is a real path in both environments, so it is tried first; the
    /// caller file is kept as a fallback for runs whose output was relocated outside the repo.
    /// </para>
    /// </remarks>
    private static string FindRoot([CallerFilePath] string thisFile = "")
    {
        string[] starts = [AppContext.BaseDirectory, Path.GetDirectoryName(thisFile) ?? string.Empty];

        foreach (string start in starts)
        {
            if (start.Length == 0 || !Directory.Exists(start))
            {
                continue;
            }

            for (DirectoryInfo? directory = new(start); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, Marker)))
                {
                    return directory.FullName;
                }
            }
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root (the directory holding {Marker}) by walking up from " +
            $"{string.Join(", ", starts.Select(static path => $"'{path}'"))}.");
    }
}
