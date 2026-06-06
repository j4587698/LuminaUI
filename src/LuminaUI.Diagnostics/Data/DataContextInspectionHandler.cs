using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Inspection;
using LuminaUI.Diagnostics.Serialization;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Data;

public sealed class DataContextInspectionHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly ValueFormatter _formatter;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    public DataContextInspectionHandler(
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

    public string Method => LuminaDiagnosticsToolNames.GetDataContext;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(HandleOnUiThread(request)),
            cancellationToken);

    private DiagnosticResponse HandleOnUiThread(DiagnosticRequest request)
    {
        var root = ResolveTargetControl(request);
        if (!root.Success)
            return root.Response!;

        var control = root.Control!;
        var dataContext = control.DataContext;
        var json = new JsonObject
        {
            ["rootIndex"] = root.RootIndex,
            ["controlId"] = root.ControlId,
            ["controlType"] = control.GetType().FullName,
            ["controlName"] = control.Name,
            ["dataContextType"] = dataContext?.GetType().FullName,
            ["properties"] = dataContext is null
                ? new JsonArray()
                : ReadProperties(dataContext)
        };

        var expandProperty = InspectionRequestHelpers.GetString(request.Parameters, "expandProperty");
        if (!string.IsNullOrWhiteSpace(expandProperty) && dataContext is not null)
        {
            json["expandedProperty"] = ExpandProperty(
                dataContext,
                expandProperty,
                Math.Clamp(
                    InspectionRequestHelpers.GetInt(request.Parameters, "maxItems", defaultValue: 50),
                    0,
                    500));
        }

        return DiagnosticResponse.Ok(request.Id, json);
    }

    private ControlLookup ResolveTargetControl(DiagnosticRequest request)
    {
        var controlId = InspectionRequestHelpers.GetString(request.Parameters, "controlId");
        if (!string.IsNullOrWhiteSpace(controlId))
            return InspectionRequestHelpers.ResolveControl(request, _getRoots(), _controlResolver);

        var roots = _getRoots();
        if (roots.Count == 0)
        {
            return ControlLookup.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.TargetNotFound,
                    "No root controls are available."));
        }

        return ControlLookup.Found(roots[0], rootIndex: 0, controlId: "");
    }

    private JsonArray ReadProperties(object dataContext)
    {
        var result = new JsonArray();
        foreach (var property in GetReadableProperties(dataContext.GetType()))
            result.Add(ReadProperty(dataContext, property));

        return result;
    }

    private JsonObject ReadProperty(
        object target,
        PropertyInfo property)
    {
        var json = new JsonObject
        {
            ["name"] = property.Name,
            ["propertyType"] = property.PropertyType.FullName
        };

        try
        {
            json["value"] = _formatter.Format(
                property.GetValue(target),
                new ValueFormatOptions { MaxStringLength = 160, MaxEnumerableItems = 10, MaxDepth = 1 });
        }
        catch (Exception ex)
        {
            json["error"] = ex.InnerException?.Message ?? ex.Message;
        }

        return json;
    }

    private JsonObject ExpandProperty(
        object dataContext,
        string propertyName,
        int maxItems)
    {
        var property = GetReadableProperties(dataContext.GetType())
            .FirstOrDefault(candidate => string.Equals(candidate.Name, propertyName, StringComparison.Ordinal));

        if (property is null)
        {
            return new JsonObject
            {
                ["name"] = propertyName,
                ["error"] = "Property was not found."
            };
        }

        object? value;
        try
        {
            value = property.GetValue(dataContext);
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["name"] = propertyName,
                ["error"] = ex.InnerException?.Message ?? ex.Message
            };
        }

        if (value is not IEnumerable enumerable || value is string)
        {
            return new JsonObject
            {
                ["name"] = propertyName,
                ["error"] = "Property is not an expandable collection."
            };
        }

        var items = new JsonArray();
        var truncated = false;
        var index = 0;
        foreach (var item in enumerable)
        {
            if (index >= maxItems)
            {
                truncated = true;
                break;
            }

            items.Add(
                _formatter.Format(
                    item,
                    new ValueFormatOptions { MaxStringLength = 160, MaxEnumerableItems = 5, MaxDepth = 1 }));
            index++;
        }

        return new JsonObject
        {
            ["name"] = propertyName,
            ["count"] = items.Count,
            ["maxItems"] = maxItems,
            ["truncated"] = truncated,
            ["items"] = items
        };
    }

    private static IEnumerable<PropertyInfo> GetReadableProperties(Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0 && property.GetMethod is not null)
            .OrderBy(property => property.Name, StringComparer.Ordinal);
}
