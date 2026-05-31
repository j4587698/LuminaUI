namespace LuminaUI.Mcp.Models;

public sealed class ComponentInfo
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Assembly { get; set; } = "";
    public string BaseType { get; set; } = "";
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? AxamlNamespace { get; set; }
    public List<PropertyInfo> Properties { get; set; } = [];
    public List<string> StyleClasses { get; set; } = [];
    public List<string> ThemeResources { get; set; } = [];
}

public sealed class PropertyInfo
{
    public string Name { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? DefaultValue { get; set; }
    public bool IsTwoWay { get; set; }
    public string? Description { get; set; }
}

public sealed class EnumInfo
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string? Description { get; set; }
    public List<EnumValueInfo> Values { get; set; } = [];
}

public sealed class EnumValueInfo
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public string? Description { get; set; }
}

public sealed class ExampleInfo
{
    public string ComponentName { get; set; } = "";
    public string ShowcasePage { get; set; } = "";
    public string? AxamlSource { get; set; }
    public string? CodeBehindSource { get; set; }
    public string? ViewModelSource { get; set; }
}

public sealed class DesignToken
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string? LightValue { get; set; }
    public string? DarkValue { get; set; }
    public string? Description { get; set; }
}
