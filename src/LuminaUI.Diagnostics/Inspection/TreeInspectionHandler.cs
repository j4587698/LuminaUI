using System.Text.Json.Nodes;
using Avalonia.Controls;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class TreeInspectionHandler : IDiagnosticToolHandler
{
    private readonly TreeKind _kind;
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly VisualNodeSerializer _serializer;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    private TreeInspectionHandler(
        TreeKind kind,
        IUiThreadInvoker invoker,
        IControlResolver controlResolver,
        VisualNodeSerializer? serializer,
        Func<IReadOnlyList<Control>>? getRoots)
    {
        _kind = kind;
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _controlResolver = controlResolver ?? throw new ArgumentNullException(nameof(controlResolver));
        _serializer = serializer ?? new VisualNodeSerializer();
        _getRoots = getRoots ?? InspectionRequestHelpers.GetCurrentWindowRoots;
    }

    public string Method => _kind == TreeKind.Visual
        ? LuminaDiagnosticsToolNames.GetVisualTree
        : LuminaDiagnosticsToolNames.GetLogicalTree;

    public static TreeInspectionHandler VisualTree(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        VisualNodeSerializer? serializer = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(
            TreeKind.Visual,
            invoker,
            controlResolver ?? new AvaloniaControlResolver(),
            serializer,
            getRoots);

    public static TreeInspectionHandler LogicalTree(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        VisualNodeSerializer? serializer = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(
            TreeKind.Logical,
            invoker,
            controlResolver ?? new AvaloniaControlResolver(),
            serializer,
            getRoots);

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            _ => Task.FromResult(HandleOnUiThread(request)),
            cancellationToken);

    private DiagnosticResponse HandleOnUiThread(DiagnosticRequest request)
    {
        var roots = _getRoots();
        var windowIndex = InspectionRequestHelpers.GetInt(request.Parameters, "windowIndex", defaultValue: 0);

        if (windowIndex < 0 || windowIndex >= roots.Count)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TargetNotFound,
                $"Window index {windowIndex} was not found.",
                new JsonObject { ["windowIndex"] = windowIndex });
        }

        var root = ResolveRoot(request, roots[windowIndex]);
        if (!root.Success)
            return root.Response!;

        var maxDepth = InspectionRequestHelpers.GetInt(request.Parameters, "maxDepth", defaultValue: 10);
        var tree = _kind == TreeKind.Visual
            ? _serializer.SerializeVisualTree(root.Control!, maxDepth)
            : _serializer.SerializeLogicalTree(root.Control!, maxDepth);

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["treeType"] = _kind == TreeKind.Visual ? "visual" : "logical",
                ["windowIndex"] = windowIndex,
                ["maxDepth"] = Math.Clamp(maxDepth, 0, 50),
                ["root"] = tree
            });
    }

    private RootResolution ResolveRoot(
        DiagnosticRequest request,
        Control rootControl)
    {
        var controlId = InspectionRequestHelpers.GetString(request.Parameters, "controlId");
        if (string.IsNullOrWhiteSpace(controlId))
            return RootResolution.Found(rootControl);

        if (!ControlIdentifierParser.TryParse(controlId, out var identifier, out var error))
        {
            return RootResolution.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    error!,
                    new JsonObject { ["controlId"] = controlId }));
        }

        var resolution = _controlResolver.Resolve(rootControl, identifier);
        if (resolution.Found)
            return RootResolution.Found(resolution.Control!);

        return RootResolution.Failed(
            DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TargetNotFound,
                resolution.ErrorMessage!,
                new JsonObject { ["controlId"] = controlId }));
    }

    private enum TreeKind
    {
        Visual,
        Logical
    }

    private sealed record RootResolution(
        bool Success,
        Control? Control,
        DiagnosticResponse? Response)
    {
        public static RootResolution Found(Control control) =>
            new(Success: true, control, Response: null);

        public static RootResolution Failed(DiagnosticResponse response) =>
            new(Success: false, Control: null, response);
    }
}
