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

        var itemsControl = FindItemsControl(lookup.Control!);
        if (itemsControl is null)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                "Target control is not an ItemsControl and does not contain an ItemsControl.");
        }

        var scrollViewer = FindScrollViewer(itemsControl);
        var containers = SerializeRealizedContainers(itemsControl);
        var virtualizingPanel = AvaloniaControlResolver
            .EnumerateControls(itemsControl)
            .OfType<VirtualizingStackPanel>()
            .FirstOrDefault();

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["itemCount"] = itemsControl.ItemCount,
                ["realizedContainerCount"] = containers.Count,
                ["realizedContainers"] = containers,
                ["virtualization"] = new JsonObject
                {
                    ["isVirtualizing"] = virtualizingPanel is not null,
                    ["panelType"] = virtualizingPanel?.GetType().FullName
                },
                ["scrollState"] = scrollViewer is null ? null : _serializer.Serialize(scrollViewer)
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
                    ["bounds"] = new JsonObject
                    {
                        ["x"] = container.Bounds.X,
                        ["y"] = container.Bounds.Y,
                        ["width"] = container.Bounds.Width,
                        ["height"] = container.Bounds.Height
                    }
                });
        }

        return containers;
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
}
