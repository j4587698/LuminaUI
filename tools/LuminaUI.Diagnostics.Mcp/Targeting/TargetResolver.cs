using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Mcp.Targeting;

public static class TargetResolver
{
    public static TargetResolution Resolve(TargetOptions options)
    {
        var timeoutMs = options.TimeoutMs > 0
            ? options.TimeoutMs
            : LuminaDiagnosticsProtocol.DefaultTimeoutMs;

        if (!string.IsNullOrWhiteSpace(options.DiagnosticsPipeName))
            return TargetResolution.ForPipe(options.DiagnosticsPipeName.Trim(), timeoutMs);

        if (options.ProcessId.HasValue)
        {
            if (options.ProcessId.Value <= 0)
            {
                return TargetResolution.Fail(
                    DiagnosticErrorCode.InvalidRequest,
                    "Process ID must be a positive integer.");
            }

            return TargetResolution.ForPipe(DiagnosticsPipeName.ForProcess(options.ProcessId.Value), timeoutMs);
        }

        return TargetResolution.Fail(
            DiagnosticErrorCode.InvalidRequest,
            "No LuminaUI.Diagnostics target was specified. Provide a pid or pipe parameter.");
    }
}
