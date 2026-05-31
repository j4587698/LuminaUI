using LuminaUI.Mcp.Models;

namespace LuminaUI.Mcp.Indexing;

public sealed class ShowcaseMapper
{
    private static readonly Dictionary<string, (string ViewPath, string? ViewModelName, string[] ComponentNames)> ShowcaseMap = new()
    {
        ["Buttons"] = ("Components/ButtonShowcasePage", "ButtonShowcaseViewModel", ["LuminaButton", "LuminaHamburgerButton", "LuminaButtonGroup"]),
        ["ButtonGroup"] = ("Components/ButtonGroupShowcasePage", "ButtonGroupShowcaseViewModel", ["LuminaButtonGroup"]),
        ["Cards"] = ("Components/CardShowcasePage", "CardShowcaseViewModel", ["LuminaCard"]),
        ["Images"] = ("Components/ImageShowcasePage", "ImageShowcaseViewModel", ["LuminaImage"]),
        ["AvatarBadge"] = ("Components/AvatarBadgeShowcasePage", "AvatarBadgeShowcaseViewModel", ["LuminaAvatar", "LuminaBadge"]),
        ["Loading"] = ("Components/LoadingShowcasePage", "LoadingShowcaseViewModel", ["LuminaLoading"]),
        ["LoadingContainer"] = ("Components/LoadingContainerShowcasePage", "LoadingContainerShowcaseViewModel", ["LuminaLoadingContainer"]),
        ["Banner"] = ("Components/BannerShowcasePage", "BannerShowcaseViewModel", ["LuminaBanner"]),
        ["Breadcrumb"] = ("Components/BreadcrumbShowcasePage", "BreadcrumbShowcaseViewModel", ["LuminaBreadcrumb"]),
        ["Carousel"] = ("Components/CarouselShowcasePage", "CarouselShowcaseViewModel", ["LuminaCarousel"]),
        ["Cascader"] = ("Components/CascaderShowcasePage", "CascaderShowcaseViewModel", ["LuminaCascader"]),
        ["ColorPicker"] = ("Components/ColorPickerShowcasePage", "ColorPickerShowcaseViewModel", ["LuminaColorSwatch"]),
        ["CommandBar"] = ("Components/CommandBarShowcasePage", "CommandBarShowcaseViewModel", ["LuminaCommand"]),
        ["DataGrid"] = ("Components/DataGridShowcasePage", "DataGridShowcaseViewModel", []),
        ["DateRangePicker"] = ("Components/DateRangePickerShowcasePage", "DateRangePickerShowcaseViewModel", ["LuminaDateRangePicker", "LuminaDateRangeCalendar"]),
        ["Descriptions"] = ("Components/DescriptionsShowcasePage", "DescriptionsShowcaseViewModel", ["LuminaDescriptions"]),
        ["DrawerPage"] = ("Components/DrawerPageShowcasePage", "DrawerPageShowcaseViewModel", ["LuminaDrawerPage"]),
        ["DropDownButton"] = ("Components/DropDownButtonShowcasePage", "DropDownButtonShowcaseViewModel", []),
        ["Empty"] = ("Components/EmptyShowcasePage", "EmptyShowcaseViewModel", ["LuminaEmpty"]),
        ["Form"] = ("Components/FormShowcasePage", "FormShowcaseViewModel", ["LuminaForm", "LuminaFormItem", "LuminaFormGroup"]),
        ["GroupBox"] = ("Components/GroupBoxShowcasePage", null, ["LuminaGroupBox"]),
        ["HeaderedContentControl"] = ("Components/HeaderedContentControlShowcasePage", null, []),
        ["IconButton"] = ("Components/IconButtonShowcasePage", "IconButtonShowcaseViewModel", ["LuminaIconButton"]),
        ["IndexedList"] = ("Components/IndexedListShowcasePage", "IndexedListShowcaseViewModel", ["LuminaIndexedList"]),
        ["InputOtp"] = ("Components/InputOtpShowcasePage", "InputOtpShowcaseViewModel", ["LuminaInputOtp"]),
        ["ItemsControl"] = ("Components/ItemsControlShowcasePage", "ItemsControlShowcaseViewModel", []),
        ["LinkedCategoryList"] = ("Components/LinkedCategoryListShowcasePage", "LinkedCategoryListShowcaseViewModel", ["LuminaLinkedCategoryList"]),
        ["MultiSelect"] = ("Components/MultiSelectShowcasePage", "MultiSelectShowcaseViewModel", ["LuminaMultiSelect"]),
        ["NavigationPage"] = ("Components/NavigationPageShowcasePage", "NavigationPageShowcaseViewModel", ["LuminaNavigationPage"]),
        ["NotificationCard"] = ("Components/NotificationCardShowcasePage", "NotificationCardShowcaseViewModel", ["LuminaNotificationCardOptions"]),
        ["Pagination"] = ("Components/PaginationShowcasePage", "PaginationShowcaseViewModel", ["LuminaPagination"]),
        ["Picker"] = ("Components/PickerShowcasePage", null, []),
        ["PopConfirm"] = ("Components/PopConfirmShowcasePage", "PopConfirmShowcaseViewModel", ["LuminaPopConfirm"]),
        ["Properties"] = ("Components/PropertiesShowcasePage", "PropertiesShowcaseViewModel", ["LuminaProperties"]),
        ["Range"] = ("Components/RangeShowcasePage", "RangeShowcaseViewModel", []),
        ["RangeSlider"] = ("Components/RangeSliderShowcasePage", "RangeSliderShowcaseViewModel", ["LuminaRangeSlider"]),
        ["Rating"] = ("Components/RatingShowcasePage", "RatingShowcaseViewModel", ["LuminaRating"]),
        ["Selection"] = ("Components/SelectionShowcasePage", null, []),
        ["SplitButton"] = ("Components/SplitButtonShowcasePage", "SplitButtonShowcaseViewModel", []),
        ["SplitView"] = ("Components/SplitViewShowcasePage", null, []),
        ["Steps"] = ("Components/StepsShowcasePage", "StepsShowcaseViewModel", ["LuminaSteps"]),
        ["TabbedPage"] = ("Components/TabbedPageShowcasePage", null, ["LuminaTabbedPage"]),
        ["TabControl"] = ("Components/TabControlShowcasePage", null, ["LuminaTabControl"]),
        ["TabsExpander"] = ("Components/TabsExpanderShowcasePage", null, []),
        ["TabStrip"] = ("Components/TabStripShowcasePage", null, ["LuminaTabStrip"]),
        ["TagInput"] = ("Components/TagInputShowcasePage", "TagInputShowcaseViewModel", ["LuminaTagInput"]),
        ["TextInput"] = ("Components/TextInputShowcasePage", null, []),
        ["Timeline"] = ("Components/TimelineShowcasePage", "TimelineShowcaseViewModel", ["LuminaTimeline"]),
        ["TransitioningContentControl"] = ("Components/TransitioningContentControlShowcasePage", "TransitioningContentControlShowcaseViewModel", []),
        ["TreeDataGrid"] = ("Components/TreeDataGridShowcasePage", "TreeDataGridShowcaseViewModel", []),
        ["AutoScrollText"] = ("Components/AutoScrollTextShowcasePage", null, ["LuminaAutoScrollText"]),
        ["Collection"] = ("Components/CollectionShowcasePage", null, []),
        ["Overlays"] = ("Patterns/OverlaysShowcasePage", "OverlaysShowcaseViewModel", ["LuminaDialog", "LuminaToast", "LuminaBottomSheet"]),
        ["Motion"] = ("Patterns/MotionShowcasePage", "MotionShowcaseViewModel", ["LuminaMotion"]),
        ["Settings"] = ("Settings/SettingsPage", "SettingsPageViewModel", ["LuminaSettingsCard", "LuminaSettingItem"]),
    };

