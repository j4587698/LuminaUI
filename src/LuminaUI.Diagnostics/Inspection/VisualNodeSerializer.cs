using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using LuminaUI.Diagnostics.Serialization;

namespace LuminaUI.Diagnostics.Inspection;

public sealed class VisualNodeSerializer
{
    private readonly ValueFormatter _formatter;

    public VisualNodeSerializer(ValueFormatter? formatter = null)
    {
        _formatter = formatter ?? new ValueFormatter();
    }

    public JsonObject SerializeVisualTree(
        Control root,
        int maxDepth) =>
        SerializeVisual(
            root,
            depth: 0,
            NormalizeMaxDepth(maxDepth));

    public JsonObject SerializeLogicalTree(
        Control root,
        int maxDepth) =>
        SerializeLogical(
            root,
            depth: 0,
            NormalizeMaxDepth(maxDepth));

    private JsonObject SerializeVisual(
        Avalonia.Visual visual,
        int depth,
        int maxDepth)
    {
        var children = visual.GetVisualChildren().ToArray();
        var json = CreateNode(visual);
        json["childCount"] = children.Length;

        if (depth >= maxDepth)
            return json;

        var childNodes = new JsonArray();
        foreach (var child in children)
            childNodes.Add(SerializeVisual(child, depth + 1, maxDepth));

        json["children"] = childNodes;
        return json;
    }

    private JsonObject SerializeLogical(
        ILogical logical,
        int depth,
        int maxDepth)
    {
        var children = logical.GetLogicalChildren().ToArray();
        var json = CreateNode(logical);
        json["childCount"] = children.Length;

        if (depth >= maxDepth)
            return json;

        var childNodes = new JsonArray();
        foreach (var child in children)
            childNodes.Add(SerializeLogical(child, depth + 1, maxDepth));

        json["children"] = childNodes;
        return json;
    }

    private JsonObject CreateNode(object node)
    {
        var type = node.GetType();
        var json = new JsonObject
        {
            ["type"] = type.Name,
            ["fullType"] = type.FullName
        };

        var classes = new JsonArray();
        if (node is StyledElement styledElement)
        {
            json["name"] = styledElement.Name;
            foreach (var styleClass in styledElement.Classes)
                classes.Add(styleClass);
        }

        json["classes"] = classes;

        if (node is Avalonia.Visual visual)
        {
            json["bounds"] = FormatRect(visual.Bounds);
            json["isVisible"] = visual.IsVisible;
        }

        if (node is InputElement inputElement)
            json["isEnabled"] = inputElement.IsEnabled;

        AddTextOrContent(node, json);
        return json;
    }

    private void AddTextOrContent(
        object node,
        JsonObject json)
    {
        switch (node)
        {
            case TextBox textBox:
                json["text"] = textBox.Text;
                break;
            case TextBlock textBlock:
                json["text"] = textBlock.Text;
                break;
            case ContentControl { Content: not null } contentControl:
                json["content"] = _formatter.Format(
                    contentControl.Content,
                    new ValueFormatOptions
                    {
                        MaxStringLength = 120,
                        MaxEnumerableItems = 5,
                        MaxDepth = 0
                    });
                break;
        }
    }

    private static JsonObject FormatRect(Rect bounds) =>
        new()
        {
            ["x"] = bounds.X,
            ["y"] = bounds.Y,
            ["width"] = bounds.Width,
            ["height"] = bounds.Height
        };

    private static int NormalizeMaxDepth(int maxDepth) =>
        Math.Clamp(maxDepth, 0, 50);
}
