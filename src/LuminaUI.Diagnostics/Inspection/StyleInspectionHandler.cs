using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Styling;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class StyleInspectionHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    public StyleInspectionHandler(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        Func<IReadOnlyList<Control>>? getRoots = null)
    {
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _controlResolver = controlResolver ?? new AvaloniaControlResolver();
        _getRoots = getRoots ?? InspectionRequestHelpers.GetCurrentWindowRoots;
    }

    public string Method => LuminaDiagnosticsToolNames.GetAppliedStyles;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(HandleOnUiThread(request)),
            cancellationToken);

    private DiagnosticResponse HandleOnUiThread(DiagnosticRequest request)
    {
        var lookup = InspectionRequestHelpers.ResolveControl(request, _getRoots(), _controlResolver);
        if (!lookup.Success)
            return lookup.Response!;

        var control = lookup.Control!;
        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["rootIndex"] = lookup.RootIndex,
                ["controlId"] = lookup.ControlId,
                ["type"] = control.GetType().FullName,
                ["name"] = control.Name,
                ["classes"] = ReadClasses(control),
                ["styleKey"] = control.StyleKey?.ToString(),
                ["templatedParentType"] = control.TemplatedParent?.GetType().FullName,
                ["localStyles"] = ReadLocalStyles(control),
                ["setters"] = new JsonArray()
            });
    }

    private static JsonArray ReadClasses(Control control)
    {
        var classes = new JsonArray();
        foreach (var styleClass in control.Classes)
            classes.Add(styleClass);

        return classes;
    }

    private static JsonArray ReadLocalStyles(Control control)
    {
        var styles = new JsonArray();
        if (control is not IStyleHost { IsStylesInitialized: true } styleHost)
            return styles;

        foreach (var style in styleHost.Styles)
        {
            styles.Add(new JsonObject
            {
                ["type"] = style.GetType().FullName,
                ["summary"] = style.ToString()
            });
        }

        return styles;
    }
}
