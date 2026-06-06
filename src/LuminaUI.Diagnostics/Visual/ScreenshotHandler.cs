using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Inspection;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Visual;

public sealed class ScreenshotHandler : IDiagnosticToolHandler
{
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    public ScreenshotHandler(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        Func<IReadOnlyList<Control>>? getRoots = null)
    {
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _controlResolver = controlResolver ?? new AvaloniaControlResolver();
        _getRoots = getRoots ?? InspectionRequestHelpers.GetCurrentWindowRoots;
    }

    public string Method => LuminaDiagnosticsToolNames.TakeScreenshot;

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
        var windowIndex = InspectionRequestHelpers.GetInt(request.Parameters, "windowIndex", 0);
        if (windowIndex < 0 || windowIndex >= roots.Count)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TargetNotFound,
                $"Window index {windowIndex} was not found.",
                new JsonObject { ["windowIndex"] = windowIndex });
        }

        var target = ResolveTarget(request, roots[windowIndex]);
        if (!target.Success)
            return target.Response!;

        var pixelSize = GetPixelSize(target.Control!);
        if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.InvalidRequest,
                "Cannot capture a screenshot for a zero-size target.",
                new JsonObject
                {
                    ["width"] = pixelSize.Width,
                    ["height"] = pixelSize.Height
                });
        }

        try
        {
            using var bitmap = new RenderTargetBitmap(pixelSize);
            bitmap.Render(target.Control!);

            using var stream = new MemoryStream();
            bitmap.Save(stream);

            return DiagnosticResponse.Ok(
                request.Id,
                new JsonObject
                {
                    ["format"] = "png",
                    ["mimeType"] = "image/png",
                    ["width"] = pixelSize.Width,
                    ["height"] = pixelSize.Height,
                    ["base64"] = Convert.ToBase64String(stream.ToArray())
                });
        }
        catch (Exception ex)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.InternalError,
                $"Screenshot capture failed: {ex.Message}");
        }
    }

    private TargetResolution ResolveTarget(
        DiagnosticRequest request,
        Control root)
    {
        var controlId = InspectionRequestHelpers.GetString(request.Parameters, "controlId");
        if (string.IsNullOrWhiteSpace(controlId))
            return TargetResolution.Found(root);

        var lookup = InspectionRequestHelpers.ResolveControl(
            request,
            [root],
            _controlResolver);

        return lookup.Success
            ? TargetResolution.Found(lookup.Control!)
            : TargetResolution.Failed(lookup.Response!);
    }

    private static PixelSize GetPixelSize(Control control)
    {
        var width = (int)Math.Ceiling(control.Bounds.Width);
        var height = (int)Math.Ceiling(control.Bounds.Height);
        return new PixelSize(width, height);
    }

    private sealed record TargetResolution(
        bool Success,
        Control? Control,
        DiagnosticResponse? Response)
    {
        public static TargetResolution Found(Control control) =>
            new(Success: true, control, Response: null);

        public static TargetResolution Failed(DiagnosticResponse response) =>
            new(Success: false, Control: null, response);
    }
}
