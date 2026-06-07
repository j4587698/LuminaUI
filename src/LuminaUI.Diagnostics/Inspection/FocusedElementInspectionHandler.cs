using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class FocusedElementInspectionHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly Func<IReadOnlyList<Window>> _getWindows;

    public FocusedElementInspectionHandler(
        IUiThreadInvoker invoker,
        Func<IReadOnlyList<Window>>? getWindows = null)
    {
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _getWindows = getWindows ?? WindowInspectionHandler.GetCurrentWindows;
    }

    public string Method => LuminaDiagnosticsToolNames.GetFocusedElement;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(HandleOnUiThread(request)),
            cancellationToken);

    private DiagnosticResponse HandleOnUiThread(DiagnosticRequest request)
    {
        var windows = _getWindows();
        for (var index = 0; index < windows.Count; index++)
        {
            var focused = windows[index].FocusManager?.GetFocusedElement();
            if (focused is not null)
                return DiagnosticResponse.Ok(request.Id, CreateFocusedResponse(focused, index));
        }

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject { ["focused"] = false });
    }

    private static JsonObject CreateFocusedResponse(
        object focused,
        int windowIndex)
    {
        var type = focused.GetType();
        var json = new JsonObject
        {
            ["focused"] = true,
            ["windowIndex"] = windowIndex,
            ["type"] = type.Name,
            ["fullType"] = type.FullName
        };

        if (focused is Control control)
        {
            json["name"] = control.Name;
            json["text"] = ControlSearchHandler.GetDisplayText(control);
            json["bounds"] = new JsonObject
            {
                ["x"] = control.Bounds.X,
                ["y"] = control.Bounds.Y,
                ["width"] = control.Bounds.Width,
                ["height"] = control.Bounds.Height
            };
            json["isVisible"] = control.IsVisible;
            json["isEnabled"] = control.IsEnabled;
        }

        return json;
    }
}
