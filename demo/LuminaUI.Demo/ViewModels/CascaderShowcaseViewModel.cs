using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Controls;

namespace LuminaUI.Demo.ViewModels;

public partial class CascaderShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _selectedRegion;

    [ObservableProperty]
    private object? _selectedCategory;

    [ObservableProperty]
    private object? _selectedIntermediateRegion;

    [ObservableProperty]
    private object? _selectedLazyRegion;

    [ObservableProperty]
    private object? _selectedLongList;

    [ObservableProperty]
    private object? _selectedSheetRegion;

    public AvaloniaList<LuminaCascaderNode> RegionData { get; } =
    [
        Node("zhejiang", "Zhejiang",
            Node("hangzhou", "Hangzhou",
                Node("xihu", "Xihu"), Node("yuhang", "Yuhang"), Node("binjiang", "Binjiang"),
                Node("xiaoshan", "Xiaoshan"), Node("fuyang", "Fuyang"), Node("linan", "Lin'an")),
            Node("ningbo", "Ningbo",
                Node("haishu", "Haishu"), Node("jiangbei", "Jiangbei"), Node("zhenhai", "Zhenhai")),
            Node("wenzhou", "Wenzhou",
                Node("lucheng", "Lucheng"), Node("ouhai", "Ouhai"), Node("longwan", "Longwan")),
            Node("jiaxing", "Jiaxing",
                Node("nanhu", "Nanhu"), Node("xiuzhou", "Xiuzhou")),
            Node("huzhou", "Huzhou",
                Node("wuxing", "Wuxing"), Node("nanxun", "Nanxun")),
            Node("shaoxing", "Shaoxing",
                Node("yuecheng", "Yuecheng"), Node("keqiao", "Keqiao"))),
        Node("jiangsu", "Jiangsu",
            Node("nanjing", "Nanjing",
                Node("xuanwu", "Xuanwu"), Node("gulou", "Gulou"), Node("jianye", "Jianye"),
                Node("qinhuai", "Qinhuai"), Node("jiangning", "Jiangning")),
            Node("suzhou", "Suzhou",
                Node("gusu", "Gusu"), Node("wuzhong", "Wuzhong"), Node("huqiu", "Huqiu")),
            Node("wuxi", "Wuxi",
                Node("liangxi", "Liangxi"), Node("xishan", "Xishan"), Node("huishan", "Huishan")),
            Node("changzhou", "Changzhou",
                Node("tianning", "Tianning"), Node("zhonglou", "Zhonglou")),
            Node("nantong", "Nantong",
                Node("chongchuan", "Chongchuan"), Node("tongzhou", "Tongzhou"))),
        Node("guangdong", "Guangdong",
            Node("guangzhou", "Guangzhou",
                Node("tianhe", "Tianhe"), Node("yuexiu", "Yuexiu"), Node("haizhu", "Haizhu"),
                Node("panyu", "Panyu"), Node("baiyun", "Baiyun"), Node("huangpu", "Huangpu")),
            Node("shenzhen", "Shenzhen",
                Node("nanshan", "Nanshan"), Node("futian", "Futian"), Node("luohu", "Luohu"),
                Node("baoan", "Bao'an"), Node("longhua", "Longhua")),
            Node("dongguan", "Dongguan",
                Node("chengqu", "Chengqu"), Node("wanjiang", "Wanjiang")),
            Node("foshan", "Foshan",
                Node("chancheng", "Chancheng"), Node("nanhai", "Nanhai"), Node("shunde", "Shunde")),
            Node("zhuhai", "Zhuhai",
                Node("xiangzhou", "Xiangzhou"), Node("jinwan", "Jinwan"))),
        Node("beijing", "Beijing",
            Node("dongcheng", "Dongcheng"), Node("xicheng", "Xicheng"),
            Node("chaoyang", "Chaoyang"), Node("haidian", "Haidian"),
            Node("fengtai", "Fengtai"), Node("shijingshan", "Shijingshan")),
        Node("shanghai", "Shanghai",
            Node("huangpu", "Huangpu"), Node("xuhui", "Xuhui"),
            Node("changning", "Changning"), Node("jingan", "Jing'an"),
            Node("putuo", "Putuo"), Node("hongkou", "Hongkou"),
            Node("yangpu", "Yangpu"), Node("pudong", "Pudong"))
    ];

    public AvaloniaList<LuminaCascaderNode> CategoryData { get; } =
    [
        Node("electronics", "Electronics",
            Node("phone", "Phone", Node("apple", "Apple"), Node("samsung", "Samsung"), Node("huawei", "Huawei"), Node("xiaomi", "Xiaomi")),
            Node("laptop", "Laptop", Node("macbook", "MacBook"), Node("thinkpad", "ThinkPad"), Node("dell", "Dell")),
            Node("tablet", "Tablet", Node("ipad", "iPad"), Node("surface", "Surface")),
            Node("camera", "Camera", Node("canon", "Canon"), Node("sony", "Sony"), Node("nikon", "Nikon"))),
        Node("clothing", "Clothing",
            Node("men", "Men", Node("shirts", "Shirts"), Node("pants", "Pants"), Node("shoes", "Shoes")),
            Node("women", "Women", Node("dresses", "Dresses"), Node("skirts", "Skirts"), Node("bags", "Bags")),
            Node("kids", "Kids")),
        Node("food", "Food",
            Node("fruit", "Fruit", Node("apple", "Apple"), Node("banana", "Banana"), Node("orange", "Orange"), Node("grape", "Grape")),
            Node("snacks", "Snacks", Node("chips", "Chips"), Node("cookies", "Cookies"), Node("candy", "Candy")),
            Node("drinks", "Drinks", Node("cola", "Cola"), Node("juice", "Juice"), Node("tea", "Tea"))),
        Node("home", "Home",
            Node("furniture", "Furniture", Node("sofa", "Sofa"), Node("bed", "Bed"), Node("desk", "Desk")),
            Node("kitchen", "Kitchen", Node("cookware", "Cookware"), Node("tableware", "Tableware")),
            Node("decor", "Decor", Node("lighting", "Lighting"), Node("rugs", "Rugs")))
    ];

    public AvaloniaList<LuminaCascaderNode> LazyRegionData { get; } =
    [
        LazyNode("asia", "Asia"),
        LazyNode("europe", "Europe"),
        LazyNode("america", "America")
    ];

    public AvaloniaList<LuminaCascaderNode> LongListData { get; } = CreateLongListData();

    public async Task<IEnumerable<LuminaCascaderNode>> LoadLazyChildrenAsync(LuminaCascaderNode node)
    {
        await Task.Delay(450);

        return node.Value?.ToString() switch
        {
            "asia" =>
            [
                LazyNode("china", "China"),
                LazyNode("japan", "Japan"),
                LazyNode("singapore", "Singapore")
            ],
            "europe" =>
            [
                LazyNode("germany", "Germany"),
                LazyNode("france", "France"),
                LazyNode("italy", "Italy")
            ],
            "america" =>
            [
                LazyNode("usa", "United States"),
                LazyNode("canada", "Canada"),
                LazyNode("brazil", "Brazil")
            ],
            "china" => [Node("shanghai-lazy", "Shanghai"), Node("hangzhou-lazy", "Hangzhou"), Node("chengdu-lazy", "Chengdu")],
            "japan" => [Node("tokyo", "Tokyo"), Node("osaka", "Osaka"), Node("kyoto", "Kyoto")],
            "singapore" => [Node("central", "Central"), Node("east", "East"), Node("west", "West")],
            "germany" => [Node("berlin", "Berlin"), Node("munich", "Munich"), Node("hamburg", "Hamburg")],
            "france" => [Node("paris", "Paris"), Node("lyon", "Lyon"), Node("nice", "Nice")],
            "italy" => [Node("rome", "Rome"), Node("milan", "Milan"), Node("florence", "Florence")],
            "usa" => [Node("new-york", "New York"), Node("san-francisco", "San Francisco"), Node("seattle", "Seattle")],
            "canada" => [Node("toronto", "Toronto"), Node("vancouver", "Vancouver"), Node("montreal", "Montreal")],
            "brazil" => [Node("sao-paulo", "Sao Paulo"), Node("rio", "Rio de Janeiro"), Node("brasilia", "Brasilia")],
            _ => []
        };
    }

    private static LuminaCascaderNode Node(string value, string label, params LuminaCascaderNode[] children)
    {
        var node = new LuminaCascaderNode { Label = label, Value = value };
        foreach (var c in children) node.Children.Add(c);
        return node;
    }

    private static LuminaCascaderNode LazyNode(string value, string label)
    {
        return new LuminaCascaderNode
        {
            Label = label,
            Value = value,
            HasUnloadedChildren = true
        };
    }

    private static AvaloniaList<LuminaCascaderNode> CreateLongListData()
    {
        var roots = new AvaloniaList<LuminaCascaderNode>();
        for (var i = 1; i <= 50; i++)
        {
            var cities = new List<LuminaCascaderNode>();
            for (var j = 1; j <= 30; j++)
            {
                cities.Add(Node($"long-{i:00}-{j:00}", $"Option {i:00}-{j:00}"));
            }

            roots.Add(Node($"long-{i:00}", $"Very long option group {i:00}", [.. cities]));
        }

        return roots;
    }
}
