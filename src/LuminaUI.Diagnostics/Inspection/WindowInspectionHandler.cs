using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class WindowInspectionHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly Func<IReadOnlyList<Window>> _getWindows;

    public WindowInspectionHandler(
        IUiThreadInvoker invoker,
        Func<IReadOnlyList<Window>>? getWindows = null)
    {
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _getWindows = getWindows ?? GetCurrentWindows;
    }

    public string Method => LuminaDiagnosticsToolNames.ListWindows;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(DiagnosticResponse.Ok(request.Id, CreateResponse(_getWindows()))),
            cancellationToken);

    internal static IReadOnlyList<Window> GetCurrentWindows()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.ToArray();

        return [];
    }

    private static JsonObject CreateResponse(IReadOnlyList<Window> windows)
    {
        var items = new JsonArray();
        for (var index = 0; index < windows.Count; index++)
            items.Add(SerializeWindow(windows[index], index));

        return new JsonObject
        {
            ["count"] = windows.Count,
            ["windows"] = items
        };
    }

    private static JsonObject SerializeWindow(
        Window window,
        int index) =>
        new()
        {
            ["index"] = index,
            ["title"] = window.Title,
            ["type"] = window.GetType().Name,
            ["fullType"] = window.GetType().FullName,
            ["name"] = window.Name,
            ["clientSize"] = FormatSize(window.ClientSize),
            ["isVisible"] = window.IsVisible,
            ["isActive"] = window.IsActive,
            ["state"] = window.WindowState.ToString(),
            ["position"] = FormatPixelPoint(window.Position)
        };

    private static JsonObject FormatSize(Size size) =>
        new()
        {
            ["width"] = size.Width,
            ["height"] = size.Height
        };

    private static JsonObject FormatPixelPoint(PixelPoint point) =>
        new()
        {
            ["x"] = point.X,
            ["y"] = point.Y
        };
}
