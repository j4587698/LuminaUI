using System.Collections;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Serialization;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class ResourceInspectionHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly ValueFormatter _formatter;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    public ResourceInspectionHandler(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        ValueFormatter? formatter = null,
        Func<IReadOnlyList<Control>>? getRoots = null)
    {
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _controlResolver = controlResolver ?? new AvaloniaControlResolver();
        _formatter = formatter ?? new ValueFormatter();
        _getRoots = getRoots ?? InspectionRequestHelpers.GetCurrentWindowRoots;
    }

    public string Method => LuminaDiagnosticsToolNames.GetResources;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(HandleOnUiThread(request)),
            cancellationToken);

    private DiagnosticResponse HandleOnUiThread(DiagnosticRequest request)
    {
        var controlId = InspectionRequestHelpers.GetString(request.Parameters, "controlId");
        if (string.IsNullOrWhiteSpace(controlId))
        {
            return DiagnosticResponse.Ok(
                request.Id,
                CreateResponse(source: "application", Application.Current?.Resources));
        }

        var lookup = InspectionRequestHelpers.ResolveControl(request, _getRoots(), _controlResolver);
        if (!lookup.Success)
            return lookup.Response!;

        return DiagnosticResponse.Ok(
            request.Id,
            CreateResponse(source: "control", lookup.Control!.Resources));
    }

    private JsonObject CreateResponse(
        string source,
        IResourceDictionary? resources)
    {
        var items = new JsonArray();
        if (resources is IEnumerable enumerable)
        {
            foreach (var entry in enumerable)
                items.Add(SerializeResource(entry));
        }

        return new JsonObject
        {
            ["source"] = source,
            ["count"] = items.Count,
            ["resources"] = items
        };
    }

    private JsonObject SerializeResource(object entry)
    {
        var key = GetEntryMember(entry, "Key");
        var value = GetEntryMember(entry, "Value");

        return new JsonObject
        {
            ["key"] = key?.ToString(),
            ["keyType"] = key?.GetType().FullName,
            ["value"] = _formatter.Format(
                value,
                new ValueFormatOptions { MaxStringLength = 160, MaxEnumerableItems = 5, MaxDepth = 0 })
        };
    }

    private static object? GetEntryMember(
        object entry,
        string name) =>
        entry.GetType().GetProperty(name)?.GetValue(entry);
}
