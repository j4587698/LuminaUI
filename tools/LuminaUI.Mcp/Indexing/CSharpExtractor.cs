using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using LuminaUI.Mcp.Models;

namespace LuminaUI.Mcp.Indexing;

public sealed class CSharpExtractor
{
    public List<ComponentInfo> ExtractComponents(string filePath, string content)
    {
        var components = new List<ComponentInfo>();
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = tree.GetRoot();

        var namespaceDecl = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        var fileScopedNamespace = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        var ns = namespaceDecl?.Name.ToString() ?? fileScopedNamespace?.Name.ToString() ?? "";

        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var baseType = classDecl.BaseList?.Types.FirstOrDefault()?.Type.ToString() ?? "object";
            var className = classDecl.Identifier.Text;

            if (!className.StartsWith("Lumina"))
                continue;

            var isControl = IsControlBaseType(baseType) || HasAvaloniaProperties(classDecl);
            if (!isControl && !IsStaticAttachedPropertyClass(classDecl))
                continue;

            var component = new ComponentInfo
            {
                Name = className,
                Namespace = ns,
                Assembly = Path.GetFileNameWithoutExtension(filePath),
                BaseType = CleanTypeName(baseType),
                Category = InferCategory(className),
                Description = ExtractXmlSummary(classDecl),
                AxamlNamespace = InferAxamlNamespace(ns, filePath),
                Properties = ExtractProperties(classDecl),
                StyleClasses = ExtractStyleClasses(content)
            };

            components.Add(component);
        }

