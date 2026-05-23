using System;
using System.IO;
using System.Text;

namespace LuminaUI.Demo;

internal static class ShowcaseSourceProvider
{
    private const string ResourceRoot = "LuminaUI.Demo.";

    public static string ReadSource(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var normalizedPath = relativePath.Replace('\\', '/');
        var resourceName = ResourceRoot + normalizedPath.Replace('/', '.');

        try
        {
            using var stream = typeof(ShowcaseSourceProvider).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return $"Unable to load source resource: {normalizedPath}{Environment.NewLine}Manifest resource not found: {resourceName}";
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            return $"Unable to load source resource: {normalizedPath}{Environment.NewLine}{ex.Message}";
        }
    }
}
