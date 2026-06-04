using System.Resources;
using LuminaUI.Localization;

namespace LuminaUI.Demo;

internal static class SandboxLocalization
{
	private static readonly ResourceManager ResourceManager = new ResourceManager("LuminaUI.Demo.Localization.Resources.SandboxStrings", typeof(SandboxLocalization).Assembly);

	private static readonly ResourceManager TextResourceManager = new ResourceManager("LuminaUI.Demo.Localization.Resources.SandboxTexts", typeof(SandboxLocalization).Assembly);

	private static readonly ResourceManager OverrideResourceManager = new ResourceManager("LuminaUI.Demo.Localization.Resources.SandboxOverrides", typeof(SandboxLocalization).Assembly);

	public const string AppTitle = "Sandbox.App.Title";

	public const string ShellTitle = "Sandbox.Shell.Title";

	public const string UserName = "Sandbox.User.Name";

	public const string MenuFile = "Sandbox.Menu.File";

	public const string MenuNewWindow = "Sandbox.Menu.NewWindow";

	public const string MenuOpenSandbox = "Sandbox.Menu.OpenSandbox";

	public const string MenuClose = "Sandbox.Menu.Close";

	public const string MenuView = "Sandbox.Menu.View";

	public const string MenuToggleSidebar = "Sandbox.Menu.ToggleSidebar";

	public const string MenuComponents = "Sandbox.Menu.Components";

	public const string MenuSettings = "Sandbox.Menu.Settings";

	public const string MenuHelp = "Sandbox.Menu.Help";

	public const string MenuDocumentation = "Sandbox.Menu.Documentation";

	public const string MenuAbout = "Sandbox.Menu.About";

	public const string MenuActionOpenedSandbox = "Sandbox.Menu.Action.OpenedSandbox";

	public const string MenuActionOpenedComponents = "Sandbox.Menu.Action.OpenedComponents";

	public const string MenuActionOpenedSettings = "Sandbox.Menu.Action.OpenedSettings";

	public const string MenuActionOpenedDocumentation = "Sandbox.Menu.Action.OpenedDocumentation";

	public const string MenuActionSidebarOpened = "Sandbox.Menu.Action.SidebarOpened";

	public const string MenuActionSidebarClosed = "Sandbox.Menu.Action.SidebarClosed";

	public const string MenuActionAboutMessage = "Sandbox.Menu.Action.AboutMessage";

	public const string NavFoundationGroup = "Sandbox.Nav.FoundationGroup";

	public const string NavAutoCompleteBox = "Sandbox.Nav.AutoCompleteBox";

	public const string NavLayoutGroup = "Sandbox.Nav.LayoutGroup";

	public const string NavDataEntryGroup = "Sandbox.Nav.DataEntryGroup";

	public const string NavForm = "Sandbox.Nav.Form";

	public const string NavMultiSelect = "Sandbox.Nav.MultiSelect";

	public const string NavTagInput = "Sandbox.Nav.TagInput";

	public const string NavDataDisplayGroup = "Sandbox.Nav.DataDisplayGroup";

	public const string NavFeedbackGroup = "Sandbox.Nav.FeedbackGroup";

	public const string NavLoadingContainer = "Sandbox.Nav.LoadingContainer";

	public const string NavBanner = "Sandbox.Nav.Banner";

	public const string NavNotificationCard = "Sandbox.Nav.NotificationCard";

	public const string NavDropDownButton = "Sandbox.Nav.DropDownButton";

	public const string NavSplitButton = "Sandbox.Nav.SplitButton";

	public const string NavCommandBar = "Sandbox.Nav.CommandBar";

	public const string NavDesignSystem = "Sandbox.Nav.DesignSystem";

	public const string NavColors = "Sandbox.Nav.Colors";

	public const string NavLocalizationResources = "Sandbox.Nav.LocalizationResources";

	public const string NavComponents = "Sandbox.Nav.Components";

	public const string NavFoundation = "Sandbox.Nav.Foundation";

	public const string NavCards = "Sandbox.Nav.Cards";

	public const string NavGroupBox = "Sandbox.Nav.GroupBox";

	public const string NavImages = "Sandbox.Nav.Images";

	public const string NavAvatarBadge = "Sandbox.Nav.AvatarBadge";

	public const string NavButtons = "Sandbox.Nav.Buttons";

	public const string NavButtonGroup = "Sandbox.Nav.ButtonGroup";

	public const string NavLoading = "Sandbox.Nav.Loading";

	public const string NavActions = "Sandbox.Nav.Actions";

	public const string NavPopConfirm = "Sandbox.Nav.PopConfirm";

	public const string NavHeaderedContentControl = "Sandbox.Nav.HeaderedContentControl";

