using System.Collections;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Inspection;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Scroll;

public sealed class ScrollDiagnosticsHandler : IDiagnosticToolHandler
{
    private const double LineDelta = 20;

    private readonly ScrollDiagnosticKind _kind;
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly ScrollStateSerializer _serializer;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    private ScrollDiagnosticsHandler(
        ScrollDiagnosticKind kind,
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        ScrollStateSerializer? serializer = null,
        Func<IReadOnlyList<Control>>? getRoots = null)
    {
        _kind = kind;
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _controlResolver = controlResolver ?? new AvaloniaControlResolver();
        _serializer = serializer ?? new ScrollStateSerializer();
        _getRoots = getRoots ?? InspectionRequestHelpers.GetCurrentWindowRoots;
    }

    public string Method => _kind == ScrollDiagnosticKind.Scroll
        ? LuminaDiagnosticsToolNames.Scroll
        : LuminaDiagnosticsToolNames.GetScrollableItems;

    public static ScrollDiagnosticsHandler Scroll(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        ScrollStateSerializer? serializer = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(ScrollDiagnosticKind.Scroll, invoker, controlResolver, serializer, getRoots);

    public static ScrollDiagnosticsHandler ScrollableItems(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        ScrollStateSerializer? serializer = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(ScrollDiagnosticKind.ScrollableItems, invoker, controlResolver, serializer, getRoots);

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(HandleOnUiThread(request)),
            cancellationToken);

    private DiagnosticResponse HandleOnUiThread(DiagnosticRequest request) =>
        _kind == ScrollDiagnosticKind.Scroll
            ? Scroll(request)
            : GetScrollableItems(request);

    private DiagnosticResponse Scroll(DiagnosticRequest request)
    {
        var target = ResolveOptionalScrollTarget(request);
        if (!target.Success)
            return target.Response!;

        var scrollViewer = FindScrollViewer(target.Control!);
        if (scrollViewer is null)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                "Target control is not a ScrollViewer and does not contain or belong to a ScrollViewer.");
        }

