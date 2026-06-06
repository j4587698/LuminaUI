using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class ControlSearchHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    public ControlSearchHandler(
        IUiThreadInvoker invoker,
        Func<IReadOnlyList<Control>>? getRoots = null)
    {
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _getRoots = getRoots ?? InspectionRequestHelpers.GetCurrentWindowRoots;
    }

    public string Method => LuminaDiagnosticsToolNames.FindControl;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(HandleOnUiThread(request)),
            cancellationToken);

    private DiagnosticResponse HandleOnUiThread(DiagnosticRequest request)
    {
        var name = InspectionRequestHelpers.GetString(request.Parameters, "name");
        var typeName = InspectionRequestHelpers.GetString(request.Parameters, "typeName");
        var text = InspectionRequestHelpers.GetString(request.Parameters, "text");
        var maxResults = Math.Clamp(
            InspectionRequestHelpers.GetInt(request.Parameters, "maxResults", defaultValue: 20),
            1,
            200);

        var results = new JsonArray();
        var roots = _getRoots();
        for (var rootIndex = 0; rootIndex < roots.Count && results.Count < maxResults; rootIndex++)
        {
            foreach (var control in AvaloniaControlResolver.EnumerateControls(roots[rootIndex]))
            {
                if (!Matches(control, name, typeName, text))
                    continue;

                results.Add(CreateResult(control, rootIndex));
                if (results.Count >= maxResults)
                    break;
            }
        }

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["count"] = results.Count,
                ["maxResults"] = maxResults,
                ["results"] = results
            });
    }

    private static bool Matches(
        Control control,
        string? name,
        string? typeName,
        string? text)
    {
        if (!Contains(control.Name, name))
            return false;

        var controlType = control.GetType();
        if (!Contains(controlType.Name, typeName) && !Contains(controlType.FullName, typeName))
            return false;

        if (!Contains(GetDisplayText(control), text))
            return false;

        return true;
    }

    private static bool Contains(
        string? value,
        string? filter) =>
        string.IsNullOrWhiteSpace(filter)
        || (!string.IsNullOrEmpty(value)
            && value.Contains(filter, StringComparison.OrdinalIgnoreCase));

    private static JsonObject CreateResult(
        Control control,
        int rootIndex) =>
        new()
        {
            ["rootIndex"] = rootIndex,
            ["controlId"] = CreateControlId(control),
            ["type"] = control.GetType().Name,
            ["fullType"] = control.GetType().FullName,
            ["name"] = control.Name,
            ["text"] = GetDisplayText(control),
            ["bounds"] = FormatRect(control.Bounds),
            ["isVisible"] = control.IsVisible,
            ["isEnabled"] = control.IsEnabled
        };

    private static string CreateControlId(Control control) =>
        string.IsNullOrWhiteSpace(control.Name)
            ? control.GetType().Name
            : $"#{control.Name}";

    internal static string? GetDisplayText(Control control) =>
        control switch
        {
            TextBox textBox => textBox.Text,
            TextBlock textBlock => textBlock.Text,
            ContentControl { Content: not null } contentControl => contentControl.Content.ToString(),
            _ => null
        };

    private static JsonObject FormatRect(Rect bounds) =>
        new()
        {
            ["x"] = bounds.X,
            ["y"] = bounds.Y,
            ["width"] = bounds.Width,
            ["height"] = bounds.Height
        };
}
