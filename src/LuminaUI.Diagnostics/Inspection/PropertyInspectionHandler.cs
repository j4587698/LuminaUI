using System.Reflection;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Serialization;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class PropertyInspectionHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly ValueFormatter _formatter;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    public PropertyInspectionHandler(
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

    public string Method => LuminaDiagnosticsToolNames.GetControlProperties;

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

        var propertyNames = InspectionRequestHelpers.GetStringArray(request.Parameters, "propertyNames");
        var filters = propertyNames.Length == 0
            ? null
            : new HashSet<string>(propertyNames, StringComparer.Ordinal);

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["rootIndex"] = lookup.RootIndex,
                ["controlId"] = lookup.ControlId,
                ["type"] = lookup.Control!.GetType().FullName,
                ["name"] = lookup.Control.Name,
                ["avaloniaProperties"] = ReadAvaloniaProperties(lookup.Control, filters),
                ["clrProperties"] = filters is null
                    ? new JsonArray()
                    : ReadClrProperties(lookup.Control, filters)
            });
    }

    private JsonArray ReadAvaloniaProperties(
        Control control,
        HashSet<string>? filters)
    {
        var properties = AvaloniaPropertyRegistry.Instance
            .GetRegistered(control)
            .Where(property => filters is null || filters.Contains(property.Name))
            .OrderBy(property => property.Name, StringComparer.Ordinal);

        var result = new JsonArray();
        foreach (var property in properties)
            result.Add(ReadAvaloniaProperty(control, property));

        return result;
    }

    private JsonObject ReadAvaloniaProperty(
        Control control,
        AvaloniaProperty property)
    {
        var json = new JsonObject
        {
            ["name"] = property.Name,
            ["ownerType"] = property.OwnerType.FullName,
            ["propertyType"] = property.PropertyType.FullName,
            ["isAttached"] = property.IsAttached,
            ["isDirect"] = property.IsDirect,
            ["isSet"] = control.IsSet(property)
        };

        try
        {
            json["value"] = _formatter.Format(
                control.GetValue(property),
                new ValueFormatOptions { MaxStringLength = 160, MaxEnumerableItems = 10, MaxDepth = 1 });
        }
        catch (Exception ex)
        {
            json["error"] = ex.Message;
        }

        return json;
    }

    private JsonArray ReadClrProperties(
        Control control,
        HashSet<string> filters)
    {
        var result = new JsonArray();
        var type = control.GetType();

        foreach (var propertyName in filters.OrderBy(name => name, StringComparer.Ordinal))
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);

            if (property is null || property.GetIndexParameters().Length > 0)
                continue;

            var json = new JsonObject
            {
                ["name"] = property.Name,
                ["declaringType"] = property.DeclaringType?.FullName,
                ["propertyType"] = property.PropertyType.FullName
            };

            try
            {
                json["value"] = _formatter.Format(
                    property.GetValue(control),
                    new ValueFormatOptions { MaxStringLength = 160, MaxEnumerableItems = 10, MaxDepth = 1 });
            }
            catch (Exception ex)
            {
                json["error"] = ex.InnerException?.Message ?? ex.Message;
            }

            result.Add(json);
        }

        return result;
    }
}
