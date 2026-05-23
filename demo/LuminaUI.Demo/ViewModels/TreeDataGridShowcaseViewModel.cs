using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public sealed class TreeDataGridShowcaseViewModel : ObservableObject
{
    public TreeDataGridShowcaseViewModel()
    {
        FlatSource = CreateFlatSource();
        HierarchicalSource = CreateHierarchicalSource();
        HierarchicalSource.ExpandAll();
    }

    public FlatTreeDataGridSource<TreePackageRecord> FlatSource { get; }

    public HierarchicalTreeDataGridSource<TreeGridNode> HierarchicalSource { get; }

    private static FlatTreeDataGridSource<TreePackageRecord> CreateFlatSource()
    {
        var records = new ObservableCollection<TreePackageRecord>
        {
            new("LuminaUI", "Core", "net10.0", 42, true),
            new("LuminaUI.ColorPicker", "Optional", "net10.0", 6, true),
            new("LuminaUI.DataGrid", "Optional", "net10.0", 6, true),
            new("LuminaUI.TreeDataGrid", "Optional", "net10.0", 7, true),
            new("LuminaUI.Demo", "Sandbox", "net10.0", 128, false)
        };

        return new FlatTreeDataGridSource<TreePackageRecord>(records)
        {
            Columns =
            {
                new TextColumn<TreePackageRecord, string>("Project", x => x.Project, new GridLength(2, GridUnitType.Star)),
                new TextColumn<TreePackageRecord, string>("Kind", x => x.Kind, new GridLength(120)),
                new TextColumn<TreePackageRecord, string>("TFM", x => x.TargetFramework, new GridLength(120)),
                new TextColumn<TreePackageRecord, int>("Files", x => x.Files, new GridLength(90)),
                new CheckBoxColumn<TreePackageRecord>("Loaded", x => x.Loaded, (x, value) => x.Loaded = value, new GridLength(100))
            }
        };
    }

    private static HierarchicalTreeDataGridSource<TreeGridNode> CreateHierarchicalSource()
    {
        var roots = new ObservableCollection<TreeGridNode>
        {
            new("src", "Folder", "Library", 0, true,
            [
                new("LuminaUI", "Project", "Core", 42, true),
                new("LuminaUI.ColorPicker", "Project", "Optional", 6, true),
                new("LuminaUI.DataGrid", "Project", "Optional", 6, true),
                new("LuminaUI.TreeDataGrid", "Project", "Optional", 7, true)
            ]),
            new("demo", "Folder", "Sandbox", 0, true,
            [
                new("LuminaUI.Demo", "Project", "Shared demo", 128, true),
                new("LuminaUI.Demo.Desktop", "Project", "Desktop host", 9, false),
                new("LuminaUI.Demo.Browser", "Project", "Browser host", 8, false)
            ])
        };

        return new HierarchicalTreeDataGridSource<TreeGridNode>(roots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<TreeGridNode>(
                    new TextColumn<TreeGridNode, string>("Name", x => x.Name, new GridLength(240)),
                    x => x.Children,
                    x => x.Children.Count > 0,
                    x => x.IsExpanded),
                new TextColumn<TreeGridNode, string>("Type", x => x.Type, new GridLength(120)),
                new TextColumn<TreeGridNode, string>("Area", x => x.Area, new GridLength(160)),
                new TextColumn<TreeGridNode, int>("Files", x => x.Files, new GridLength(90))
            }
        };
    }
}

public sealed class TreePackageRecord(string project, string kind, string targetFramework, int files, bool loaded)
{
    public string Project { get; } = project;

    public string Kind { get; } = kind;

    public string TargetFramework { get; } = targetFramework;

    public int Files { get; } = files;

    public bool Loaded { get; set; } = loaded;
}

public sealed class TreeGridNode(
    string name,
    string type,
    string area,
    int files,
    bool isExpanded,
    ObservableCollection<TreeGridNode>? children = null)
{
    public string Name { get; } = name;

    public string Type { get; } = type;

    public string Area { get; } = area;

    public int Files { get; } = files;

    public bool IsExpanded { get; set; } = isExpanded;

    public ObservableCollection<TreeGridNode> Children { get; } = children ?? [];
}