        return components;
    }

    public List<EnumInfo> ExtractEnums(string filePath, string content)
    {
        var enums = new List<EnumInfo>();
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = tree.GetRoot();

        var namespaceDecl = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        var fileScopedNamespace = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        var ns = namespaceDecl?.Name.ToString() ?? fileScopedNamespace?.Name.ToString() ?? "";

        foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            var enumName = enumDecl.Identifier.Text;
            if (!enumName.StartsWith("Lumina"))
                continue;

            var enumInfo = new EnumInfo
            {
                Name = enumName,
                Namespace = ns,
                Description = ExtractXmlSummary(enumDecl),
                Values = enumDecl.Members.Select((m, i) => new EnumValueInfo
                {
                    Name = m.Identifier.Text,
                    Value = ResolveEnumValue(m.EqualsValue?.Value, i),
                    Description = ExtractXmlSummary(m)
                }).ToList()
            };

            enums.Add(enumInfo);
        }

        return enums;
    }

    private static List<Models.PropertyInfo> ExtractProperties(ClassDeclarationSyntax classDecl)
    {
        var props = new List<Models.PropertyInfo>();

        foreach (var field in classDecl.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var fieldType = field.Declaration.Type.ToString();

            if (fieldType.Contains("StyledProperty") || fieldType.Contains("DirectProperty") || fieldType.Contains("AttachedProperty"))
            {
                var variable = field.Declaration.Variables.FirstOrDefault();
                if (variable is null) continue;

                var fieldName = variable.Identifier.Text;
                var propertyName = fieldName.Replace("Property", "");

                string kind = fieldType.Contains("Attached") ? "Attached"
                    : fieldType.Contains("Direct") ? "Direct"
                    : "Styled";

                string? defaultValue = null;
                var invocation = variable.Initializer?.Value as InvocationExpressionSyntax;
                if (invocation?.ArgumentList.Arguments.Count >= 2)
                {
                    var lastArg = invocation.ArgumentList.Arguments.Last();
                    defaultValue = lastArg.ToString().Trim();
                }

                bool isTwoWay = field.ToString().Contains("BindingMode.TwoWay") || field.ToString().Contains("defaultBindingMode");

                props.Add(new Models.PropertyInfo
                {
                    Name = propertyName,
                    PropertyType = ExtractPropertyType(fieldType),
                    Kind = kind,
                    DefaultValue = defaultValue,
                    IsTwoWay = isTwoWay,
                    Description = ExtractXmlSummary(field)
                });
            }
        }

        return props;
    }

    private static int ResolveEnumValue(ExpressionSyntax? expression, int fallback)
    {
        if (expression is LiteralExpressionSyntax { Token.Value: { } value })
        {
            try
            {
                return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is OverflowException or InvalidCastException or FormatException)
            {
                return fallback;
            }
        }

        return fallback;
    }

    private static string ExtractPropertyType(string fieldType)
    {
        var match = Regex.Match(fieldType, @"<(.+?)>");
        return match.Success ? match.Groups[1].Value : "object";
    }

    private static bool IsControlBaseType(string baseType)
    {
        var controlTypes = new[]
        {
            "TemplatedControl", "ContentControl", "ItemsControl", "HeaderedContentControl",
            "Control", "Panel", "Window", "ContentPage", "RangeBase", "StackPanel",
            "Grid", "ToggleButton", "Button", "CalendarDatePicker", "TimePicker",
            "HeaderedItemsControl", "VirtualizingPanel", "Menu"
        };

        var clean = CleanTypeName(baseType);
        return controlTypes.Any(t => clean.Contains(t));
    }

    private static bool HasAvaloniaProperties(ClassDeclarationSyntax classDecl)
    {
        return classDecl.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Any(f => f.Declaration.Type.ToString().Contains("Property")
                && (f.ToString().Contains("AvaloniaProperty.Register") || f.ToString().Contains("RegisterAttached")));
    }

    private static bool IsStaticAttachedPropertyClass(ClassDeclarationSyntax classDecl)
    {
        return classDecl.Modifiers.Any(m => m.Text == "static")
            && HasAvaloniaProperties(classDecl);
    }

    private static string CleanTypeName(string typeName)
    {
        var genericIndex = typeName.IndexOf('<');
        return genericIndex > 0 ? typeName[..genericIndex] : typeName;
    }

    private static string? ExtractXmlSummary(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia();
        foreach (var tr in trivia)
        {
            if (tr.GetStructure() is DocumentationCommentTriviaSyntax doc)
            {
                var summary = doc.Content.OfType<XmlElementSyntax>()
                    .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "summary");
                if (summary != null)
                {
                    var text = string.Join("", summary.Content
                        .OfType<XmlTextSyntax>()
                        .SelectMany(t => t.TextTokens)
                        .Select(t => t.Text.Trim()));
                    return string.IsNullOrWhiteSpace(text) ? null : text;
                }
            }
        }
        return null;
    }

    private static string? InferCategory(string className)
    {
        return className switch
        {
            var n when n.Contains("Button") || n.Contains("Hamburger") || n.Contains("Split") => "Action",
            var n when n.Contains("Card") || n.Contains("GroupBox") || n.Contains("Banner") => "Layout",
            var n when n.Contains("Input") || n.Contains("Tag") || n.Contains("Cascader") || n.Contains("MultiSelect") || n.Contains("AutoComplete") || n.Contains("Otp") || n.Contains("Form") => "DataEntry",
            var n when n.Contains("Avatar") || n.Contains("Badge") || n.Contains("Tag") || n.Contains("Image") || n.Contains("Rating") || n.Contains("Empty") || n.Contains("Descriptions") || n.Contains("Properties") || n.Contains("Timeline") || n.Contains("Steps") || n.Contains("Breadcrumb") => "DataDisplay",
            var n when n.Contains("Loading") || n.Contains("Skeleton") || n.Contains("Toast") || n.Contains("Notification") || n.Contains("Dialog") || n.Contains("PopConfirm") || n.Contains("BottomSheet") => "Feedback",
            var n when n.Contains("Shell") || n.Contains("Navigation") || n.Contains("Page") || n.Contains("TitleBar") || n.Contains("TopView") || n.Contains("Window") || n.Contains("Tab") || n.Contains("Carousel") || n.Contains("Drawer") || n.Contains("SplitView") || n.Contains("Breadcrumb") || n.Contains("Settings") => "Navigation",
            var n when n.Contains("Pagination") || n.Contains("Selection") || n.Contains("Range") || n.Contains("Picker") || n.Contains("Color") || n.Contains("Calendar") || n.Contains("Date") || n.Contains("Time") => "DataEntry",
            var n when n.Contains("DataGrid") || n.Contains("TreeDataGrid") || n.Contains("List") || n.Contains("Collection") => "DataDisplay",
            var n when n.Contains("Motion") || n.Contains("Animation") || n.Contains("Spring") || n.Contains("Back") => "Animation",
            var n when n.Contains("Blur") || n.Contains("Backdrop") || n.Contains("Glass") => "Effect",
            var n when n.Contains("Disable") || n.Contains("LoadingContainer") => "Utility",
            _ => "General"
        };
    }

    private static string? InferAxamlNamespace(string ns, string filePath)
    {
        if (filePath.Contains("ColorPicker"))
            return "clr-namespace:LuminaUI.ColorPicker;assembly=LuminaUI.ColorPicker";
        if (filePath.Contains("DataGrid"))
            return "clr-namespace:LuminaUI.DataGrid;assembly=LuminaUI.DataGrid";
        if (filePath.Contains("TreeDataGrid"))
            return "clr-namespace:LuminaUI.TreeDataGrid;assembly=LuminaUI.TreeDataGrid";

        return ns switch
        {
            "LuminaUI.Controls" => "clr-namespace:LuminaUI.Controls;assembly=LuminaUI",
            "LuminaUI" => "clr-namespace:LuminaUI;assembly=LuminaUI",
            _ => $"clr-namespace:{ns};assembly={Path.GetFileNameWithoutExtension(filePath)}"
        };
    }

    private static List<string> ExtractStyleClasses(string content)
    {
        var classes = new List<string>();
        var matches = Regex.Matches(content, @"Selector=""\^\.(\w+)""");
        foreach (Match m in matches)
        {
            if (m.Groups[1].Success)
                classes.Add(m.Groups[1].Value);
        }

        var classMatches = Regex.Matches(content, @"Classes=""([^""]+)""");
        foreach (Match m in classMatches)
        {
            if (m.Groups[1].Success)
            {
                foreach (var cls in m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!classes.Contains(cls))
                        classes.Add(cls);
                }
            }
        }

        return classes.Distinct().Order().ToList();
    }
}
