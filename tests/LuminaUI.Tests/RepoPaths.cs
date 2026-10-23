using System.Runtime.CompilerServices;

namespace LuminaUI.Tests;

/// <summary>
/// Locates the repository on disk so the static contract tests can read the XAML and RESX sources
/// directly instead of duplicating their content into the test project.
/// </summary>
internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string CoreThemeDirectory => Path.Combine(Root, "src", "LuminaUI");

    public static string LocalizationDirectory => Path.Combine(CoreThemeDirectory, "Localization");

    private static string FindRoot([CallerFilePath] string thisFile = "")
    {
        DirectoryInfo? directory = new(Path.GetDirectoryName(thisFile)!);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LuminaUI.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root above '{thisFile}'. Expected to find LuminaUI.slnx.");
    }
}