	public const string NavItemsControl = "Sandbox.Nav.ItemsControl";

	public const string NavVirtualizingWrapView = "Sandbox.Nav.VirtualizingWrapView";

	public const string NavSplitView = "Sandbox.Nav.SplitView";

	public const string NavNavigationPage = "Sandbox.Nav.NavigationPage";

	public const string NavDrawerPage = "Sandbox.Nav.DrawerPage";

	public const string NavTabbedPage = "Sandbox.Nav.TabbedPage";

	public const string NavCarousel = "Sandbox.Nav.Carousel";

	public const string NavTransitioningContentControl = "Sandbox.Nav.TransitioningContentControl";

	public const string NavBreadcrumb = "Sandbox.Nav.Breadcrumb";

	public const string NavTabStrip = "Sandbox.Nav.TabStrip";

	public const string NavTabControl = "Sandbox.Nav.TabControl";

	public const string NavTextInputs = "Sandbox.Nav.TextInputs";

	public const string NavInputOtp = "Sandbox.Nav.InputOtp";

	public const string NavSelection = "Sandbox.Nav.Selection";

	public const string NavPickers = "Sandbox.Nav.Pickers";

	public const string NavColorPicker = "Sandbox.Nav.ColorPicker";

	public const string NavDateRangePicker = "Sandbox.Nav.DateRangePicker";

	public const string NavRange = "Sandbox.Nav.Range";

	public const string NavRangeSlider = "Sandbox.Nav.RangeSlider";

	public const string NavPagination = "Sandbox.Nav.Pagination";

	public const string NavTimeline = "Sandbox.Nav.Timeline";

	public const string NavDescriptions = "Sandbox.Nav.Descriptions";

	public const string NavProperties = "Sandbox.Nav.Properties";

	public const string NavRating = "Sandbox.Nav.Rating";

	public const string NavCollections = "Sandbox.Nav.Collections";

	public const string NavDataGrid = "Sandbox.Nav.DataGrid";

	public const string NavTreeDataGrid = "Sandbox.Nav.TreeDataGrid";

	public const string NavAutoScrollText = "Sandbox.Nav.AutoScrollText";

	public const string NavIndexedList = "Sandbox.Nav.IndexedList";

	public const string NavLinkedCategory = "Sandbox.Nav.LinkedCategory";

	public const string NavTabsExpanders = "Sandbox.Nav.TabsExpanders";

	public const string NavPatterns = "Sandbox.Nav.Patterns";

	public const string NavMenus = "Sandbox.Nav.Menus";

	public const string NavOverlays = "Sandbox.Nav.Overlays";

	public const string NavWindowDialogs = "Sandbox.Nav.WindowDialogs";

	public const string NavLayout = "Sandbox.Nav.Layout";

	public const string NavLogin = "Sandbox.Nav.Login";

	public const string NavSettings = "Sandbox.Nav.Settings";

	public const string LanguageEnglish = "Sandbox.Language.English";

	public const string LanguageChinese = "Sandbox.Language.Chinese";

	public const string LanguageCurrentValue = "Sandbox.Language.CurrentValue";

	public const string SettingsTitle = "Sandbox.Settings.Title";

	public const string SettingsMobileTitle = "Sandbox.Settings.MobileTitle";

	public const string SettingsMobileDescription = "Sandbox.Settings.MobileDescription";

	public const string SettingsProfile = "Sandbox.Settings.Profile";

	public const string SettingsProfileDescription = "Sandbox.Settings.ProfileDescription";

	public const string SettingsNotifications = "Sandbox.Settings.Notifications";

	public const string SettingsNotificationsDescription = "Sandbox.Settings.NotificationsDescription";

	public const string SettingsEnabled = "Sandbox.Settings.Enabled";

	public const string SettingsLanguage = "Sandbox.Settings.Language";

	public const string SettingsStorage = "Sandbox.Settings.Storage";

	public const string SettingsDeleteAccount = "Sandbox.Settings.DeleteAccount";

	public const string SettingsDeleteAccountDescription = "Sandbox.Settings.DeleteAccountDescription";

	public const string SettingsTapHint = "Sandbox.Settings.TapHint";

	public const string SettingsTappedFormat = "Sandbox.Settings.TappedFormat";

	public const string SettingsAppearance = "Sandbox.Settings.Appearance";

	public const string SettingsAppearanceDescription = "Sandbox.Settings.AppearanceDescription";

	public const string SettingsDarkMode = "Sandbox.Settings.DarkMode";

	public const string SettingsDarkModeDescription = "Sandbox.Settings.DarkModeDescription";

	public const string SettingsToggleTheme = "Sandbox.Settings.ToggleTheme";

	public const string SettingsThemeMode = "Sandbox.Settings.ThemeMode";