    public List<ExampleInfo> BuildExamples(string demoViewsPath)
    {
        var examples = new List<ExampleInfo>();
        var mappedAxamlFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, (viewPath, viewModelName, componentNames)) in ShowcaseMap)
        {
            var axamlFile = Path.Combine(demoViewsPath, $"{viewPath}.axaml");
            var csFile = Path.Combine(demoViewsPath, $"{viewPath}.axaml.cs");
            string? vmFile = viewModelName is not null
                ? Path.Combine(demoViewsPath, "..", "ViewModels", $"{viewModelName}.cs")
                : null;

            mappedAxamlFiles.Add(Path.GetFullPath(axamlFile));

            var axamlSource = SafeReadFile(axamlFile);
            var csSource = SafeReadFile(csFile);
            var vmSource = vmFile is not null ? SafeReadFile(vmFile) : null;

            if (axamlSource is null) continue;

            var pageName = Path.GetFileNameWithoutExtension(axamlFile);

            if (componentNames.Length > 0)
            {
                foreach (var compName in componentNames)
                {
                    examples.Add(new ExampleInfo
                    {
                        ComponentName = compName,
                        ShowcasePage = pageName,
                        AxamlSource = axamlSource,
                        CodeBehindSource = csSource,
                        ViewModelSource = vmSource
                    });
                }
            }
            else
            {
                // Map by page name convention: ButtonShowcasePage -> Button
                var inferredName = pageName.Replace("ShowcasePage", "");
                examples.Add(new ExampleInfo
                {
                    ComponentName = inferredName,
                    ShowcasePage = pageName,
                    AxamlSource = axamlSource,
                    CodeBehindSource = csSource,
                    ViewModelSource = vmSource
                });
            }
        }

        examples.AddRange(BuildUnmappedExamples(demoViewsPath, mappedAxamlFiles));

        return examples;
    }

    public List<string> FindMissingFiles(string demoViewsPath)
    {
        var missingFiles = new List<string>();

        foreach (var (_, (viewPath, viewModelName, _)) in ShowcaseMap)
        {
            var axamlFile = Path.GetFullPath(Path.Combine(demoViewsPath, $"{viewPath}.axaml"));
            if (!File.Exists(axamlFile))
                missingFiles.Add(axamlFile);

            if (viewModelName is not null)
            {
                var viewModelFile = Path.GetFullPath(Path.Combine(demoViewsPath, "..", "ViewModels", $"{viewModelName}.cs"));
                if (!File.Exists(viewModelFile))
                    missingFiles.Add(viewModelFile);
            }
        }

        return missingFiles;
    }

    public Dictionary<string, string> BuildShowcaseComponentMapping()
    {
        var mapping = new Dictionary<string, string>();
        foreach (var (_, (viewPath, _, componentNames)) in ShowcaseMap)
        {
            var pageName = Path.GetFileNameWithoutExtension($"{viewPath}.axaml");
            foreach (var compName in componentNames)
            {
                if (!mapping.ContainsKey(compName))
                    mapping[compName] = pageName;
            }
        }
        return mapping;
    }

    private static List<ExampleInfo> BuildUnmappedExamples(string demoViewsPath, HashSet<string> mappedAxamlFiles)
    {
        var examples = new List<ExampleInfo>();
        if (!Directory.Exists(demoViewsPath))
            return examples;

        foreach (var axamlFile in Directory.GetFiles(demoViewsPath, "*ShowcasePage.axaml", SearchOption.AllDirectories))
        {
            var fullPath = Path.GetFullPath(axamlFile);
            if (mappedAxamlFiles.Contains(fullPath))
                continue;

            var pageName = Path.GetFileNameWithoutExtension(axamlFile);
            var viewModelFile = FindViewModelFile(demoViewsPath, pageName);
            var axamlSource = SafeReadFile(axamlFile);
            if (axamlSource is null)
                continue;

            examples.Add(new ExampleInfo
            {
                ComponentName = InferComponentName(pageName),
                ShowcasePage = pageName,
                AxamlSource = axamlSource,
                CodeBehindSource = SafeReadFile($"{axamlFile}.cs"),
                ViewModelSource = viewModelFile is not null ? SafeReadFile(viewModelFile) : null
            });
        }

        return examples;
    }

    private static string? FindViewModelFile(string demoViewsPath, string pageName)
    {
        var viewModelName = pageName.Replace("Page", "ViewModel", StringComparison.Ordinal);
        var viewModelFile = Path.GetFullPath(Path.Combine(demoViewsPath, "..", "ViewModels", $"{viewModelName}.cs"));
        return File.Exists(viewModelFile) ? viewModelFile : null;
    }

    private static string InferComponentName(string pageName)
    {
        return pageName.Replace("ShowcasePage", "", StringComparison.Ordinal);
    }

    private static string? SafeReadFile(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
        }
        catch
        {
            return null;
        }
    }
}
