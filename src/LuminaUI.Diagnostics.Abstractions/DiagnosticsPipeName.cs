namespace LuminaUI.Diagnostics.Abstractions;

public static class DiagnosticsPipeName
{
    public const string Prefix = "lumina-ui-diagnostics";

    public static string ForProcess(int processId)
    {
        if (processId <= 0)
            throw new ArgumentOutOfRangeException(nameof(processId), "Process ID must be a positive integer.");

        return $"{Prefix}-{processId}";
    }
}
