using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics;

public sealed class LuminaUIDiagnosticsOptions
{
    public string? DiagnosticsPipeName { get; set; }

    public bool StartImmediately { get; set; } = true;

    public int DefaultTimeoutMs { get; set; } = LuminaDiagnosticsProtocol.DefaultTimeoutMs;

    public string ResolveDiagnosticsPipeName() =>
        string.IsNullOrWhiteSpace(DiagnosticsPipeName)
            ? DiagnosticsPipeNameHelper.ForCurrentProcess()
            : DiagnosticsPipeName.Trim();

    private static class DiagnosticsPipeNameHelper
    {
        public static string ForCurrentProcess() =>
            LuminaUI.Diagnostics.Abstractions.DiagnosticsPipeName.ForProcess(Environment.ProcessId);
    }
}
