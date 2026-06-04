using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using LuminaUI.Controls;
using LuminaUI.Localization;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class SandboxRootView : UserControl
{
    private const string StaticShowcaseViewModel = "StaticShowcaseViewModel";
    private bool _hasNavigated;

    private static readonly string[] RouteKeys =
    [
        "NavLogin",
        "NavDesignSystem",
        "NavLocalizationResources",
        "NavColors",
        "NavFoundation",
        "NavCards",
        "NavGroupBox",
        "NavImages",
        "NavAvatarBadge",
        "NavButtons",
        "NavButtonGroup",
        "NavLoading",
        "NavAutoCompleteBox",
        "NavDropDownButton",
        "NavSplitButton",
        "NavCommandBar",
        "NavPopConfirm",
        "NavNotificationCard",
        "NavHeaderedContentControl",
        "NavItemsControl",
        "NavVirtualizingWrapView",
        "NavSplitView",
        "NavNavigationPage",
        "NavDrawerPage",
        "NavTabbedPage",
        "NavCarousel",
        "NavTransitioningContentControl",
        "NavBreadcrumb",
        "NavTabStrip",
        "NavTabControl",
        "NavTextInputs",
        "NavInputOtp",
        "NavForm",
        "NavMultiSelect",
        "NavTagInput",
        "NavCascader",
        "NavBanner",
        "NavLoadingContainer",
        "NavSelection",
        "NavPickers",
        "NavColorPicker",
        "NavDateRangePicker",
        "NavRange",
        "NavRangeSlider",
        "NavPagination",
        "NavTimeline",
        "NavSteps",
        "NavEmpty",
        "NavDescriptions",
        "NavProperties",
        "NavRating",
        "NavCollections",
        "NavDataGrid",
        "NavTreeDataGrid",
        "NavAutoScrollText",
        "NavIndexedList",
        "NavLinkedCategory",
        "NavTabsExpanders",
        "NavMenus",
        "NavOverlays",
        "NavWindowDialogs",
        "NavLayout",
        "NavMotion",
        "NavMobileRoot",
        "NavSettings"
    ];

    public SandboxNotificationCenter NotificationCenter { get; } = new();

    public SandboxRootView()
    {
        InitializeComponent();
        RegisterRoutes();

        LuminaLocalization.LanguageChanged += OnLanguageChanged;
        DetachedFromVisualTree += (_, _) => LuminaLocalization.LanguageChanged -= OnLanguageChanged;
        Loaded += OnLoaded;
    }

    public void NavigateToRoute(string routeKey, bool closeMenuOnNavigate = false)
    {
        AppShell.NavigateTo(routeKey, closeMenuOnNavigate);
    }

    public bool ToggleShellMenu()
    {
        AppShell.IsMenuOpen = !AppShell.IsMenuOpen;
        return AppShell.IsMenuOpen;
    }

    public void ShowMenuNotification(string title, string message, NotificationType type)
    {
        NotificationCenter.Show(title, message, type, TimeSpan.FromSeconds(2.8));
    }

    public void ShowAboutDialog()
    {
        var content = new StackPanel
        {
            Spacing = 8,
            MaxWidth = 360
        };

        content.Children.Add(new TextBlock
        {
            Text = LuminaLocalization.Get(SandboxLocalization.AppTitle),
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });

        content.Children.Add(new TextBlock
        {
            Text = LuminaLocalization.Get(SandboxLocalization.MenuActionAboutMessage),
            TextWrapping = TextWrapping.Wrap
        });

        RootTopView.ShowDialog(new LuminaDialog
        {
            Title = LuminaLocalization.Get(SandboxLocalization.MenuAbout),
            Content = content
        });
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SandboxTextLocalizer.Apply(TopLevel.GetTopLevel(this) as object ?? this);

        if (_hasNavigated)
        {
            return;
        }

        _hasNavigated = true;
        AppShell.NavigateTo("NavDesignSystem", closeMenuOnNavigate: false);
    }

    private void RegisterRoutes()
    {
        foreach (var routeKey in RouteKeys)
        {
            AppShell.RegisterRoute(routeKey, () => CreateRoutePage(routeKey));
        }
    }

    private Control CreateRoutePage(string navigationName)
    {
        var page = CreatePage(navigationName);
        var sourceInfo = GetSourceInfo(navigationName);
        var routePage = sourceInfo == null
            ? page
            : new ShowcaseReferencePage(page, sourceInfo);

        routePage.Loaded += (_, _) => SandboxTextLocalizer.Apply(routePage);
        return routePage;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        SandboxTextLocalizer.Apply(TopLevel.GetTopLevel(this) as object ?? this);

        foreach (var page in AppShell.CachedRouteContents)
        {
            SandboxTextLocalizer.Apply(page);
        }

        if (AppShell.DialogContent is { } dialogContent)
        {
            SandboxTextLocalizer.Apply(dialogContent);
        }

        if (RootTopView.DialogContent is { } topDialogContent)
        {
            SandboxTextLocalizer.Apply(topDialogContent);
        }
    }

    private Control CreatePage(string navigationName)
    {
        return navigationName switch
        {
            "NavLogin" => new LoginPage(() => AppShell.NavigateTo("NavDesignSystem", closeMenuOnNavigate: false)),
            "NavDesignSystem" => new DesignSystemPage(),
            "NavLocalizationResources" => new LocalizationResourcesShowcasePage(),
            "NavColors" => new ColorsShowcasePage(),
            "NavFoundation" => new FoundationShowcasePage(),

            "NavCards" => new CardShowcasePage(),
            "NavGroupBox" => new GroupBoxShowcasePage(),
            "NavImages" => new ImageShowcasePage(),
            "NavAvatarBadge" => new AvatarBadgeShowcasePage(),
            "NavButtons" => new ButtonShowcasePage(),
            "NavButtonGroup" => new ButtonGroupShowcasePage(),
            "NavLoading" => new LoadingShowcasePage(),
            "NavAutoCompleteBox" => new AutoCompleteBoxShowcasePage(),
            "NavDropDownButton" => new DropDownButtonShowcasePage(),
            "NavSplitButton" => new SplitButtonShowcasePage(),
            "NavCommandBar" => new CommandBarShowcasePage(),
            "NavPopConfirm" => new PopConfirmShowcasePage(),
            "NavNotificationCard" => new NotificationCardShowcasePage(NotificationCenter),
            "NavHeaderedContentControl" => new HeaderedContentControlShowcasePage(),
            "NavItemsControl" => new ItemsControlShowcasePage(),
            "NavVirtualizingWrapView" => new VirtualizingWrapViewShowcasePage(),
            "NavSplitView" => new SplitViewShowcasePage(),
            "NavNavigationPage" => new NavigationPageShowcasePage(),
            "NavDrawerPage" => new DrawerPageShowcasePage(),
            "NavTabbedPage" => new TabbedPageShowcasePage(),
            "NavCarousel" => new CarouselShowcasePage(),
            "NavTransitioningContentControl" => new TransitioningContentControlShowcasePage(),
            "NavBreadcrumb" => new BreadcrumbShowcasePage(),
            "NavTabStrip" => new TabStripShowcasePage(),
            "NavTabControl" => new TabControlShowcasePage(),
            "NavTextInputs" => new TextInputShowcasePage(),
            "NavInputOtp" => new InputOtpShowcasePage(),
            "NavForm" => new FormShowcasePage(),
            "NavMultiSelect" => new MultiSelectShowcasePage(),
            "NavTagInput" => new TagInputShowcasePage(),
            "NavCascader" => new CascaderShowcasePage(),
            "NavBanner" => new BannerShowcasePage(),
            "NavLoadingContainer" => new LoadingContainerShowcasePage(),
            "NavSelection" => new SelectionShowcasePage(),
            "NavPickers" => new PickerShowcasePage(),
            "NavColorPicker" => new ColorPickerShowcasePage(),
            "NavDateRangePicker" => new DateRangePickerShowcasePage(),
            "NavRange" => new RangeShowcasePage(),
            "NavRangeSlider" => new RangeSliderShowcasePage(),
            "NavPagination" => new PaginationShowcasePage(),
            "NavTimeline" => new TimelineShowcasePage(),
            "NavSteps" => new StepsShowcasePage(),
            "NavEmpty" => new EmptyShowcasePage(),
            "NavDescriptions" => new DescriptionsShowcasePage(),
            "NavProperties" => new PropertiesShowcasePage(),
            "NavRating" => new RatingShowcasePage(),
            "NavCollections" => new CollectionShowcasePage(),
            "NavDataGrid" => new DataGridShowcasePage(),
            "NavTreeDataGrid" => new TreeDataGridShowcasePage(),
            "NavAutoScrollText" => new AutoScrollTextShowcasePage(),
            "NavIndexedList" => new IndexedListShowcasePage(),
            "NavLinkedCategory" => new LinkedCategoryListShowcasePage(),
            "NavTabsExpanders" => new TabsExpanderShowcasePage(),

            "NavMenus" => new MenuFlyoutShowcasePage(),
            "NavOverlays" => new OverlaysShowcasePage(),
            "NavWindowDialogs" => new WindowDialogShowcasePage(),
            "NavLayout" => new LayoutShowcasePage(),
            "NavMotion" => new MotionShowcasePage(),
            "NavMobileRoot" => new MobileRootShowcasePage(),

            "NavSettings" => new SettingsPage(),

            _ => new DetailContentPage()
        };
    }

    private static ShowcaseSourceInfo? GetSourceInfo(string navigationName)
    {
        return navigationName switch
        {
            "NavLogin" => Source("Root/LoginPage", "LoginPageViewModel"),
            "NavDesignSystem" => Source("Design/DesignSystemPage", "DesignSystemPageViewModel"),
            "NavLocalizationResources" => Source("Design/LocalizationResourcesShowcasePage", "LocalizationResourcesShowcaseViewModel"),
            "NavColors" => Source("Design/ColorsShowcasePage", "ColorsShowcaseViewModel"),
            "NavFoundation" => Source("Design/FoundationShowcasePage", "FoundationShowcaseViewModel"),

            "NavCards" => Source("Components/CardShowcasePage", "CardShowcaseViewModel"),
            "NavGroupBox" => Source("Components/GroupBoxShowcasePage", StaticShowcaseViewModel),
            "NavImages" => Source("Components/ImageShowcasePage", "ImageShowcaseViewModel"),
            "NavAvatarBadge" => Source("Components/AvatarBadgeShowcasePage", "AvatarBadgeShowcaseViewModel"),
            "NavButtons" => Source("Components/ButtonShowcasePage", "ButtonShowcaseViewModel"),
            "NavButtonGroup" => Source("Components/ButtonGroupShowcasePage", "ButtonGroupShowcaseViewModel"),
            "NavLoading" => Source("Components/LoadingShowcasePage", "LoadingShowcaseViewModel"),
            "NavAutoCompleteBox" => Source("Components/AutoCompleteBoxShowcasePage", "AutoCompleteBoxShowcaseViewModel"),
            "NavDropDownButton" => Source("Components/DropDownButtonShowcasePage", "DropDownButtonShowcaseViewModel"),
            "NavSplitButton" => Source("Components/SplitButtonShowcasePage", "SplitButtonShowcaseViewModel"),
            "NavCommandBar" => Source("Components/CommandBarShowcasePage", "CommandBarShowcaseViewModel"),
            "NavPopConfirm" => Source("Components/PopConfirmShowcasePage", "PopConfirmShowcaseViewModel"),
            "NavNotificationCard" => Source("Components/NotificationCardShowcasePage", "NotificationCardShowcaseViewModel"),
            "NavHeaderedContentControl" => Source("Components/HeaderedContentControlShowcasePage", StaticShowcaseViewModel),
            "NavItemsControl" => Source("Components/ItemsControlShowcasePage", StaticShowcaseViewModel),
            "NavVirtualizingWrapView" => Source("Components/VirtualizingWrapViewShowcasePage", "VirtualizingWrapViewShowcaseViewModel"),
            "NavSplitView" => Source("Components/SplitViewShowcasePage", StaticShowcaseViewModel),
            "NavNavigationPage" => Source("Components/NavigationPageShowcasePage", "NavigationPageShowcaseViewModel"),
            "NavDrawerPage" => Source("Components/DrawerPageShowcasePage", "DrawerPageShowcaseViewModel"),
            "NavTabbedPage" => Source("Components/TabbedPageShowcasePage", StaticShowcaseViewModel),
            "NavCarousel" => Source("Components/CarouselShowcasePage", "CarouselShowcaseViewModel"),
            "NavTransitioningContentControl" => Source("Components/TransitioningContentControlShowcasePage", "TransitioningContentControlShowcaseViewModel"),
            "NavBreadcrumb" => Source("Components/BreadcrumbShowcasePage", "BreadcrumbShowcaseViewModel"),
            "NavTabStrip" => Source("Components/TabStripShowcasePage", StaticShowcaseViewModel),
            "NavTabControl" => Source("Components/TabControlShowcasePage", StaticShowcaseViewModel),
            "NavTextInputs" => Source("Components/TextInputShowcasePage", StaticShowcaseViewModel),
            "NavInputOtp" => Source("Components/InputOtpShowcasePage", "InputOtpShowcaseViewModel"),
            "NavForm" => Source("Components/FormShowcasePage", "FormShowcaseViewModel"),
            "NavMultiSelect" => Source("Components/MultiSelectShowcasePage", "MultiSelectShowcaseViewModel"),
            "NavTagInput" => Source("Components/TagInputShowcasePage", "TagInputShowcaseViewModel"),
            "NavCascader" => Source("Components/CascaderShowcasePage", "CascaderShowcaseViewModel"),
            "NavBanner" => Source("Components/BannerShowcasePage", "BannerShowcaseViewModel"),
            "NavLoadingContainer" => Source("Components/LoadingContainerShowcasePage", "LoadingContainerShowcaseViewModel"),
            "NavSelection" => Source("Components/SelectionShowcasePage", StaticShowcaseViewModel),
            "NavPickers" => Source("Components/PickerShowcasePage", StaticShowcaseViewModel),
            "NavColorPicker" => Source("Components/ColorPickerShowcasePage", "ColorPickerShowcaseViewModel"),
            "NavDateRangePicker" => Source("Components/DateRangePickerShowcasePage", "DateRangePickerShowcaseViewModel"),
            "NavRange" => Source("Components/RangeShowcasePage", "RangeShowcaseViewModel"),
            "NavRangeSlider" => Source("Components/RangeSliderShowcasePage", "RangeSliderShowcaseViewModel"),
            "NavPagination" => Source("Components/PaginationShowcasePage", "PaginationShowcaseViewModel"),
            "NavTimeline" => Source("Components/TimelineShowcasePage", "TimelineShowcaseViewModel"),
            "NavSteps" => Source("Components/StepsShowcasePage", "StepsShowcaseViewModel"),
            "NavEmpty" => Source("Components/EmptyShowcasePage", "EmptyShowcaseViewModel"),
            "NavDescriptions" => Source("Components/DescriptionsShowcasePage", "DescriptionsShowcaseViewModel"),
            "NavProperties" => Source("Components/PropertiesShowcasePage", "PropertiesShowcaseViewModel"),
            "NavRating" => Source("Components/RatingShowcasePage", "RatingShowcaseViewModel"),
            "NavCollections" => Source("Components/CollectionShowcasePage", StaticShowcaseViewModel),
            "NavDataGrid" => Source("Components/DataGridShowcasePage", "DataGridShowcaseViewModel"),
            "NavTreeDataGrid" => Source("Components/TreeDataGridShowcasePage", "TreeDataGridShowcaseViewModel"),
            "NavAutoScrollText" => Source("Components/AutoScrollTextShowcasePage", StaticShowcaseViewModel),
            "NavIndexedList" => Source("Components/IndexedListShowcasePage", "IndexedListShowcaseViewModel"),
            "NavLinkedCategory" => Source("Components/LinkedCategoryListShowcasePage", "LinkedCategoryListShowcaseViewModel"),
            "NavTabsExpanders" => Source("Components/TabsExpanderShowcasePage", StaticShowcaseViewModel),

            "NavMenus" => Source("Patterns/MenuFlyoutShowcasePage", StaticShowcaseViewModel),
            "NavOverlays" => Source("Patterns/OverlaysShowcasePage", "OverlaysShowcaseViewModel"),
            "NavWindowDialogs" => Source("Popups/WindowDialogShowcasePage", "WindowDialogShowcaseViewModel"),
            "NavLayout" => Source("Patterns/LayoutShowcasePage", StaticShowcaseViewModel),
            "NavMotion" => Source("Patterns/MotionShowcasePage", "MotionShowcaseViewModel"),
            "NavMobileRoot" => Source("Patterns/MobileRootShowcasePage", StaticShowcaseViewModel),

            "NavSettings" => Source("Settings/SettingsPage", "SettingsPageViewModel"),

            _ => null
        };
    }

    private static ShowcaseSourceInfo Source(string viewPath, string viewModelName)
    {
        return new ShowcaseSourceInfo(
            $"Views/{viewPath}.axaml",
            $"Views/{viewPath}.axaml.cs",
            $"ViewModels/{viewModelName}.cs");
    }
}