        var operation = ApplyScrollOperation(request, scrollViewer, target.Control!);
        if (!operation.Success)
            return operation.Response!;

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["status"] = operation.Name == "state" ? "state" : "scrolled",
                ["operation"] = operation.Name,
                ["scrollState"] = _serializer.Serialize(scrollViewer)
            });
    }

    private DiagnosticResponse GetScrollableItems(DiagnosticRequest request)
    {
        var lookup = ResolveControlId(request, "controlId");
        if (!lookup.Success)
            return lookup.Response!;

        var snapshot = CreateScrollableItemsSnapshot(lookup.Control!);
        if (snapshot is null)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                "Target control is not an ItemsControl, DataGrid, TreeDataGrid, and does not contain a supported items control.");
        }

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["itemCount"] = snapshot.ItemCount,
                ["realizedContainerCount"] = snapshot.RealizedContainers.Count,
                ["realizedContainers"] = snapshot.RealizedContainers,
                ["virtualization"] = new JsonObject
                {
                    ["isVirtualizing"] = snapshot.IsVirtualizing,
                    ["panelType"] = snapshot.PanelType
                },
                ["scrollState"] = snapshot.ScrollViewer is null ? null : _serializer.Serialize(snapshot.ScrollViewer)
            });
    }

    private ScrollOperationResult ApplyScrollOperation(
        DiagnosticRequest request,
        ScrollViewer scrollViewer,
        Control target)
    {
        var targetControlId = InspectionRequestHelpers.GetString(request.Parameters, "targetControlId");
        var hasItemIndex = HasParameter(request.Parameters, "itemIndex");
        if (hasItemIndex)
            return ScrollItemIntoView(request, target, targetControlId);

        if (!string.IsNullOrWhiteSpace(targetControlId))
            return BringControlIntoView(request, targetControlId);

        var edge = InspectionRequestHelpers.GetString(request.Parameters, "edge");
        if (!string.IsNullOrWhiteSpace(edge))
            return ScrollToEdge(request, scrollViewer, edge);

        var x = InspectionRequestHelpers.GetDouble(request.Parameters, "x");
        var y = InspectionRequestHelpers.GetDouble(request.Parameters, "y");
        if (x.HasValue || y.HasValue)
        {
            SetOffset(
                scrollViewer,
                x ?? scrollViewer.Offset.X,
                y ?? scrollViewer.Offset.Y);
            return ScrollOperationResult.Applied("absolute");
        }

        var deltaX = InspectionRequestHelpers.GetDouble(request.Parameters, "deltaX");
        var deltaY = InspectionRequestHelpers.GetDouble(request.Parameters, "deltaY");
        if (deltaX.HasValue || deltaY.HasValue)
            return ScrollByDelta(request, scrollViewer, deltaX ?? 0, deltaY ?? 0);

        return ScrollOperationResult.Applied("state");
    }

    private ScrollOperationResult ScrollItemIntoView(
        DiagnosticRequest request,
        Control target,
        string? targetControlId)
    {
        var itemIndex = InspectionRequestHelpers.GetInt(request.Parameters, "itemIndex", -1);
        var itemsTarget = string.IsNullOrWhiteSpace(targetControlId)
            ? ControlLookup.Found(target, rootIndex: 0, controlId: "")
            : ResolveControlId(request, "targetControlId");

        if (!itemsTarget.Success)
            return ScrollOperationResult.Failed(itemsTarget.Response!);

        var itemsControl = FindItemsControl(itemsTarget.Control!);
        if (itemsControl is null)
        {
            return ScrollOperationResult.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.UnsupportedOperation,
                    "Item scrolling requires an ItemsControl target."));
        }

        if (itemIndex < 0 || itemIndex >= itemsControl.ItemCount)
        {
            return ScrollOperationResult.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    $"Item index {itemIndex} is out of range.",
                    new JsonObject
                    {
                        ["itemIndex"] = itemIndex,
                        ["itemCount"] = itemsControl.ItemCount
                    }));
        }

        itemsControl.ScrollIntoView(itemIndex);
        return ScrollOperationResult.Applied("itemIndex");
    }

    private ScrollOperationResult BringControlIntoView(
        DiagnosticRequest request,
        string targetControlId)
    {
        var target = ResolveControlId(request, "targetControlId");
        if (!target.Success)
            return ScrollOperationResult.Failed(target.Response!);

        target.Control!.BringIntoView();
        return ScrollOperationResult.Applied("targetControl");
    }

    private static ScrollOperationResult ScrollToEdge(
        DiagnosticRequest request,
        ScrollViewer scrollViewer,
        string edge)
    {
        var maxX = ScrollStateSerializer.GetMaxOffset(scrollViewer.Extent.Width, scrollViewer.Viewport.Width);
        var maxY = ScrollStateSerializer.GetMaxOffset(scrollViewer.Extent.Height, scrollViewer.Viewport.Height);

        var target = edge.Trim().ToLowerInvariant() switch
        {
            "top" => new Vector(scrollViewer.Offset.X, 0),
            "bottom" => new Vector(scrollViewer.Offset.X, maxY),
            "left" => new Vector(0, scrollViewer.Offset.Y),
            "right" => new Vector(maxX, scrollViewer.Offset.Y),
            "home" => new Vector(0, 0),
            "end" => new Vector(maxX, maxY),
            _ => default(Vector?)
        };

        if (!target.HasValue)
        {
            return ScrollOperationResult.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    $"Unsupported edge '{edge}'."));
        }

        SetOffset(scrollViewer, target.Value.X, target.Value.Y);
        return ScrollOperationResult.Applied($"edge:{edge.Trim().ToLowerInvariant()}");
    }

    private static ScrollOperationResult ScrollByDelta(
        DiagnosticRequest request,
        ScrollViewer scrollViewer,
        double deltaX,
        double deltaY)
    {
        var unit = InspectionRequestHelpers.GetString(request.Parameters, "unit") ?? "pixels";
        var normalizedUnit = unit.Trim().ToLowerInvariant();

        var xMultiplier = normalizedUnit switch
        {
            "pixels" => 1,
            "lines" => LineDelta,
            "pages" => scrollViewer.Viewport.Width,
            _ => double.NaN
        };
        var yMultiplier = normalizedUnit switch
        {
            "pixels" => 1,
            "lines" => LineDelta,
            "pages" => scrollViewer.Viewport.Height,
            _ => double.NaN
        };

        if (double.IsNaN(xMultiplier) || double.IsNaN(yMultiplier))
        {
            return ScrollOperationResult.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    $"Unsupported scroll unit '{unit}'."));
        }

        SetOffset(
            scrollViewer,
            scrollViewer.Offset.X + deltaX * xMultiplier,
            scrollViewer.Offset.Y + deltaY * yMultiplier);

        return ScrollOperationResult.Applied($"relative:{normalizedUnit}");
    }

    private static void SetOffset(
        ScrollViewer scrollViewer,
        double x,
        double y)
    {
        var maxX = ScrollStateSerializer.GetMaxOffset(scrollViewer.Extent.Width, scrollViewer.Viewport.Width);
        var maxY = ScrollStateSerializer.GetMaxOffset(scrollViewer.Extent.Height, scrollViewer.Viewport.Height);
        scrollViewer.Offset = new Vector(
            Math.Clamp(x, 0, maxX),
            Math.Clamp(y, 0, maxY));
    }

    private ControlLookup ResolveOptionalScrollTarget(DiagnosticRequest request)
    {
        if (!string.IsNullOrWhiteSpace(InspectionRequestHelpers.GetString(request.Parameters, "controlId")))
            return ResolveControlId(request, "controlId");

        var roots = _getRoots();
        for (var rootIndex = 0; rootIndex < roots.Count; rootIndex++)
        {
            var scrollViewer = FindScrollViewer(roots[rootIndex]);
            if (scrollViewer is not null)
                return ControlLookup.Found(scrollViewer, rootIndex, "");
        }

        return ControlLookup.Failed(
            DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TargetNotFound,
                "No ScrollViewer is available."));
    }

    private ControlLookup ResolveControlId(
        DiagnosticRequest request,
        string parameterName)
    {
        var controlId = InspectionRequestHelpers.GetString(request.Parameters, parameterName);
        if (string.IsNullOrWhiteSpace(controlId))
        {
            return ControlLookup.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    $"Parameter '{parameterName}' is required."));
        }

        if (!ControlIdentifierParser.TryParse(controlId, out var identifier, out var error))
        {
            return ControlLookup.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    error!,
                    new JsonObject { [parameterName] = controlId }));
        }

        var roots = _getRoots();
        for (var rootIndex = 0; rootIndex < roots.Count; rootIndex++)
        {
            var resolution = _controlResolver.Resolve(roots[rootIndex], identifier);
            if (resolution.Found)
                return ControlLookup.Found(resolution.Control!, rootIndex, controlId);
        }

        return ControlLookup.Failed(
            DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TargetNotFound,
                $"Control '{controlId}' was not found.",
                new JsonObject { [parameterName] = controlId }));
    }

    private static ScrollViewer? FindScrollViewer(Control control)
    {
        if (control is ScrollViewer scrollViewer)
            return scrollViewer;

        return AvaloniaControlResolver.EnumerateControls(control).OfType<ScrollViewer>().FirstOrDefault()
            ?? control.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault()
            ?? control.GetLogicalAncestors().OfType<ScrollViewer>().FirstOrDefault();
    }

    private static ItemsControl? FindItemsControl(Control control)
    {
        if (control is ItemsControl itemsControl)
            return itemsControl;

        return AvaloniaControlResolver.EnumerateControls(control).OfType<ItemsControl>().FirstOrDefault();
    }

    private static ScrollableItemsSnapshot? CreateScrollableItemsSnapshot(Control control)
    {
        if (FindItemsControl(control) is { } itemsControl)
        {
            var scrollViewer = FindScrollViewer(itemsControl);
            var containers = SerializeRealizedContainers(itemsControl);
            var virtualizingPanel = AvaloniaControlResolver
                .EnumerateControls(itemsControl)
                .OfType<VirtualizingStackPanel>()
                .FirstOrDefault();
            var isCustomVirtualizing = IsVirtualizingType(itemsControl);

            return new ScrollableItemsSnapshot(
                itemsControl.ItemCount,
                containers,
                scrollViewer,
                virtualizingPanel is not null || isCustomVirtualizing,
                virtualizingPanel?.GetType().FullName ?? (isCustomVirtualizing ? itemsControl.GetType().FullName : null));
        }

        if (FindControlByFullName(control, "Avalonia.Controls.DataGrid") is { } dataGrid)
        {
            var itemCount = CountItems(
                GetPublicPropertyValue(dataGrid, "ItemsSource")
                ?? GetPublicPropertyValue(dataGrid, "Items"));
            var containers = SerializeRealizedRows(dataGrid, "Avalonia.Controls.DataGridRow", "Index");
            var rowsPresenter = FindControlByTypeName(dataGrid, "DataGridRowsPresenter");

            return new ScrollableItemsSnapshot(
                itemCount,
                containers,
                FindScrollViewer(dataGrid),
                containers.Count < itemCount,
                rowsPresenter?.GetType().FullName);
        }

        if (FindControlByFullName(control, "Avalonia.Controls.TreeDataGrid") is { } treeDataGrid)
        {
            var itemCount = CountItems(GetPublicPropertyValue(treeDataGrid, "Rows"));
            var containers = SerializeRealizedRows(treeDataGrid, "Avalonia.Controls.Primitives.TreeDataGridRow", "RowIndex");
            var rowsPresenter = FindControlByTypeName(treeDataGrid, "TreeDataGridRowsPresenter");

            return new ScrollableItemsSnapshot(
                itemCount,
                containers,
                FindScrollViewer(treeDataGrid),
                containers.Count < itemCount,
                rowsPresenter?.GetType().FullName);
        }

        return null;
    }

    private static JsonArray SerializeRealizedContainers(ItemsControl itemsControl)
    {
        var containers = new JsonArray();
        var scanCount = Math.Min(itemsControl.ItemCount, 10_000);

        for (var index = 0; index < scanCount; index++)
        {
            if (itemsControl.ContainerFromIndex(index) is not Control container)
                continue;

            containers.Add(
                new JsonObject
                {
                    ["index"] = index,
                    ["type"] = container.GetType().FullName,
                    ["name"] = container.Name,
                    ["bounds"] = FormatRect(container.Bounds)
                });
        }

        return containers;
    }

    private static JsonArray SerializeRealizedRows(
        Control root,
        string rowFullName,
        string indexPropertyName)
    {
        var containers = new JsonArray();
        foreach (var row in AvaloniaControlResolver
                     .EnumerateControls(root)
                     .Where(control => string.Equals(control.GetType().FullName, rowFullName, StringComparison.Ordinal))
                     .Take(10_000))
        {
            containers.Add(
                new JsonObject
                {
                    ["index"] = GetIntPublicPropertyValue(row, indexPropertyName) ?? containers.Count,
                    ["type"] = row.GetType().FullName,
                    ["name"] = row.Name,
                    ["bounds"] = FormatRect(row.Bounds)
                });
        }

        return containers;
    }

    private static JsonObject FormatRect(Rect bounds) =>
        new()
        {
            ["x"] = FormatDouble(bounds.X),
            ["y"] = FormatDouble(bounds.Y),
            ["width"] = FormatDouble(bounds.Width),
            ["height"] = FormatDouble(bounds.Height)
        };

    private static JsonNode? FormatDouble(double value) =>
        double.IsFinite(value) ? JsonValue.Create(value) : null;

    private static Control? FindControlByFullName(
        Control control,
        string fullName)
    {
        if (string.Equals(control.GetType().FullName, fullName, StringComparison.Ordinal))
            return control;

        return AvaloniaControlResolver
            .EnumerateControls(control)
            .FirstOrDefault(child => string.Equals(child.GetType().FullName, fullName, StringComparison.Ordinal));
    }

    private static Control? FindControlByTypeName(
        Control control,
        string typeName) =>
        AvaloniaControlResolver
            .EnumerateControls(control)
            .FirstOrDefault(child => string.Equals(child.GetType().Name, typeName, StringComparison.Ordinal));

    private static bool IsVirtualizingType(Control control) =>
        control.GetType().Name.Contains("Virtualizing", StringComparison.Ordinal);

    private static object? GetPublicPropertyValue(
        object instance,
        string propertyName) =>
        instance.GetType().GetProperty(propertyName)?.GetValue(instance);

    private static int? GetIntPublicPropertyValue(
        object instance,
        string propertyName) =>
        GetPublicPropertyValue(instance, propertyName) is int value ? value : null;

    private static int CountItems(object? value)
    {
        if (value is null)
            return 0;

        if (value is ICollection collection)
            return collection.Count;

        var countProperty = value.GetType().GetProperty("Count");
        if (countProperty?.GetValue(value) is int count)
            return count;

        if (value is not IEnumerable enumerable)
            return 0;

        var result = 0;
        foreach (var _ in enumerable)
        {
            result++;
        }

        return result;
    }

    private static bool HasParameter(
        JsonObject parameters,
        string name) =>
        parameters.TryGetPropertyValue(name, out var value) && value is not null;

    private enum ScrollDiagnosticKind
    {
        Scroll,
        ScrollableItems
    }

    private sealed record ScrollOperationResult(
        bool Success,
        string Name,
        DiagnosticResponse? Response)
    {
        public static ScrollOperationResult Applied(string name) =>
            new(Success: true, name, Response: null);

        public static ScrollOperationResult Failed(DiagnosticResponse response) =>
            new(Success: false, Name: "", response);
    }

    private sealed record ScrollableItemsSnapshot(
        int ItemCount,
        JsonArray RealizedContainers,
        ScrollViewer? ScrollViewer,
        bool IsVirtualizing,
        string? PanelType);
}
