namespace LuminaUI.Diagnostics.Mcp.Targeting;

public sealed record TargetOptions(int? ProcessId, string? DiagnosticsPipeName, int TimeoutMs);
