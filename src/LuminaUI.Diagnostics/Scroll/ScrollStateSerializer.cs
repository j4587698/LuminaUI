using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Diagnostics.Scroll;

public sealed class ScrollStateSerializer
{
    public JsonObject Serialize(ScrollViewer scrollViewer)
    {
        ArgumentNullException.ThrowIfNull(scrollViewer);

        var maxX = GetMaxOffset(scrollViewer.Extent.Width, scrollViewer.Viewport.Width);
        var maxY = GetMaxOffset(scrollViewer.Extent.Height, scrollViewer.Viewport.Height);

        return new JsonObject
        {
            ["offset"] = Vector(scrollViewer.Offset),
            ["viewport"] = Size(scrollViewer.Viewport),
            ["extent"] = Size(scrollViewer.Extent),
            ["maxOffset"] = new JsonObject
            {
                ["x"] = maxX,
                ["y"] = maxY
            },
            ["canScrollHorizontally"] = maxX > 0,
            ["canScrollVertically"] = maxY > 0,
            ["isAtLeft"] = scrollViewer.Offset.X <= 0,
            ["isAtRight"] = scrollViewer.Offset.X >= maxX - 0.5,
            ["isAtTop"] = scrollViewer.Offset.Y <= 0,
            ["isAtBottom"] = scrollViewer.Offset.Y >= maxY - 0.5
        };
    }

    public static double GetMaxOffset(
        double extent,
        double viewport) =>
        Math.Max(0, extent - viewport);

    private static JsonObject Vector(Vector vector) =>
        new()
        {
            ["x"] = vector.X,
            ["y"] = vector.Y
        };

    private static JsonObject Size(Size size) =>
        new()
        {
            ["width"] = size.Width,
            ["height"] = size.Height
        };
}