	public const string SettingsThemeModeDescription = "Sandbox.Settings.ThemeModeDescription";

	public const string SettingsThemeModeSystem = "Sandbox.Settings.ThemeMode.System";

	public const string SettingsThemeModeLight = "Sandbox.Settings.ThemeMode.Light";

	public const string SettingsThemeModeDark = "Sandbox.Settings.ThemeMode.Dark";

	public const string SettingsThemeColor = "Sandbox.Settings.ThemeColor";

	public const string SettingsThemeColorDescription = "Sandbox.Settings.ThemeColorDescription";

	public const string SettingsReduceAnimations = "Sandbox.Settings.ReduceAnimations";

	public const string SettingsReduceAnimationsDescription = "Sandbox.Settings.ReduceAnimationsDescription";

	public const string SettingsDisabled = "Sandbox.Settings.Disabled";

	public const string SettingsAccount = "Sandbox.Settings.Account";

	public const string SettingsAccountDescription = "Sandbox.Settings.AccountDescription";

	public const string SettingsLogOut = "Sandbox.Settings.LogOut";

	public const string SettingsInvalidColor = "Sandbox.Settings.InvalidColor";

	public const string LocalizationOverrideTitle = "Sandbox.Localization.OverrideTitle";

	public const string LocalizationOverrideDescription = "Sandbox.Localization.OverrideDescription";

	public const string LocalizationResourcesTitle = "Sandbox.LocalizationResources.Title";

	public const string LocalizationResourcesDescription = "Sandbox.LocalizationResources.Description";

	public const string LocalizationResourcesCountFormat = "Sandbox.LocalizationResources.CountFormat";

	public const string LocalizationResourcesCopiedFormat = "Sandbox.LocalizationResources.CopiedFormat";

	public const string LocalizationResourcesSnippetTitle = "Sandbox.LocalizationResources.SnippetTitle";

	public const string LocalizationResourcesSnippetDescription = "Sandbox.LocalizationResources.SnippetDescription";

	public const string LocalizationResourcesOverridden = "Sandbox.LocalizationResources.Overridden";

	public const string LocalizationResourcesHeaderGroup = "Sandbox.LocalizationResources.Header.Group";

	public const string LocalizationResourcesHeaderKey = "Sandbox.LocalizationResources.Header.Key";

	public const string LocalizationResourcesHeaderEnglish = "Sandbox.LocalizationResources.Header.English";

	public const string LocalizationResourcesHeaderChinese = "Sandbox.LocalizationResources.Header.Chinese";

	public const string LocalizationResourcesHeaderCurrentFormat = "Sandbox.LocalizationResources.Header.CurrentFormat";

	public const string LocalizationResourcesHeaderStatus = "Sandbox.LocalizationResources.Header.Status";

	public const string WindowGlassUnavailable = "Sandbox.WindowGlass.Unavailable";

	public const string WindowGlassActiveFormat = "Sandbox.WindowGlass.ActiveFormat";

	public const string WindowGlassFallbackFormat = "Sandbox.WindowGlass.FallbackFormat";

	public const string CommonCopiedFormat = "Sandbox.Common.CopiedFormat";

	public const string CommonSelectedFormat = "Sandbox.Common.SelectedFormat";

	public const string IndexedListGroupFormat = "Sandbox.IndexedList.GroupFormat";

	public const string PickerTitle = "Sandbox.Picker.Title";

	public const string PickerDescription = "Sandbox.Picker.Description";

	public const string PickerComboAdaptive = "Sandbox.Picker.ComboAdaptive";

	public const string PickerModeDropdown = "Sandbox.Picker.ModeDropdown";

	public const string PickerModeSheet = "Sandbox.Picker.ModeSheet";

	public const string PickerDateTimeAdaptive = "Sandbox.Picker.DateTimeAdaptive";

	public const string PickerCalendarDate = "Sandbox.Picker.CalendarDate";

	public const string PickerItem1 = "Sandbox.Picker.Item1";

	public const string PickerItem2 = "Sandbox.Picker.Item2";

	public const string PickerItem3 = "Sandbox.Picker.Item3";

	public const string ActionsSheetActions = "Sandbox.Actions.SheetActions";

	public const string ActionsDuplicate = "Sandbox.Actions.Duplicate";

	public const string ActionsRename = "Sandbox.Actions.Rename";

	public const string ActionsRemove = "Sandbox.Actions.Remove";

	public static void Register()
	{
		LuminaLocalization.RegisterResourceManager(ResourceManager, 50);
		LuminaLocalization.RegisterResourceManager(TextResourceManager, 50);
		LuminaLocalization.RegisterResourceManager(OverrideResourceManager, 500);
	}
}
