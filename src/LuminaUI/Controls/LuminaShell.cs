using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaShell : ContentControl, ILuminaOverlayHost
{
    private static readonly object ShellRegistryLock = new object();

    private static readonly List<WeakReference<LuminaShell>> AttachedShells = new List<WeakReference<LuminaShell>>();

    private static readonly Dictionary<string, WeakReference<LuminaShell>> ShellRegistry = new Dictionary<string, WeakReference<LuminaShell>>(StringComparer.Ordinal);

    private const double SmallScreenBreakpoint = 768.0;

    private readonly Dictionary<string, Func<Control>> _routeFactories = new Dictionary<string, Func<Control>>(StringComparer.Ordinal);

    private readonly Dictionary<string, Control> _routeCache = new Dictionary<string, Control>(StringComparer.Ordinal);

    private readonly Dictionary<string, Page> _routePageCache = new Dictionary<string, Page>(StringComparer.Ordinal);

    private readonly ConditionalWeakTable<Page, LuminaShellPushOptions> _pushOptions = new ConditionalWeakTable<Page, LuminaShellPushOptions>();

    private TopLevel? _backRequestedTopLevel;

    private NavigationPage? _navigationHost;

    private NavigationPage? _observedNavigationHost;

    private LuminaOverlayHost? _overlayHost;

    private LuminaOverlayHost? _observedOverlayHost;

    private LuminaOverlayHost? _menuDrawerHost;

    private LuminaDrawer? _menuDrawer;

    private ContentPresenter? _menuDrawerHeaderPresenter;

    private ContentPresenter? _menuDrawerContentPresenter;

    private ContentPresenter? _menuDrawerFooterPresenter;

    private IDisposable? _menuDrawerHeaderBinding;

    private IDisposable? _menuDrawerContentBinding;

    private IDisposable? _menuDrawerFooterBinding;

    private Page? _activeRoutePage;

    private int _navigationHostResetVersion;

    private bool _isResettingNavigationHost;

    // 事件驱动的导航空闲等待：替代 Task.Delay 轮询。
    // 当 NavigationPage.IsNavigating 变为 false 时，在属性变更回调中完成该 TCS。
    private TaskCompletionSource<bool>? _navigationIdleCompletion;

    private LuminaPage? _activePage;

    private object? _toastContent;

    private bool _isNavigating;

    private bool _syncingNavigationKey;

    private bool _syncingOverlayHostFromShell;

    private bool _syncingOverlayHostFromHost;

    private bool _syncingMenuDrawer;

    private bool _isMenuDrawerMode;

    private bool _hasWideScreenMenuStateBeforeSmallScreen;

    private bool _wideScreenMenuWasOpen;

    private bool _wideScreenCompactMenuWasEnabled;

    private bool _effectiveIsMenuOpen = true;

    private bool _effectiveIsShellChromeVisible = true;

    private bool _effectiveIsShellHeaderVisible = true;

    private bool _effectiveIsPaneToggleVisible;

    private bool _effectiveIsHeaderBackButtonVisible;

    private bool _effectiveIsHeaderMenuToggleVisible;

    private bool _effectiveHasHeaderLeadingButtons;

    private bool _effectiveIsHeaderLeadingButtonSeparatorVisible;

    private bool _canGoBack;

    private int _navigationStackDepth;

    private bool _effectiveIsMenuCompact;

    private LuminaShellPaneDisplayMode _effectivePaneDisplayMode = LuminaShellPaneDisplayMode.Left;

    private object? _effectiveHeaderTitle;

    private double _effectiveOpenPaneLength = 220.0;

    private double _effectiveCompactPaneLength = 48.0;

    private object? _effectiveMenuHeader;

    private object? _effectiveMenuContent;

    private object? _effectiveMenuFooter;

    private Thickness _layoutSafeAreaPadding;

    private Thickness _menuLayoutSafeAreaPadding;

    private Thickness _contentLayoutSafeAreaPadding;

    private Thickness _overlaySafeAreaPadding;

    private Thickness _effectivePageContentPadding;

    public static readonly StyledProperty<bool> IsMenuOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsMenuOpen), defaultValue: true);

    public static readonly StyledProperty<bool> IsShellChromeVisibleProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsShellChromeVisible), defaultValue: true);

    public static readonly StyledProperty<bool> IsShellHeaderVisibleProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsShellHeaderVisible), defaultValue: true);

    public static readonly StyledProperty<bool> IsCompactMenuEnabledProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsCompactMenuEnabled), defaultValue: false);

    public static readonly StyledProperty<bool> CanCompactMenuProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(CanCompactMenu), defaultValue: true);

    public static readonly StyledProperty<bool> IsMenuAutoResponsiveProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsMenuAutoResponsive), defaultValue: true);

    public static readonly StyledProperty<LuminaShellPaneDisplayMode> PaneDisplayModeProperty = AvaloniaProperty.Register<LuminaShell, LuminaShellPaneDisplayMode>(nameof(PaneDisplayMode), LuminaShellPaneDisplayMode.Auto);

    public static readonly StyledProperty<LuminaShellHeaderButtonVisibility> HeaderBackButtonVisibilityProperty = AvaloniaProperty.Register<LuminaShell, LuminaShellHeaderButtonVisibility>(nameof(HeaderBackButtonVisibility), LuminaShellHeaderButtonVisibility.Auto);

    public static readonly StyledProperty<LuminaShellHeaderButtonVisibility> HeaderPaneToggleButtonVisibilityProperty = AvaloniaProperty.Register<LuminaShell, LuminaShellHeaderButtonVisibility>(nameof(HeaderPaneToggleButtonVisibility), LuminaShellHeaderButtonVisibility.Auto);

    public static readonly StyledProperty<bool> CollapseHeaderPaneToggleWhenCanGoBackProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(CollapseHeaderPaneToggleWhenCanGoBack), defaultValue: false);

    public static readonly StyledProperty<Thickness> PageContentPaddingProperty = AvaloniaProperty.Register<LuminaShell, Thickness>(nameof(PageContentPadding));

    public static readonly StyledProperty<Thickness?> HeaderedPageContentPaddingProperty = AvaloniaProperty.Register<LuminaShell, Thickness?>(nameof(HeaderedPageContentPadding));

    public static readonly AttachedProperty<bool> IsMenuCompactProperty = AvaloniaProperty.RegisterAttached<LuminaShell, Control, bool>("IsMenuCompact", defaultValue: false, inherits: true);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsMenuOpenProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsMenuOpen), (LuminaShell shell) => shell.EffectiveIsMenuOpen, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsShellChromeVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsShellChromeVisible), (LuminaShell shell) => shell.EffectiveIsShellChromeVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsShellHeaderVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsShellHeaderVisible), (LuminaShell shell) => shell.EffectiveIsShellHeaderVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsPaneToggleVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsPaneToggleVisible), (LuminaShell shell) => shell.EffectiveIsPaneToggleVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsHeaderBackButtonVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsHeaderBackButtonVisible), (LuminaShell shell) => shell.EffectiveIsHeaderBackButtonVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsHeaderMenuToggleVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsHeaderMenuToggleVisible), (LuminaShell shell) => shell.EffectiveIsHeaderMenuToggleVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveHasHeaderLeadingButtonsProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveHasHeaderLeadingButtons), (LuminaShell shell) => shell.EffectiveHasHeaderLeadingButtons, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsHeaderLeadingButtonSeparatorVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsHeaderLeadingButtonSeparatorVisible), (LuminaShell shell) => shell.EffectiveIsHeaderLeadingButtonSeparatorVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> CanGoBackProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(CanGoBack), (LuminaShell shell) => shell.CanGoBack, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, int> NavigationStackDepthProperty = AvaloniaProperty.RegisterDirect<LuminaShell, int>(nameof(NavigationStackDepth), (LuminaShell shell) => shell.NavigationStackDepth, null, unsetValue: 0);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsMenuCompactProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsMenuCompact), (LuminaShell shell) => shell.EffectiveIsMenuCompact, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, LuminaShellPaneDisplayMode> EffectivePaneDisplayModeProperty = AvaloniaProperty.RegisterDirect<LuminaShell, LuminaShellPaneDisplayMode>(nameof(EffectivePaneDisplayMode), (LuminaShell shell) => shell.EffectivePaneDisplayMode, null, LuminaShellPaneDisplayMode.Left);

    public static readonly DirectProperty<LuminaShell, Thickness> EffectivePageContentPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaShell, Thickness>(nameof(EffectivePageContentPadding), shell => shell.EffectivePageContentPadding);

    public static readonly StyledProperty<string?> ShellKeyProperty = AvaloniaProperty.Register<LuminaShell, string?>(nameof(ShellKey));

    public static readonly StyledProperty<object?> MenuContentProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(MenuContent));

    public static readonly StyledProperty<object?> MenuHeaderProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(MenuHeader));

    public static readonly StyledProperty<object?> MenuFooterProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(MenuFooter));

    public static readonly DirectProperty<LuminaShell, object?> EffectiveMenuHeaderProperty = AvaloniaProperty.RegisterDirect<LuminaShell, object?>(nameof(EffectiveMenuHeader), shell => shell.EffectiveMenuHeader);

    public static readonly DirectProperty<LuminaShell, object?> EffectiveMenuContentProperty = AvaloniaProperty.RegisterDirect<LuminaShell, object?>(nameof(EffectiveMenuContent), shell => shell.EffectiveMenuContent);

    public static readonly DirectProperty<LuminaShell, object?> EffectiveMenuFooterProperty = AvaloniaProperty.RegisterDirect<LuminaShell, object?>(nameof(EffectiveMenuFooter), shell => shell.EffectiveMenuFooter);

    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(Title));

    public static readonly StyledProperty<object?> DefaultPageTitleProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(DefaultPageTitle));

    public static readonly StyledProperty<object?> DefaultPageSubtitleProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(DefaultPageSubtitle));

    public static readonly StyledProperty<object?> DefaultPageActionsProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(DefaultPageActions));

    public static readonly StyledProperty<object?> ActivePageTitleProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(ActivePageTitle));

    public static readonly StyledProperty<object?> ActivePageSubtitleProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(ActivePageSubtitle));

    public static readonly StyledProperty<object?> ActivePageActionsProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(ActivePageActions));

    public static readonly DirectProperty<LuminaShell, object?> EffectiveHeaderTitleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, object?>(nameof(EffectiveHeaderTitle), shell => shell.EffectiveHeaderTitle);

    public static readonly StyledProperty<bool> AutoApplyPageMetadataProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(AutoApplyPageMetadata), defaultValue: true);

    public static readonly StyledProperty<bool> AutoNavigateProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(AutoNavigate), defaultValue: true);

    public static readonly StyledProperty<bool> CachePagesProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(CachePages), defaultValue: true);

    /// <summary>
    /// 根页面切换时使用的过渡动画。默认是一段较短的淡入淡出（移动端更跟手）。
    /// 设为 <c>null</c> 可完全关闭过渡，在低端设备上能进一步提升切换响应速度。
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty = AvaloniaProperty.Register<LuminaShell, IPageTransition?>(
        nameof(PageTransition),
        defaultValue: new CrossFade(TimeSpan.FromMilliseconds(150)));

    public static readonly StyledProperty<bool> CloseMenuOnNavigateProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(CloseMenuOnNavigate), defaultValue: true);

    public static readonly StyledProperty<LuminaPage?> ActivePageProperty = AvaloniaProperty.Register<LuminaShell, LuminaPage?>(nameof(ActivePage));

    public static readonly StyledProperty<Control?> ActiveRouteContentProperty = AvaloniaProperty.Register<LuminaShell, Control?>(nameof(ActiveRouteContent));

    public static readonly StyledProperty<string?> ActiveNavigationKeyProperty = AvaloniaProperty.Register<LuminaShell, string?>(nameof(ActiveNavigationKey), null, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsDialogOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsDialogOpen), defaultValue: false);

    public static readonly StyledProperty<object?> DialogContentProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(DialogContent));

    public static readonly DirectProperty<LuminaShell, object?> ToastContentProperty = AvaloniaProperty.RegisterDirect("ToastContent", (LuminaShell shell) => shell.ToastContent, (LuminaShell shell, object? value) =>
    {
        shell.ToastContent = value;
    });

    public static readonly StyledProperty<bool> IsBottomSheetOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsBottomSheetOpen), defaultValue: false);

    public static readonly StyledProperty<object?> BottomSheetContentProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(BottomSheetContent));

    public static readonly StyledProperty<bool> IsDrawerOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsDrawerOpen), defaultValue: false);

    public static readonly StyledProperty<object?> DrawerContentProperty = AvaloniaProperty.Register<LuminaShell, object?>(nameof(DrawerContent));

    public static readonly StyledProperty<TimeSpan> ToastDurationProperty = AvaloniaProperty.Register<LuminaShell, TimeSpan>(nameof(ToastDuration), TimeSpan.FromSeconds(3));

    public static readonly StyledProperty<IBrush?> PaneBackgroundProperty = AvaloniaProperty.Register<LuminaShell, IBrush?>(nameof(PaneBackground));

    public static readonly StyledProperty<double> OpenPaneLengthProperty = AvaloniaProperty.Register<LuminaShell, double>(nameof(OpenPaneLength), 220.0);

    public static readonly DirectProperty<LuminaShell, double> EffectiveOpenPaneLengthProperty = AvaloniaProperty.RegisterDirect<LuminaShell, double>(nameof(EffectiveOpenPaneLength), (LuminaShell shell) => shell.EffectiveOpenPaneLength, null, 0.0);

    public static readonly StyledProperty<double> CompactPaneLengthProperty = AvaloniaProperty.Register<LuminaShell, double>(nameof(CompactPaneLength), 48.0);

    public static readonly DirectProperty<LuminaShell, double> EffectiveCompactPaneLengthProperty = AvaloniaProperty.RegisterDirect<LuminaShell, double>(nameof(EffectiveCompactPaneLength), (LuminaShell shell) => shell.EffectiveCompactPaneLength, null, 0.0);

    public static readonly StyledProperty<LuminaSafeAreaMode> SafeAreaModeProperty = AvaloniaProperty.Register<LuminaShell, LuminaSafeAreaMode>(nameof(SafeAreaMode), LuminaSafeAreaMode.Auto);

    public static readonly StyledProperty<bool> UseSafeAreaForOverlaysProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(UseSafeAreaForOverlays), defaultValue: true);

    public static readonly DirectProperty<LuminaShell, Thickness> LayoutSafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaShell, Thickness>(nameof(LayoutSafeAreaPadding), shell => shell.LayoutSafeAreaPadding);

    public static readonly DirectProperty<LuminaShell, Thickness> MenuLayoutSafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaShell, Thickness>(nameof(MenuLayoutSafeAreaPadding), shell => shell.MenuLayoutSafeAreaPadding);

    public static readonly DirectProperty<LuminaShell, Thickness> ContentLayoutSafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaShell, Thickness>(nameof(ContentLayoutSafeAreaPadding), shell => shell.ContentLayoutSafeAreaPadding);

    public static readonly DirectProperty<LuminaShell, Thickness> OverlaySafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaShell, Thickness>(nameof(OverlaySafeAreaPadding), shell => shell.OverlaySafeAreaPadding);

    public static readonly StyledProperty<bool> IsWindowGlassEnabledProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsWindowGlassEnabled), defaultValue: false);

    public static LuminaShell? Current { get; private set; }

    public ICommand NavigateCommand { get; }

    public ICommand NavigateBackCommand { get; }

    public ICommand ToggleMenuCommand { get; }

    public ICommand ToggleCompactModeCommand { get; }

    public ICommand CloseDialogCommand { get; }

    public ICommand CloseBottomSheetCommand { get; }

    public ICommand CloseDrawerCommand { get; }

    public ICommand ClearToastCommand { get; }

    public bool IsMenuOpen
    {
        get => GetValue(IsMenuOpenProperty);
        set => SetValue(IsMenuOpenProperty, value);
    }

    public bool IsShellChromeVisible
    {
        get => GetValue(IsShellChromeVisibleProperty);
        set => SetValue(IsShellChromeVisibleProperty, value);
    }

    public bool IsShellHeaderVisible
    {
        get => GetValue(IsShellHeaderVisibleProperty);
        set => SetValue(IsShellHeaderVisibleProperty, value);
    }

    public bool IsCompactMenuEnabled
    {
        get => GetValue(IsCompactMenuEnabledProperty);
        set => SetValue(IsCompactMenuEnabledProperty, value);
    }

    public bool CanCompactMenu
    {
        get => GetValue(CanCompactMenuProperty);
        set => SetValue(CanCompactMenuProperty, value);
    }

    public bool IsMenuAutoResponsive
    {
        get => GetValue(IsMenuAutoResponsiveProperty);
        set => SetValue(IsMenuAutoResponsiveProperty, value);
    }

    public LuminaShellPaneDisplayMode PaneDisplayMode
    {
        get => GetValue(PaneDisplayModeProperty);
        set => SetValue(PaneDisplayModeProperty, value);
    }

    public LuminaShellHeaderButtonVisibility HeaderBackButtonVisibility
    {
        get => GetValue(HeaderBackButtonVisibilityProperty);
        set => SetValue(HeaderBackButtonVisibilityProperty, value);
    }

    public LuminaShellHeaderButtonVisibility HeaderPaneToggleButtonVisibility
    {
        get => GetValue(HeaderPaneToggleButtonVisibilityProperty);
        set => SetValue(HeaderPaneToggleButtonVisibilityProperty, value);
    }

    public bool CollapseHeaderPaneToggleWhenCanGoBack
    {
        get => GetValue(CollapseHeaderPaneToggleWhenCanGoBackProperty);
        set => SetValue(CollapseHeaderPaneToggleWhenCanGoBackProperty, value);
    }

    public Thickness PageContentPadding
    {
        get => GetValue(PageContentPaddingProperty);
        set => SetValue(PageContentPaddingProperty, value);
    }

    public Thickness? HeaderedPageContentPadding
    {
        get => GetValue(HeaderedPageContentPaddingProperty);
        set => SetValue(HeaderedPageContentPaddingProperty, value);
    }

    public bool EffectiveIsShellChromeVisible
    {
        get
        {
            return _effectiveIsShellChromeVisible;
        }
        private set
        {
            SetAndRaise(EffectiveIsShellChromeVisibleProperty, ref _effectiveIsShellChromeVisible, value);
        }
    }

    public bool EffectiveIsShellHeaderVisible
    {
        get
        {
            return _effectiveIsShellHeaderVisible;
        }
        private set
        {
            SetAndRaise(EffectiveIsShellHeaderVisibleProperty, ref _effectiveIsShellHeaderVisible, value);
        }
    }

    public bool EffectiveIsMenuOpen
    {
        get
        {
            return _effectiveIsMenuOpen;
        }
        private set
        {
            SetAndRaise(EffectiveIsMenuOpenProperty, ref _effectiveIsMenuOpen, value);
        }
    }

    public bool EffectiveIsPaneToggleVisible
    {
        get
        {
            return _effectiveIsPaneToggleVisible;
        }
        private set
        {
            if (SetAndRaise(EffectiveIsPaneToggleVisibleProperty, ref _effectiveIsPaneToggleVisible, value))
            {
                UpdateEffectiveHeaderMenuToggleVisible();
            }
        }
    }

    public bool EffectiveIsHeaderBackButtonVisible
    {
        get
        {
            return _effectiveIsHeaderBackButtonVisible;
        }
        private set
        {
            SetAndRaise(EffectiveIsHeaderBackButtonVisibleProperty, ref _effectiveIsHeaderBackButtonVisible, value);
        }
    }

    public bool EffectiveIsHeaderMenuToggleVisible
    {
        get
        {
            return _effectiveIsHeaderMenuToggleVisible;
        }
        private set
        {
            SetAndRaise(EffectiveIsHeaderMenuToggleVisibleProperty, ref _effectiveIsHeaderMenuToggleVisible, value);
        }
    }

    public bool EffectiveHasHeaderLeadingButtons
    {
        get
        {
            return _effectiveHasHeaderLeadingButtons;
        }
        private set
        {
            SetAndRaise(EffectiveHasHeaderLeadingButtonsProperty, ref _effectiveHasHeaderLeadingButtons, value);
        }
    }

    public bool EffectiveIsHeaderLeadingButtonSeparatorVisible
    {
        get
        {
            return _effectiveIsHeaderLeadingButtonSeparatorVisible;
        }
        private set
        {
            SetAndRaise(EffectiveIsHeaderLeadingButtonSeparatorVisibleProperty, ref _effectiveIsHeaderLeadingButtonSeparatorVisible, value);
        }
    }

    public bool CanGoBack
    {
        get
        {
            return _canGoBack;
        }
        private set
        {
            if (SetAndRaise(CanGoBackProperty, ref _canGoBack, value))
            {
                UpdateEffectiveHeaderBackButtonVisible();
                UpdateEffectiveHeaderMenuToggleVisible();
            }
        }
    }

    public int NavigationStackDepth
    {
        get
        {
            return _navigationStackDepth;
        }
        private set
        {
            SetAndRaise(NavigationStackDepthProperty, ref _navigationStackDepth, value);
        }
    }

    public bool EffectiveIsMenuCompact
    {
        get
        {
            return _effectiveIsMenuCompact;
        }
        private set
        {
            SetAndRaise(EffectiveIsMenuCompactProperty, ref _effectiveIsMenuCompact, value);
        }
    }

    public LuminaShellPaneDisplayMode EffectivePaneDisplayMode
    {
        get
        {
            return _effectivePaneDisplayMode;
        }
        private set
        {
            SetAndRaise(EffectivePaneDisplayModeProperty, ref _effectivePaneDisplayMode, value);
        }
    }

    public Thickness EffectivePageContentPadding
    {
        get
        {
            return _effectivePageContentPadding;
        }
        private set
        {
            SetAndRaise(EffectivePageContentPaddingProperty, ref _effectivePageContentPadding, value);
        }
    }

    public string? ShellKey
    {
        get => GetValue(ShellKeyProperty);
        set => SetValue(ShellKeyProperty, value);
    }

    public object? MenuContent
    {
        get => GetValue(MenuContentProperty);
        set => SetValue(MenuContentProperty, value);
    }

    public object? MenuHeader
    {
        get => GetValue(MenuHeaderProperty);
        set => SetValue(MenuHeaderProperty, value);
    }

    public object? MenuFooter
    {
        get => GetValue(MenuFooterProperty);
        set => SetValue(MenuFooterProperty, value);
    }

    public object? EffectiveMenuHeader
    {
        get
        {
            return _effectiveMenuHeader;
        }
        private set
        {
            SetAndRaise(EffectiveMenuHeaderProperty, ref _effectiveMenuHeader, value);
        }
    }

    public object? EffectiveMenuContent
    {
        get
        {
            return _effectiveMenuContent;
        }
        private set
        {
            SetAndRaise(EffectiveMenuContentProperty, ref _effectiveMenuContent, value);
        }
    }

    public object? EffectiveMenuFooter
    {
        get
        {
            return _effectiveMenuFooter;
        }
        private set
        {
            SetAndRaise(EffectiveMenuFooterProperty, ref _effectiveMenuFooter, value);
        }
    }

    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? DefaultPageTitle
    {
        get => GetValue(DefaultPageTitleProperty);
        set => SetValue(DefaultPageTitleProperty, value);
    }

    public object? DefaultPageSubtitle
    {
        get => GetValue(DefaultPageSubtitleProperty);
        set => SetValue(DefaultPageSubtitleProperty, value);
    }

    public object? DefaultPageActions
    {
        get => GetValue(DefaultPageActionsProperty);
        set => SetValue(DefaultPageActionsProperty, value);
    }

    public object? ActivePageTitle
    {
        get => GetValue(ActivePageTitleProperty);
        set => SetValue(ActivePageTitleProperty, value);
    }

    public object? ActivePageSubtitle
    {
        get => GetValue(ActivePageSubtitleProperty);
        set => SetValue(ActivePageSubtitleProperty, value);
    }

    public object? ActivePageActions
    {
        get => GetValue(ActivePageActionsProperty);
        set => SetValue(ActivePageActionsProperty, value);
    }

    public object? EffectiveHeaderTitle
    {
        get
        {
            return _effectiveHeaderTitle;
        }
        private set
        {
            SetAndRaise(EffectiveHeaderTitleProperty, ref _effectiveHeaderTitle, value);
        }
    }

    public bool AutoApplyPageMetadata
    {
        get => GetValue(AutoApplyPageMetadataProperty);
        set => SetValue(AutoApplyPageMetadataProperty, value);
    }

    public bool AutoNavigate
    {
        get => GetValue(AutoNavigateProperty);
        set => SetValue(AutoNavigateProperty, value);
    }

    public bool CachePages
    {
        get => GetValue(CachePagesProperty);
        set => SetValue(CachePagesProperty, value);
    }

    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    public bool CloseMenuOnNavigate
    {
        get => GetValue(CloseMenuOnNavigateProperty);
        set => SetValue(CloseMenuOnNavigateProperty, value);
    }

    public LuminaPage? ActivePage
    {
        get
        {
            return GetValue(ActivePageProperty);
        }
        private set
        {
            SetValue(ActivePageProperty, value);
        }
    }

    public Control? ActiveRouteContent
    {
        get
        {
            return GetValue(ActiveRouteContentProperty);
        }
        private set
        {
            SetValue(ActiveRouteContentProperty, value);
        }
    }

    public string? ActiveNavigationKey
    {
        get => GetValue(ActiveNavigationKeyProperty);
        set => SetValue(ActiveNavigationKeyProperty, value);
    }

    public IEnumerable<Control> CachedRouteContents => _routeCache.Values;

    public bool IsDialogOpen
    {
        get => GetValue(IsDialogOpenProperty);
        set => SetValue(IsDialogOpenProperty, value);
    }

    public object? DialogContent
    {
        get => GetValue(DialogContentProperty);
        set => SetValue(DialogContentProperty, value);
    }

    public object? ToastContent
    {
        get
        {
            return _toastContent;
        }
        set
        {
            SetAndRaise(ToastContentProperty, ref _toastContent, value);
        }
    }

    public bool IsBottomSheetOpen
    {
        get => GetValue(IsBottomSheetOpenProperty);
        set => SetValue(IsBottomSheetOpenProperty, value);
    }

    public object? BottomSheetContent
    {
        get => GetValue(BottomSheetContentProperty);
        set => SetValue(BottomSheetContentProperty, value);
    }

    public bool IsDrawerOpen
    {
        get => GetValue(IsDrawerOpenProperty);
        set => SetValue(IsDrawerOpenProperty, value);
    }

    public object? DrawerContent
    {
        get => GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    public TimeSpan ToastDuration
    {
        get => GetValue(ToastDurationProperty);
        set => SetValue(ToastDurationProperty, value);
    }

    public IBrush? PaneBackground
    {
        get => GetValue(PaneBackgroundProperty);
        set => SetValue(PaneBackgroundProperty, value);
    }

    public double OpenPaneLength
    {
        get => GetValue(OpenPaneLengthProperty);
        set => SetValue(OpenPaneLengthProperty, value);
    }

    public double EffectiveOpenPaneLength
    {
        get
        {
            return _effectiveOpenPaneLength;
        }
        private set
        {
            SetAndRaise(EffectiveOpenPaneLengthProperty, ref _effectiveOpenPaneLength, value);
        }
    }

    public double CompactPaneLength
    {
        get => GetValue(CompactPaneLengthProperty);
        set => SetValue(CompactPaneLengthProperty, value);
    }

    public double EffectiveCompactPaneLength
    {
        get
        {
            return _effectiveCompactPaneLength;
        }
        private set
        {
            SetAndRaise(EffectiveCompactPaneLengthProperty, ref _effectiveCompactPaneLength, value);
        }
    }

    public LuminaSafeAreaMode SafeAreaMode
    {
        get => GetValue(SafeAreaModeProperty);
        set => SetValue(SafeAreaModeProperty, value);
    }

    public bool UseSafeAreaForOverlays
    {
        get => GetValue(UseSafeAreaForOverlaysProperty);
        set => SetValue(UseSafeAreaForOverlaysProperty, value);
    }

    public Thickness LayoutSafeAreaPadding
    {
        get
        {
            return _layoutSafeAreaPadding;
        }
        private set
        {
            if (SetAndRaise(LayoutSafeAreaPaddingProperty, ref _layoutSafeAreaPadding, value))
            {
                UpdateLayoutSafeAreaPartitions();
            }
        }
    }

    public Thickness MenuLayoutSafeAreaPadding
    {
        get
        {
            return _menuLayoutSafeAreaPadding;
        }
        private set
        {
            SetAndRaise(MenuLayoutSafeAreaPaddingProperty, ref _menuLayoutSafeAreaPadding, value);
        }
    }

    public Thickness ContentLayoutSafeAreaPadding
    {
        get
        {
            return _contentLayoutSafeAreaPadding;
        }
        private set
        {
            SetAndRaise(ContentLayoutSafeAreaPaddingProperty, ref _contentLayoutSafeAreaPadding, value);
        }
    }

    public Thickness OverlaySafeAreaPadding
    {
        get
        {
            return _overlaySafeAreaPadding;
        }
        private set
        {
            SetAndRaise(OverlaySafeAreaPaddingProperty, ref _overlaySafeAreaPadding, value);
        }
    }

    public bool IsWindowGlassEnabled
    {
        get => GetValue(IsWindowGlassEnabledProperty);
        set => SetValue(IsWindowGlassEnabledProperty, value);
    }

    public static bool GetIsMenuCompact(Control control)
    {
        return control.GetValue(IsMenuCompactProperty);
    }

    public static void SetIsMenuCompact(Control control, bool value)
    {
        control.SetValue(IsMenuCompactProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RegisterAttachedShell();
        AttachBackRequestedHandler();
        SyncNavigationHostContent();
        UpdateEffectiveShellChrome();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachBackRequestedHandler();
        ObserveOverlayHost(null);
        _overlayHost = null;
        CloseMenuDrawer(forceClearContent: true);
        ObserveNavigationHost(null);
        _navigationHost = null;
        UpdateNavigationStackState();
        if (_activePage != null)
        {
            _activePage.PropertyChanged -= OnActivePagePropertyChanged;
            _activePage = null;
        }
        ActivePage = null;
        ActiveRouteContent = null;
        base.OnDetachedFromVisualTree(e);
        UnregisterAttachedShell();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        ObserveOverlayHost(null);
        _overlayHost = e.NameScope.FindRequired<LuminaOverlayHost>("PART_OverlayHost");
        ObserveOverlayHost(_overlayHost);
        SyncOverlayHostFromShell();
        ObserveNavigationHost(null);
        _navigationHost = e.NameScope.FindRequired<NavigationPage>("PART_NavigationHost");
        ObserveNavigationHost(_navigationHost);
        SyncNavigationHostContent();
        UpdateNavigationStackState();
    }

    private void ObserveOverlayHost(LuminaOverlayHost? overlayHost)
    {
        if (ReferenceEquals(_observedOverlayHost, overlayHost))
        {
            return;
        }

        if (_observedOverlayHost != null)
        {
            _observedOverlayHost.PropertyChanged -= OnOverlayHostPropertyChanged;
        }

        _observedOverlayHost = overlayHost;
        if (_observedOverlayHost != null)
        {
            _observedOverlayHost.PropertyChanged += OnOverlayHostPropertyChanged;
        }
    }

    private void OnOverlayHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _overlayHost) || _syncingOverlayHostFromShell)
        {
            return;
        }

        _syncingOverlayHostFromHost = true;
        try
        {
            if (e.Property == LuminaOverlayHost.IsDialogOpenProperty)
            {
                IsDialogOpen = _overlayHost!.IsDialogOpen;
            }
            else if (e.Property == LuminaOverlayHost.DialogContentProperty)
            {
                DialogContent = _overlayHost!.DialogContent;
            }
            else if (e.Property == LuminaOverlayHost.ToastContentProperty)
            {
                ToastContent = _overlayHost!.ToastContent;
            }
            else if (e.Property == LuminaOverlayHost.IsBottomSheetOpenProperty)
            {
                IsBottomSheetOpen = _overlayHost!.IsBottomSheetOpen;
            }
            else if (e.Property == LuminaOverlayHost.BottomSheetContentProperty)
            {
                BottomSheetContent = _overlayHost!.BottomSheetContent;
            }
            else if (e.Property == LuminaOverlayHost.IsDrawerOpenProperty)
            {
                IsDrawerOpen = _overlayHost!.IsDrawerOpen;
            }
            else if (e.Property == LuminaOverlayHost.DrawerContentProperty)
            {
                DrawerContent = _overlayHost!.DrawerContent;
            }
            else if (e.Property == LuminaOverlayHost.ToastDurationProperty)
            {
                ToastDuration = _overlayHost!.ToastDuration;
            }
            else if (e.Property == LuminaOverlayHost.LayoutSafeAreaPaddingProperty)
            {
                LayoutSafeAreaPadding = _overlayHost!.LayoutSafeAreaPadding;
            }
            else if (e.Property == LuminaOverlayHost.OverlaySafeAreaPaddingProperty)
            {
                OverlaySafeAreaPadding = _overlayHost!.OverlaySafeAreaPadding;
            }
        }
        finally
        {
            _syncingOverlayHostFromHost = false;
        }
    }

    private void SyncOverlayHostFromShell()
    {
        if (_overlayHost == null || _syncingOverlayHostFromHost)
        {
            return;
        }

        _syncingOverlayHostFromShell = true;
        try
        {
            _overlayHost.ToastDuration = ToastDuration;
            _overlayHost.DialogContent = DialogContent;
            _overlayHost.IsDialogOpen = IsDialogOpen;
            _overlayHost.ToastContent = ToastContent;
            _overlayHost.BottomSheetContent = BottomSheetContent;
            _overlayHost.IsBottomSheetOpen = IsBottomSheetOpen;
            _overlayHost.DrawerContent = DrawerContent;
            _overlayHost.IsDrawerOpen = IsDrawerOpen;
            LayoutSafeAreaPadding = _overlayHost.LayoutSafeAreaPadding;
            OverlaySafeAreaPadding = _overlayHost.OverlaySafeAreaPadding;
        }
        finally
        {
            _syncingOverlayHostFromShell = false;
        }
    }

    private void SyncOverlayHostProperty(AvaloniaProperty property)
    {
        if (_overlayHost == null || _syncingOverlayHostFromHost)
        {
            return;
        }

        _syncingOverlayHostFromShell = true;
        try
        {
            if (property == IsDialogOpenProperty)
            {
                _overlayHost.IsDialogOpen = IsDialogOpen;
            }
            else if (property == DialogContentProperty)
            {
                _overlayHost.DialogContent = DialogContent;
            }
            else if (property == ToastContentProperty)
            {
                _overlayHost.ToastContent = ToastContent;
            }
            else if (property == IsBottomSheetOpenProperty)
            {
                _overlayHost.IsBottomSheetOpen = IsBottomSheetOpen;
            }
            else if (property == BottomSheetContentProperty)
            {
                _overlayHost.BottomSheetContent = BottomSheetContent;
            }
            else if (property == IsDrawerOpenProperty)
            {
                CloseMenuDrawerForCustomDrawer();
                _overlayHost.IsDrawerOpen = IsDrawerOpen;
            }
            else if (property == DrawerContentProperty)
            {
                CloseMenuDrawerForCustomDrawer();
                _overlayHost.DrawerContent = DrawerContent;
            }
            else if (property == ToastDurationProperty)
            {
                _overlayHost.ToastDuration = ToastDuration;
            }
        }
        finally
        {
            _syncingOverlayHostFromShell = false;
        }
    }

    private bool IsMenuDrawerUsingOverlay()
    {
        return _menuDrawer != null && ReferenceEquals(_menuDrawerHost?.DrawerContent, _menuDrawer);
    }

    private void CloseMenuDrawerForCustomDrawer()
    {
        if (!IsMenuDrawerUsingOverlay())
        {
            return;
        }

        CloseMenuDrawer(forceClearContent: true);
        if (IsMenuOpen)
        {
            IsMenuOpen = false;
        }
    }

    private void SetMenuDrawerMode(bool value)
    {
        if (_isMenuDrawerMode == value)
        {
            UpdateEffectiveMenuSlots();
            return;
        }

        if (!value)
        {
            CloseMenuDrawer(forceClearContent: true);
        }

        _isMenuDrawerMode = value;
        UpdateEffectiveMenuSlots();
    }

    private void SyncMenuDrawer(LuminaOverlayHost? host)
    {
        if (_syncingMenuDrawer)
        {
            return;
        }

        if (!_isMenuDrawerMode || !EffectiveIsShellChromeVisible || !IsMenuOpen)
        {
            CloseMenuDrawer(forceClearContent: false);
            return;
        }

        if (host == null)
        {
            CloseMenuDrawer(forceClearContent: true);
            return;
        }

        if (_menuDrawerHost != host)
        {
            CloseMenuDrawer(forceClearContent: true);
            _menuDrawerHost = host;
            _menuDrawerHost.PropertyChanged += OnMenuDrawerHostPropertyChanged;
        }

        OpenMenuDrawer(host);
    }

    private void OpenMenuDrawer(LuminaOverlayHost host)
    {
        if (_syncingMenuDrawer || !_isMenuDrawerMode || !EffectiveIsShellChromeVisible || !IsMenuOpen || !ReferenceEquals(host, _menuDrawerHost))
        {
            return;
        }

        if (_menuDrawer == null)
        {
            _menuDrawer = CreateMenuDrawer();
        }

        if (ReferenceEquals(host.DrawerContent, _menuDrawer))
        {
            _menuDrawer.SafeAreaPadding = host.OverlaySafeAreaPadding;
            if (!host.IsDrawerOpen)
            {
                host.IsDrawerOpen = true;
            }
            return;
        }

        _syncingMenuDrawer = true;
        try
        {
            host.ShowDrawer(_menuDrawer);
        }
        finally
        {
            _syncingMenuDrawer = false;
        }
    }

    private LuminaDrawer CreateMenuDrawer()
    {
        LuminaDrawer drawer = new LuminaDrawer
        {
            Placement = DrawerPlacement.Left,
            ContentPadding = default,
            BorderThickness = LuminaPickerResources.Thickness("LuminaShellTopMenuDrawerBorderThickness", new Thickness(0, 0, 1, 0)),
            Content = CreateMenuDrawerContent()
        };
        LuminaPickerResources.BindResource(drawer, TemplatedControl.CornerRadiusProperty, "LuminaShellTopMenuDrawerCornerRadius");
        drawer.Bind(TemplatedControl.BackgroundProperty, this.GetObservable(PaneBackgroundProperty));
        drawer.Bind(LuminaDrawer.DrawerLengthProperty, this.GetObservable(OpenPaneLengthProperty));
        return drawer;
    }

    private Control CreateMenuDrawerContent()
    {
        Grid root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto")
        };
        SetIsMenuCompact(root, false);

        ContentPresenter headerPresenter = new ContentPresenter
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        _menuDrawerHeaderPresenter = headerPresenter;
        _menuDrawerHeaderBinding = headerPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(MenuHeaderProperty));

        Border header = new Border
        {
            Name = "PART_TopMenuDrawerHeader",
            Margin = LuminaPickerResources.Thickness("LuminaShellTopMenuDrawerHeaderMargin", new Thickness(16, 10, 16, 8)),
            Height = LuminaPickerResources.Double("LuminaShellTopMenuDrawerHeaderHeight", 44),
            ClipToBounds = true,
            Child = headerPresenter
        };

        ContentPresenter menuContentPresenter = new ContentPresenter();
        _menuDrawerContentPresenter = menuContentPresenter;
        _menuDrawerContentBinding = menuContentPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(MenuContentProperty));

        ScrollViewer scrollViewer = new ScrollViewer
        {
            Name = "PART_TopMenuDrawerScrollViewer",
            Margin = LuminaPickerResources.Thickness("LuminaShellTopMenuDrawerScrollMargin", new Thickness(16, 0, 16, 12)),
            BringIntoViewOnFocusChange = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            Content = menuContentPresenter
        };

        ContentPresenter footerPresenter = new ContentPresenter();
        _menuDrawerFooterPresenter = footerPresenter;
        _menuDrawerFooterBinding = footerPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(MenuFooterProperty));

        Border footer = new Border
        {
            Name = "PART_TopMenuDrawerFooter",
            Margin = LuminaPickerResources.Thickness("LuminaShellTopMenuDrawerFooterMargin", new Thickness(16, 12, 16, 12)),
            Padding = LuminaPickerResources.Thickness("LuminaShellMenuFooterPadding", new Thickness(0, 16, 0, 0)),
            ClipToBounds = false,
            Child = footerPresenter
        };
        Grid.SetRow(scrollViewer, 1);
        Grid.SetRow(footer, 2);

        root.Children.Add(header);
        root.Children.Add(scrollViewer);
        root.Children.Add(footer);
        return root;
    }

    private void CloseMenuDrawer(bool forceClearContent)
    {
        LuminaOverlayHost? host = _menuDrawerHost;
        LuminaDrawer? drawer = _menuDrawer;
        if (host == null)
        {
            if (forceClearContent)
            {
                ReleaseMenuDrawerReference();
            }
            return;
        }

        bool previousSyncingMenuDrawer = _syncingMenuDrawer;
        if (forceClearContent)
        {
            _syncingMenuDrawer = true;
        }

        try
        {
            if (ReferenceEquals(host.DrawerContent, drawer))
            {
                host.CloseDrawer();
                if (forceClearContent)
                {
                    host.DrawerContent = null;
                }
            }
        }
        finally
        {
            _syncingMenuDrawer = previousSyncingMenuDrawer;
        }

        if (forceClearContent)
        {
            ReleaseMenuDrawerReference();
        }
    }

    private void ReleaseMenuDrawerReference()
    {
        if (_menuDrawerHost != null)
        {
            _menuDrawerHost.PropertyChanged -= OnMenuDrawerHostPropertyChanged;
        }
        ClearMenuDrawerSlots();
        _menuDrawerHost = null;
        _menuDrawer = null;
    }

    private void ClearMenuDrawerSlots()
    {
        _menuDrawerHeaderBinding?.Dispose();
        _menuDrawerContentBinding?.Dispose();
        _menuDrawerFooterBinding?.Dispose();
        _menuDrawerHeaderBinding = null;
        _menuDrawerContentBinding = null;
        _menuDrawerFooterBinding = null;

        if (_menuDrawerHeaderPresenter != null)
        {
            _menuDrawerHeaderPresenter.Content = null;
            _menuDrawerHeaderPresenter = null;
        }
        if (_menuDrawerContentPresenter != null)
        {
            _menuDrawerContentPresenter.Content = null;
            _menuDrawerContentPresenter = null;
        }
        if (_menuDrawerFooterPresenter != null)
        {
            _menuDrawerFooterPresenter.Content = null;
            _menuDrawerFooterPresenter = null;
        }
        if (_menuDrawer != null)
        {
            _menuDrawer.Content = null;
        }
    }

    private void OnMenuDrawerHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncingMenuDrawer || !ReferenceEquals(sender, _menuDrawerHost))
        {
            return;
        }

        if (sender is not LuminaOverlayHost host)
        {
            return;
        }

        if (e.Property == LuminaOverlayHost.IsDrawerOpenProperty && !host.IsDrawerOpen && ReferenceEquals(host.DrawerContent, _menuDrawer))
        {
            SetMenuOpenFromDrawer(false);
        }
        else if (e.Property == LuminaOverlayHost.DrawerContentProperty && host.DrawerContent != null && !ReferenceEquals(host.DrawerContent, _menuDrawer))
        {
            bool wasMenuOpen = IsMenuOpen;
            ReleaseMenuDrawerReference();
            if (wasMenuOpen)
            {
                SetMenuOpenFromDrawer(false);
            }
        }
    }

    private void SetMenuOpenFromDrawer(bool value)
    {
        _syncingMenuDrawer = true;
        try
        {
            IsMenuOpen = value;
        }
        finally
        {
            _syncingMenuDrawer = false;
        }
        UpdateEffectiveShellChrome();
    }

    private void ObserveNavigationHost(NavigationPage? navigationHost)
    {
        if (ReferenceEquals(_observedNavigationHost, navigationHost))
        {
            return;
        }

        if (_observedNavigationHost != null)
        {
            _observedNavigationHost.PropertyChanged -= OnNavigationHostPropertyChanged;
        }

        _observedNavigationHost = navigationHost;
        if (_observedNavigationHost != null)
        {
            _observedNavigationHost.PropertyChanged += OnNavigationHostPropertyChanged;
        }
    }

    private void OnNavigationHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _navigationHost))
        {
            return;
        }

        if (e.Property == Page.CurrentPageProperty)
        {
            if (!_isResettingNavigationHost)
            {
                SetActivePage(_navigationHost?.CurrentPage ?? _activeRoutePage);
            }
            UpdateNavigationStackState();
        }
        else if (e.Property == NavigationPage.CanGoBackProperty || e.Property == NavigationPage.IsNavigatingProperty)
        {
            UpdateNavigationStackState();

            // 导航结束时唤醒所有等待空闲的任务，实现事件驱动等待（替代轮询）。
            if (e.Property == NavigationPage.IsNavigatingProperty && _navigationHost?.IsNavigating == false)
            {
                CompleteNavigationIdleWaiters();
            }
        }
    }

    private void CompleteNavigationIdleWaiters()
    {
        TaskCompletionSource<bool>? completion = _navigationIdleCompletion;
        _navigationIdleCompletion = null;
        completion?.TrySetResult(true);
    }

    private void AttachBackRequestedHandler()
    {
        DetachBackRequestedHandler();
        _backRequestedTopLevel = TopLevel.GetTopLevel(this);
        if (_backRequestedTopLevel != null)
        {
            _backRequestedTopLevel.BackRequested += OnTopLevelBackRequested;
        }
    }

    private void DetachBackRequestedHandler()
    {
        if (_backRequestedTopLevel != null)
        {
            _backRequestedTopLevel.BackRequested -= OnTopLevelBackRequested;
            _backRequestedTopLevel = null;
        }
    }

    private void OnTopLevelBackRequested(object? sender, RoutedEventArgs e)
    {
        if (!e.Handled && TryHandleSystemBackRequested())
        {
            e.Handled = true;
        }
    }

    private bool TryHandleSystemBackRequested()
    {
        if (_overlayHost?.TryHandleSystemBackRequested() == true)
        {
            return true;
        }

        if (IsDialogOpen)
        {
            CloseDialog();
            return true;
        }

        if (IsBottomSheetOpen)
        {
            CloseBottomSheet();
            return true;
        }

        if (IsDrawerOpen)
        {
            CloseDrawer();
            return true;
        }

        if (ShouldCloseMenuOnSystemBack())
        {
            IsMenuOpen = false;
            return true;
        }

        return false;
    }

    public LuminaShell()
    {
        NavigateCommand = new LuminaRelayCommand((object? parameter) =>
        {
            if (parameter is string navigationKey)
            {
                NavigateTo(navigationKey);
            }
        }, (object? parameter) => parameter is string value && !string.IsNullOrWhiteSpace(value));
        NavigateBackCommand = new LuminaRelayCommand(_ => {
            _ = PopAsync();
        }, _ => CanNavigateBack());
        ToggleMenuCommand = new LuminaRelayCommand(_ => {
            IsMenuOpen = !IsMenuOpen;
        });
        ToggleCompactModeCommand = new LuminaRelayCommand(_ => {
            if (!CanCompactMenu || Bounds.Width < SmallScreenBreakpoint)
            {
                IsMenuOpen = !IsMenuOpen;
                return;
            }
            if (EffectiveIsMenuCompact)
            {
                IsMenuOpen = true;
                return;
            }
            IsCompactMenuEnabled = true;
            IsMenuOpen = false;
        });
        CloseDialogCommand = new LuminaRelayCommand(_ => {
            CloseDialog();
        });
        CloseBottomSheetCommand = new LuminaRelayCommand(_ => {
            CloseBottomSheet();
        });
        CloseDrawerCommand = new LuminaRelayCommand(_ => {
            CloseDrawer();
        });
        ClearToastCommand = new LuminaRelayCommand(_ => {
            ClearToast();
        });
    }

    public void ShowToast(object? content)
    {
        ShowToast(content, ToastDuration);
    }

    public void ShowToast(object? content, TimeSpan duration)
    {
        if (_overlayHost != null)
        {
            _overlayHost.ShowToast(content, duration);
            return;
        }

        ToastContent = content;
    }

    public void ClearToast()
    {
        if (_overlayHost != null)
        {
            _overlayHost.ClearToast();
            return;
        }

        ToastContent = null;
    }

    public void ShowDialog(object? content)
    {
        if (_overlayHost != null)
        {
            _overlayHost.ShowDialog(content);
            return;
        }

        DialogContent = content;
        IsDialogOpen = content != null;
    }

    public void CloseDialog()
    {
        if (_overlayHost != null)
        {
            _overlayHost.CloseDialog();
            return;
        }

        IsDialogOpen = false;
    }

    public void ShowBottomSheet(object? content)
    {
        if (_overlayHost != null)
        {
            _overlayHost.ShowBottomSheet(content);
            return;
        }

        BottomSheetContent = LuminaBottomSheet.EnsureSheet(content);
        IsBottomSheetOpen = BottomSheetContent != null;
    }

    public void CloseBottomSheet()
    {
        if (_overlayHost != null)
        {
            _overlayHost.CloseBottomSheet();
            return;
        }

        IsBottomSheetOpen = false;
    }

    public void ShowDrawer(object? content)
    {
        CloseMenuDrawerForCustomDrawer();
        if (_overlayHost != null)
        {
            _overlayHost.ShowDrawer(content);
            return;
        }

        DrawerContent = LuminaDrawer.EnsureDrawer(content);
        IsDrawerOpen = DrawerContent != null;
    }

    public void CloseDrawer()
    {
        if (IsMenuDrawerUsingOverlay())
        {
            IsMenuOpen = false;
            CloseMenuDrawer(forceClearContent: false);
            return;
        }

        if (_overlayHost != null)
        {
            _overlayHost.CloseDrawer();
            return;
        }

        IsDrawerOpen = false;
    }

    public void RegisterRoute(string navigationKey, Func<Control> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(navigationKey, "navigationKey");
        ArgumentNullException.ThrowIfNull(factory, "factory");
        _routeFactories[navigationKey] = factory;
        _routeCache.Remove(navigationKey);
        _routePageCache.Remove(navigationKey);
    }

    public bool UnregisterRoute(string navigationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(navigationKey, "navigationKey");
        _routeCache.Remove(navigationKey);
        _routePageCache.Remove(navigationKey);
        return _routeFactories.Remove(navigationKey);
    }

    public void ClearRoutes()
    {
        _routeFactories.Clear();
        _routeCache.Clear();
        _routePageCache.Clear();
    }

    public bool NavigateTo(string navigationKey)
    {
        return NavigateTo(navigationKey, CloseMenuOnNavigate);
    }

    public bool NavigateTo(string navigationKey, bool closeMenuOnNavigate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(navigationKey, "navigationKey");
        if (!_routeFactories.TryGetValue(navigationKey, out Func<Control>? factory))
        {
            return false;
        }
        Page page = GetRoutePage(navigationKey, factory);
        _isNavigating = true;
        try
        {
            SetRoutePage(page);
            SetActiveNavigationKey(navigationKey);
        }
        finally
        {
            _isNavigating = false;
        }
        ApplyActivePageMetadata();
        if (closeMenuOnNavigate && ShouldCloseMenuOnNavigate())
        {
            IsMenuOpen = false;
        }
        return true;
    }

    public Task PushAsync(Control content)
    {
        return PushAsync(content, options: null);
    }

    public Task PushAsync(Control content, IPageTransition? pageTransition)
    {
        return PushAsync(content, new LuminaShellPushOptions(pageTransition));
    }

    public Task PushAsync(Control content, LuminaShellPushOptions? options)
    {
        ArgumentNullException.ThrowIfNull(content, "content");
        Page page = content as Page ?? new ContentPage { Content = content };
        return PushAsync(page, options);
    }

    public Task PushAsync(Page page)
    {
        return PushAsync(page, options: null);
    }

    public Task PushAsync(Page page, IPageTransition? pageTransition)
    {
        return PushAsync(page, new LuminaShellPushOptions(pageTransition));
    }

    public async Task PushAsync(Page page, LuminaShellPushOptions? options)
    {
        ArgumentNullException.ThrowIfNull(page, "page");
        NavigationPage navigationHost = GetNavigationHostForStackOperation();
        StorePushOptions(page, options);
        ConfigureShellHostedPage(page);
        if (!await WaitForNavigationHostReadyAsync(navigationHost))
        {
            return;
        }

        if (navigationHost.StackDepth == 0 || navigationHost.Content == null)
        {
            _activeRoutePage = page;
            navigationHost.Content = page;
            SetActivePage(page);
            UpdateNavigationStackState();
            return;
        }

        await PushWithOptionsAsync(navigationHost, page, options);
        await WaitForNavigationHostReadyAsync(navigationHost);
        SetActivePage(navigationHost.CurrentPage ?? page);
        UpdateNavigationStackState();
    }

    private async Task PushWithOptionsAsync(NavigationPage navigationHost, Page page, LuminaShellPushOptions? options)
    {
        if (options?.HasPageTransitionOverride != true)
        {
            await navigationHost.PushAsync(page);
            return;
        }

        IPageTransition? previousTransition = navigationHost.PageTransition;
        navigationHost.PageTransition = options.PageTransition;
        try
        {
            await navigationHost.PushAsync(page);
        }
        finally
        {
            navigationHost.PageTransition = previousTransition;
        }
    }

    public async Task PopAsync()
    {
        NavigationPage? navigationHost = _navigationHost;
        if (navigationHost == null)
        {
            return;
        }

        if (!await WaitForNavigationHostReadyAsync(navigationHost))
        {
            return;
        }

        if (!navigationHost.CanGoBack)
        {
            UpdateNavigationStackState();
            return;
        }

        await navigationHost.PopAsync();
        await WaitForNavigationHostReadyAsync(navigationHost);
        SetActivePage(navigationHost.CurrentPage ?? _activeRoutePage);
        UpdateNavigationStackState();
    }

    public async Task PopToRootAsync()
    {
        NavigationPage? navigationHost = _navigationHost;
        if (navigationHost == null)
        {
            return;
        }

        if (!await WaitForNavigationHostReadyAsync(navigationHost))
        {
            return;
        }

        if (navigationHost.StackDepth <= 1)
        {
            SetActivePage(navigationHost.CurrentPage ?? _activeRoutePage);
            UpdateNavigationStackState();
            return;
        }

        await navigationHost.PopToRootAsync();
        await WaitForNavigationHostReadyAsync(navigationHost);
        SetActivePage(navigationHost.CurrentPage ?? _activeRoutePage);
        UpdateNavigationStackState();
    }

    public static LuminaShell? FindFor(Control? owner)
    {
        if (owner == null)
        {
            return Current;
        }
        if (owner is LuminaShell shell)
        {
            return shell;
        }
        LuminaShell? ancestorShell = owner.GetVisualAncestors().OfType<LuminaShell>().FirstOrDefault();
        if (ancestorShell != null)
        {
            return ancestorShell;
        }
        return TopLevel.GetTopLevel(owner)?.GetVisualDescendants().OfType<LuminaShell>().FirstOrDefault() ?? Current;
    }

    public static LuminaShell? GetShell(string shellKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shellKey, "shellKey");
        lock (ShellRegistryLock)
        {
            CleanupShellRegistry();
            WeakReference<LuminaShell>? reference;
            LuminaShell? shell;
            return (ShellRegistry.TryGetValue(shellKey, out reference) && reference.TryGetTarget(out shell)) ? shell : null;
        }
    }

    public static IReadOnlyList<LuminaShell> GetOpenShells()
    {
        lock (ShellRegistryLock)
        {
            CleanupShellRegistry();
            return AttachedShells.Select((WeakReference<LuminaShell> reference) => reference.TryGetTarget(out var target) ? target : null).OfType<LuminaShell>().ToArray();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentControl.ContentProperty)
        {
            SetDirectContentPage(change.GetNewValue<object>());
        }
        else if (change.Property == ShellKeyProperty)
        {
            UpdateShellKey(change.GetOldValue<string>(), change.GetNewValue<string>());
        }
        else if (change.Property == ActiveNavigationKeyProperty)
        {
            if (!_isNavigating && !_syncingNavigationKey && AutoNavigate)
            {
                string newKey = change.GetNewValue<string>();
                string oldKey = change.GetOldValue<string>();
                if (string.IsNullOrWhiteSpace(newKey) || !NavigateTo(newKey))
                {
                    SetActiveNavigationKey(oldKey);
                }
            }
            else
            {
                ApplyActivePageMetadata();
            }
        }
        else if (change.Property == AutoApplyPageMetadataProperty)
        {
            SetActivePage(_navigationHost?.CurrentPage ?? _activeRoutePage);
        }
        else if (change.Property == DefaultPageTitleProperty || change.Property == DefaultPageSubtitleProperty || change.Property == DefaultPageActionsProperty)
        {
            ApplyActivePageMetadata();
        }
        else if (change.Property == TitleProperty || change.Property == ActivePageTitleProperty || change.Property == ActivePageSubtitleProperty || change.Property == ActivePageActionsProperty)
        {
            UpdateEffectiveShellChrome();
        }
        else if (change.Property == IsMenuOpenProperty || change.Property == IsShellChromeVisibleProperty || change.Property == IsShellHeaderVisibleProperty || change.Property == IsCompactMenuEnabledProperty || change.Property == CanCompactMenuProperty || change.Property == IsMenuAutoResponsiveProperty || change.Property == PaneDisplayModeProperty || change.Property == HeaderBackButtonVisibilityProperty || change.Property == HeaderPaneToggleButtonVisibilityProperty || change.Property == CollapseHeaderPaneToggleWhenCanGoBackProperty || change.Property == PageContentPaddingProperty || change.Property == HeaderedPageContentPaddingProperty || change.Property == OpenPaneLengthProperty || change.Property == CompactPaneLengthProperty || change.Property == PaneBackgroundProperty)
        {
            UpdateEffectiveShellChrome();
        }
        else if (change.Property == MenuHeaderProperty || change.Property == MenuContentProperty || change.Property == MenuFooterProperty)
        {
            UpdateEffectiveMenuSlots();
            UpdateEffectiveShellChrome();
        }
        else if (change.Property == SafeAreaModeProperty || change.Property == UseSafeAreaForOverlaysProperty)
        {
            UpdateEffectiveShellChrome();
        }
        else if (change.Property == IsWindowGlassEnabledProperty)
        {
            if (change.GetNewValue<bool>())
            {
                if (!Classes.Contains("WindowGlass"))
                {
                    Classes.Add("WindowGlass");
                }
            }
            else
            {
                Classes.Remove("WindowGlass");
            }
        }
        else if (change.Property == ToastContentProperty)
        {
            SyncOverlayHostProperty(change.Property);
        }
        else if (change.Property == IsDialogOpenProperty)
        {
            SyncOverlayHostProperty(change.Property);
        }
        else if (change.Property == IsBottomSheetOpenProperty)
        {
            SyncOverlayHostProperty(change.Property);
        }
        else if (change.Property == IsDrawerOpenProperty)
        {
            SyncOverlayHostProperty(change.Property);
        }
        else if (change.Property == BottomSheetContentProperty)
        {
            SyncOverlayHostProperty(change.Property);
        }
        else if (change.Property == DrawerContentProperty)
        {
            SyncOverlayHostProperty(change.Property);
        }
        else if (change.Property == DialogContentProperty || change.Property == ToastDurationProperty)
        {
            SyncOverlayHostProperty(change.Property);
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        bool isSmallScreen = e.NewSize.Width < SmallScreenBreakpoint;
        bool wasSmallScreen = e.PreviousSize.Width < SmallScreenBreakpoint && e.PreviousSize.Width > 0.0;
        PseudoClasses.Set(":small-screen", isSmallScreen);
        if (IsMenuAutoResponsive && (e.PreviousSize.Width <= 0.0 || wasSmallScreen != isSmallScreen))
        {
            ApplyAutoResponsiveMenuState(isSmallScreen, wasSmallScreen, e.PreviousSize.Width > 0.0);
        }
        UpdateEffectiveShellChrome();
    }

    private void ApplyAutoResponsiveMenuState(bool isSmallScreen, bool wasSmallScreen, bool hasPreviousSize)
    {
        if (isSmallScreen)
        {
            if (hasPreviousSize && !wasSmallScreen)
            {
                _hasWideScreenMenuStateBeforeSmallScreen = true;
                _wideScreenMenuWasOpen = IsMenuOpen;
                _wideScreenCompactMenuWasEnabled = IsCompactMenuEnabled;
            }

            IsMenuOpen = false;
            return;
        }

        if (wasSmallScreen)
        {
            RestoreWideScreenMenuState();
            return;
        }

        IsMenuOpen = ShouldKeepMenuOpenOutsideSmallScreen();
    }

    private void RestoreWideScreenMenuState()
    {
        if (!_hasWideScreenMenuStateBeforeSmallScreen)
        {
            IsMenuOpen = ShouldKeepMenuOpenOutsideSmallScreen();
            return;
        }

        IsCompactMenuEnabled = _wideScreenCompactMenuWasEnabled;
        IsMenuOpen = _wideScreenMenuWasOpen;
        _hasWideScreenMenuStateBeforeSmallScreen = false;
    }

    private void UpdateEffectiveShellChrome()
    {
        LuminaShellPushOptions? activePushOptions = GetPushOptions(ActiveRouteContent as Page);
        bool showShellChrome = activePushOptions?.ShowShellChrome ?? (_activePage?.ShowShellChrome ?? true);
        bool showShellHeader = activePushOptions?.ShowShellHeader ?? (_activePage?.ShowShellHeader ?? true);
        bool showShellMenu = activePushOptions?.ShowShellMenu ?? true;
        bool isShellChromeEffectiveVisible = IsShellChromeVisible && showShellChrome;
        bool isShellMenuAllowed = isShellChromeEffectiveVisible && showShellMenu;
        LuminaShellPaneDisplayMode paneDisplayMode = isShellMenuAllowed ? ResolveEffectivePaneDisplayMode() : LuminaShellPaneDisplayMode.Left;
        bool isSmallScreen = Bounds.Width < SmallScreenBreakpoint;
        bool isLeftCompact = paneDisplayMode == LuminaShellPaneDisplayMode.LeftCompact;
        bool isMenuCompact = isShellMenuAllowed && !isSmallScreen && isLeftCompact && !IsMenuOpen;
        bool hasMenu = HasHeaderValue(MenuHeader) || HasHeaderValue(MenuContent) || HasHeaderValue(MenuFooter);
        bool isPaneToggleVisible = isShellMenuAllowed && hasMenu;
        bool isShellHeaderAllowed = isShellChromeEffectiveVisible && IsShellHeaderVisible && showShellHeader;
        object? effectiveHeaderTitle = NormalizeHeaderValue(Title) ?? NormalizeHeaderValue(ActivePageTitle);
        bool hasHeaderContent = HasHeaderValue(effectiveHeaderTitle) || HasHeaderValue(ActivePageSubtitle) || HasHeaderValue(ActivePageActions);
        bool canGoBack = CanGoBack || _navigationHost?.CanGoBack == true;
        bool isHeaderBackButtonVisible = ResolveHeaderBackButtonVisible(isShellHeaderAllowed, canGoBack);
        bool isHeaderPaneToggleButtonVisible = ResolveHeaderPaneToggleButtonVisible(isShellHeaderAllowed, isPaneToggleVisible, canGoBack);
        bool isShellHeaderEffectiveVisible = isShellHeaderAllowed && (hasHeaderContent || isHeaderBackButtonVisible || isHeaderPaneToggleButtonVisible);
        LuminaOverlayHost? menuDrawerHost = isShellMenuAllowed && isSmallScreen ? FindMenuOverlayHost() : null;
        bool useMenuDrawer = menuDrawerHost != null;
        SetMenuDrawerMode(useMenuDrawer);
        bool isMenuEffectiveOpen = isShellMenuAllowed && !useMenuDrawer && IsMenuOpen;
        EffectiveIsShellChromeVisible = isShellChromeEffectiveVisible;
        EffectiveIsShellHeaderVisible = isShellHeaderEffectiveVisible;
        EffectiveIsMenuOpen = isMenuEffectiveOpen;
        EffectiveIsMenuCompact = isMenuCompact;
        EffectivePaneDisplayMode = paneDisplayMode;
        EffectiveIsPaneToggleVisible = isPaneToggleVisible && isShellHeaderEffectiveVisible;
        EffectiveIsHeaderBackButtonVisible = isHeaderBackButtonVisible && isShellHeaderEffectiveVisible;
        EffectiveIsHeaderMenuToggleVisible = isHeaderPaneToggleButtonVisible && isShellHeaderEffectiveVisible;
        UpdateEffectiveHeaderLeadingButtons();
        EffectiveHeaderTitle = effectiveHeaderTitle;
        EffectivePageContentPadding = ResolveEffectivePageContentPadding(isShellChromeEffectiveVisible, isShellHeaderEffectiveVisible);
        EffectiveOpenPaneLength = isShellMenuAllowed ? OpenPaneLength : 0.0;
        EffectiveCompactPaneLength = isShellMenuAllowed ? CompactPaneLength : 0.0;
        PseudoClasses.Set(":chromeless", !isShellChromeEffectiveVisible);
        PseudoClasses.Set(":headerless", !isShellHeaderEffectiveVisible);
        PseudoClasses.Set(":menucompact", isMenuCompact);
        PseudoClasses.Set(":pane-left", paneDisplayMode == LuminaShellPaneDisplayMode.Left);
        PseudoClasses.Set(":pane-left-compact", isLeftCompact);
        UpdateLayoutSafeAreaPartitions();
        SyncMenuDrawer(menuDrawerHost);
    }

    private void UpdateLayoutSafeAreaPartitions()
    {
        Thickness safeAreaPadding = LayoutSafeAreaPadding;
        bool menuConsumesLeftInset = EffectiveIsShellChromeVisible && (EffectiveIsMenuOpen || EffectiveIsMenuCompact);
        MenuLayoutSafeAreaPadding = menuConsumesLeftInset ? new Thickness(safeAreaPadding.Left, safeAreaPadding.Top, 0.0, safeAreaPadding.Bottom) : default;
        ContentLayoutSafeAreaPadding = menuConsumesLeftInset ? new Thickness(0.0, safeAreaPadding.Top, safeAreaPadding.Right, safeAreaPadding.Bottom) : safeAreaPadding;
    }

    private Thickness ResolveEffectivePageContentPadding(bool isShellChromeEffectiveVisible, bool isShellHeaderEffectiveVisible)
    {
        if (!isShellChromeEffectiveVisible)
        {
            return default;
        }

        return isShellHeaderEffectiveVisible && HeaderedPageContentPadding is { } headeredPadding
            ? headeredPadding
            : PageContentPadding;
    }

    private bool ResolveHeaderBackButtonVisible(bool isShellHeaderAllowed, bool canGoBack)
    {
        if (!isShellHeaderAllowed || HeaderBackButtonVisibility == LuminaShellHeaderButtonVisibility.Collapsed)
        {
            return false;
        }

        return HeaderBackButtonVisibility == LuminaShellHeaderButtonVisibility.Visible || canGoBack;
    }

    private bool ResolveHeaderPaneToggleButtonVisible(bool isShellHeaderAllowed, bool isPaneToggleVisible, bool canGoBack)
    {
        if (!isShellHeaderAllowed || !isPaneToggleVisible || HeaderPaneToggleButtonVisibility == LuminaShellHeaderButtonVisibility.Collapsed)
        {
            return false;
        }

        if (HeaderPaneToggleButtonVisibility == LuminaShellHeaderButtonVisibility.Visible)
        {
            return true;
        }

        if (CollapseHeaderPaneToggleWhenCanGoBack && canGoBack)
        {
            return false;
        }

        return true;
    }

    private LuminaShellPaneDisplayMode ResolveEffectivePaneDisplayMode()
    {
        LuminaShellPaneDisplayMode requestedMode = PaneDisplayMode;
        if (requestedMode != LuminaShellPaneDisplayMode.Auto)
        {
            return CoercePaneDisplayMode(requestedMode);
        }

        if (!IsMenuAutoResponsive)
        {
            return CanCompactMenu && IsCompactMenuEnabled ? LuminaShellPaneDisplayMode.LeftCompact : LuminaShellPaneDisplayMode.Left;
        }

        return CanCompactMenu && IsCompactMenuEnabled ? LuminaShellPaneDisplayMode.LeftCompact : LuminaShellPaneDisplayMode.Left;
    }

    private LuminaShellPaneDisplayMode CoercePaneDisplayMode(LuminaShellPaneDisplayMode paneDisplayMode)
    {
        return paneDisplayMode == LuminaShellPaneDisplayMode.LeftCompact && !CanCompactMenu ? LuminaShellPaneDisplayMode.Left : paneDisplayMode;
    }

    private bool ShouldKeepMenuOpenOutsideSmallScreen()
    {
        return ResolveEffectivePaneDisplayMode() == LuminaShellPaneDisplayMode.Left;
    }

    private static object? NormalizeHeaderValue(object? value)
    {
        return HasHeaderValue(value) ? value : null;
    }

    private static bool HasHeaderValue(object? value)
    {
        return value != null && (value is not string text || !string.IsNullOrWhiteSpace(text));
    }

    private LuminaOverlayHost? FindMenuOverlayHost()
    {
        return this.GetVisualAncestors().OfType<LuminaOverlayHost>().OrderBy(GetVisualDepth).FirstOrDefault() ?? _overlayHost;
    }

    private static int GetVisualDepth(Control control)
    {
        return control.GetVisualAncestors().Count();
    }

    private void UpdateEffectiveMenuSlots()
    {
        EffectiveMenuHeader = _isMenuDrawerMode ? null : MenuHeader;
        EffectiveMenuContent = _isMenuDrawerMode ? null : MenuContent;
        EffectiveMenuFooter = _isMenuDrawerMode ? null : MenuFooter;
    }

    private static void DisablePageAutoSafeArea(Page? page)
    {
        if (page is ContentPage contentPage)
        {
            contentPage.AutomaticallyApplySafeAreaPadding = false;
        }
    }

    private static void ConfigureShellHostedPage(Page page)
    {
        DisablePageAutoSafeArea(page);
        NavigationPage.SetHasNavigationBar(page, false);
        NavigationPage.SetHasBackButton(page, false);
    }

    private void StorePushOptions(Page page, LuminaShellPushOptions? options)
    {
        _pushOptions.Remove(page);
        if (options != null)
        {
            _pushOptions.Add(page, options);
        }
    }

    private void ClearPushOptions(Page page)
    {
        _pushOptions.Remove(page);
    }

    private LuminaShellPushOptions? GetPushOptions(Page? page)
    {
        return page != null && _pushOptions.TryGetValue(page, out LuminaShellPushOptions? options) ? options : null;
    }

    private void SetDirectContentPage(object? content)
    {
        Page? page = CreateDirectContentPage(content);
        if (page != null)
        {
            ClearPushOptions(page);
        }
        _activeRoutePage = page;
        SyncNavigationHostContent();
    }

    private void SetRoutePage(Page page)
    {
        ClearPushOptions(page);
        _activeRoutePage = page;
        SyncNavigationHostContent();
    }

    private void SyncNavigationHostContent()
    {
        ObserveNavigationHost(_navigationHost);

        if (_navigationHost == null)
        {
            SetActivePage(_activeRoutePage);
            UpdateNavigationStackState();
            return;
        }

        if (_activeRoutePage == null)
        {
            _navigationHost.Content = null;
            SetActivePage(null);
            UpdateNavigationStackState();
            return;
        }

        ResetNavigationHostToRoutePage(_navigationHost, _activeRoutePage);
    }

    private static Page? CreateDirectContentPage(object? content)
    {
        if (content == null)
        {
            return null;
        }

        Page page = content as Page ?? new ContentPage { Content = content };
        ConfigureShellHostedPage(page);
        return page;
    }

    private static Page CreateRoutePage(string navigationKey, Control content)
    {
        Page page = content as Page ?? new ContentPage { Content = content };
        ApplyNavigationKey(page, navigationKey);
        ConfigureShellHostedPage(page);
        return page;
    }

    private void ResetNavigationHostToRoutePage(NavigationPage navigationHost, Page routePage)
    {
        int version = ++_navigationHostResetVersion;
        SetActivePage(routePage);
        SetNavigationStackState(false, 1);
        _ = ResetNavigationHostToRoutePageAsync(navigationHost, routePage, version);
    }

    private async Task ResetNavigationHostToRoutePageAsync(NavigationPage navigationHost, Page routePage, int version)
    {
        _isResettingNavigationHost = true;
        try
        {
            await WaitForNavigationHostIdleAsync(navigationHost, version);
            if (!IsCurrentNavigationHostReset(navigationHost, version))
            {
                return;
            }

            if (ReferenceEquals(navigationHost.CurrentPage, routePage))
            {
                SetActivePage(routePage);
                UpdateNavigationStackState();
                return;
            }

            if (navigationHost.StackDepth == 0 || navigationHost.Content == null)
            {
                navigationHost.Content = routePage;
                SetActivePage(routePage);
                UpdateNavigationStackState();
                return;
            }

            if (navigationHost.StackDepth > 1)
            {
                await navigationHost.PopToRootAsync();
                await WaitForNavigationHostIdleAsync(navigationHost, version);
                if (!IsCurrentNavigationHostReset(navigationHost, version))
                {
                    return;
                }
            }

            if (!ReferenceEquals(navigationHost.CurrentPage, routePage))
            {
                await navigationHost.ReplaceAsync(routePage);
                await WaitForNavigationHostIdleAsync(navigationHost, version);
            }

            if (IsCurrentNavigationHostReset(navigationHost, version))
            {
                SetActivePage(navigationHost.CurrentPage ?? routePage);
                UpdateNavigationStackState();
            }
        }
        catch
        {
            if (IsCurrentNavigationHostReset(navigationHost, version))
            {
                navigationHost.Content = routePage;
                SetActivePage(routePage);
                UpdateNavigationStackState();
            }
        }
        finally
        {
            if (version == _navigationHostResetVersion)
            {
                _isResettingNavigationHost = false;
                CompleteNavigationIdleWaiters();
                UpdateNavigationStackState();
            }
        }
    }

    private async Task WaitForNavigationHostIdleAsync(NavigationPage navigationHost, int version)
    {
        // 事件驱动等待：导航进行中时挂起，待 IsNavigating 变为 false 由属性回调唤醒。
        // 保留一个超时兜底，避免任何竞态下永久挂起（正常路径不会触发超时）。
        while (IsCurrentNavigationHostReset(navigationHost, version) && navigationHost.IsNavigating)
        {
            if (!await WaitForNavigationSignalAsync())
            {
                return;
            }
        }
    }

    private async Task<bool> WaitForNavigationHostReadyAsync(NavigationPage navigationHost)
    {
        while (ReferenceEquals(navigationHost, _navigationHost) && (_isResettingNavigationHost || navigationHost.IsNavigating))
        {
            if (!await WaitForNavigationSignalAsync())
            {
                break;
            }
        }

        return ReferenceEquals(navigationHost, _navigationHost);
    }

    private async Task<bool> WaitForNavigationSignalAsync()
    {
        TaskCompletionSource<bool> completion = _navigationIdleCompletion ??=
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // 超时兜底：万一唤醒信号因竞态丢失，最长 2 秒后重新检查循环条件，避免死等。
        Task delay = Task.Delay(2000);
        Task finished = await Task.WhenAny(completion.Task, delay).ConfigureAwait(true);
        if (finished == delay && ReferenceEquals(_navigationIdleCompletion, completion))
        {
            // 超时：丢弃旧的等待源，让调用方按最新状态重新评估。
            _navigationIdleCompletion = null;
            completion.TrySetResult(false);
        }

        return true;
    }

    private bool IsCurrentNavigationHostReset(NavigationPage navigationHost, int version)
    {
        return version == _navigationHostResetVersion && ReferenceEquals(navigationHost, _navigationHost);
    }

    private NavigationPage GetNavigationHostForStackOperation()
    {
        return _navigationHost ?? throw new InvalidOperationException("LuminaShell navigation host is not available.");
    }

    private bool CanNavigateBack()
    {
        return _navigationHost is { CanGoBack: true, IsNavigating: false };
    }

    private void UpdateNavigationStackState()
    {
        NavigationPage? navigationHost = _navigationHost;
        SetNavigationStackState(navigationHost?.CanGoBack == true, navigationHost?.StackDepth ?? 0);
    }

    private void SetNavigationStackState(bool canGoBack, int stackDepth)
    {
        bool oldCanGoBack = CanGoBack;
        CanGoBack = canGoBack;
        NavigationStackDepth = stackDepth;
        (NavigateBackCommand as LuminaRelayCommand)?.RaiseCanExecuteChanged();
        UpdateEffectiveHeaderBackButtonVisible();
        UpdateEffectiveHeaderMenuToggleVisible();
        if (oldCanGoBack != canGoBack)
        {
            UpdateEffectiveShellChrome();
        }
    }

    private void UpdateEffectiveHeaderBackButtonVisible()
    {
        EffectiveIsHeaderBackButtonVisible = ResolveHeaderBackButtonVisible(EffectiveIsShellHeaderVisible, CanGoBack);
        UpdateEffectiveHeaderLeadingButtons();
    }

    private void UpdateEffectiveHeaderMenuToggleVisible()
    {
        EffectiveIsHeaderMenuToggleVisible = ResolveHeaderPaneToggleButtonVisible(EffectiveIsShellHeaderVisible, EffectiveIsPaneToggleVisible, CanGoBack);
        UpdateEffectiveHeaderLeadingButtons();
    }

    private void UpdateEffectiveHeaderLeadingButtons()
    {
        EffectiveHasHeaderLeadingButtons = EffectiveIsHeaderBackButtonVisible || EffectiveIsHeaderMenuToggleVisible;
        EffectiveIsHeaderLeadingButtonSeparatorVisible = EffectiveIsHeaderBackButtonVisible && EffectiveIsHeaderMenuToggleVisible;
    }

    private void SetActivePage(Page? content)
    {
        ActiveRouteContent = content;
        if (_activePage != null)
        {
            _activePage.PropertyChanged -= OnActivePagePropertyChanged;
            _activePage = null;
        }
        if (!AutoApplyPageMetadata)
        {
            ActivePage = null;
            ApplyDefaultPageMetadata();
            return;
        }
        if (content is not LuminaPage page)
        {
            ActivePage = null;
            ApplyDefaultPageMetadata();
            return;
        }
        _activePage = page;
        ActivePage = page;
        _activePage.PropertyChanged += OnActivePagePropertyChanged;
        ApplyActivePageMetadata();
    }

    private void OnActivePagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == LuminaPage.ShellTitleProperty || e.Property == LuminaPage.ShellSubtitleProperty || e.Property == LuminaPage.ShellActionsProperty || e.Property == LuminaPage.ShowShellChromeProperty || e.Property == LuminaPage.ShowShellHeaderProperty || e.Property == LuminaPage.NavigationKeyProperty)
        {
            ApplyActivePageMetadata();
        }
    }

    private void ApplyActivePageMetadata()
    {
        if (_activePage == null)
        {
            ApplyDefaultPageMetadata();
            return;
        }
        ActivePageTitle = _activePage.ShellTitle ?? _activePage.Header ?? ResolveNavigationItemHeader(ActiveNavigationKey) ?? DefaultPageTitle;
        ActivePageSubtitle = _activePage.ShellSubtitle ?? DefaultPageSubtitle;
        ActivePageActions = _activePage.ShellActions ?? DefaultPageActions;
        UpdateEffectiveShellChrome();
        if (!string.IsNullOrWhiteSpace(_activePage.NavigationKey))
        {
            SetActiveNavigationKey(_activePage.NavigationKey);
        }
        if (EffectiveIsShellChromeVisible && _activePage.CloseShellMenuOnNavigate && ShouldCloseMenuOnNavigate())
        {
            IsMenuOpen = false;
        }
    }

    private void ApplyDefaultPageMetadata()
    {
        ActivePageTitle = ResolveNavigationItemHeader(ActiveNavigationKey) ?? DefaultPageTitle;
        ActivePageSubtitle = DefaultPageSubtitle;
        ActivePageActions = DefaultPageActions;
        UpdateEffectiveShellChrome();
    }

    private bool ShouldCloseMenuOnNavigate()
    {
        return Bounds.Width < SmallScreenBreakpoint;
    }

    private bool ShouldCloseMenuOnSystemBack()
    {
        return EffectiveIsShellChromeVisible && IsMenuOpen && ShouldCloseMenuOnNavigate();
    }

    private object? ResolveNavigationItemHeader(string? navigationKey)
    {
        if (string.IsNullOrWhiteSpace(navigationKey))
        {
            return null;
        }

        if (TryFindNavigationItem(MenuContent, navigationKey, out LuminaNavigationItem? item) && item != null)
        {
            return NormalizeHeaderValue(item.Header);
        }

        return null;
    }

    private static bool TryFindNavigationItem(object? source, string navigationKey, out LuminaNavigationItem? item)
    {
        item = null;
        if (source is LuminaNavigationItem navigationItem)
        {
            if (string.Equals(GetNavigationItemKey(navigationItem), navigationKey, StringComparison.Ordinal))
            {
                item = navigationItem;
                return true;
            }

            return TryFindNavigationItemInItems(navigationItem, navigationKey, out item);
        }

        if (source is ItemsControl itemsControl && TryFindNavigationItemInItems(itemsControl, navigationKey, out item))
        {
            return true;
        }

        if (source is Control control)
        {
            foreach (LuminaNavigationItem descendant in control.GetLogicalDescendants().OfType<LuminaNavigationItem>())
            {
                if (string.Equals(GetNavigationItemKey(descendant), navigationKey, StringComparison.Ordinal))
                {
                    item = descendant;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindNavigationItemInItems(ItemsControl owner, string navigationKey, out LuminaNavigationItem? item)
    {
        foreach (object? candidate in EnumerateNavigationSources(owner))
        {
            if (TryFindNavigationItem(candidate, navigationKey, out item))
            {
                return true;
            }
        }

        item = null;
        return false;
    }

    private static IEnumerable<object?> EnumerateNavigationSources(ItemsControl owner)
    {
        foreach (object? item in EnumerateItems(owner.Items))
        {
            yield return item;
        }

        if (owner.ItemsSource == null)
        {
            yield break;
        }

        foreach (object? item in EnumerateItems(owner.ItemsSource))
        {
            yield return item;
        }
    }

    private static IEnumerable<object?> EnumerateItems(IEnumerable source)
    {
        foreach (object? item in source)
        {
            yield return item;
        }
    }

    private static string? GetNavigationItemKey(LuminaNavigationItem item)
    {
        return string.IsNullOrWhiteSpace(item.NavigationKey) ? item.Name : item.NavigationKey;
    }

    private Page GetRoutePage(string navigationKey, Func<Control> factory)
    {
        if (CachePages && _routePageCache.TryGetValue(navigationKey, out Page? cachedPage))
        {
            return cachedPage;
        }

        if (!CachePages || !_routeCache.TryGetValue(navigationKey, out Control? content))
        {
            content = factory() ?? throw new InvalidOperationException("Route '" + navigationKey + "' returned null content.");
        }

        Page page = CreateRoutePage(navigationKey, content);
        if (CachePages)
        {
            _routeCache[navigationKey] = content;
            _routePageCache[navigationKey] = page;
        }

        return page;
    }

    private static void ApplyNavigationKey(Page content, string navigationKey)
    {
        if (content is LuminaPage { NavigationKey: null or "" } page)
        {
            page.NavigationKey = navigationKey;
        }
    }

    private void SetActiveNavigationKey(string? navigationKey)
    {
        _syncingNavigationKey = true;
        try
        {
            ActiveNavigationKey = navigationKey;
        }
        finally
        {
            _syncingNavigationKey = false;
        }
    }

    private void RegisterAttachedShell()
    {
        lock (ShellRegistryLock)
        {
            CleanupShellRegistry();
            AttachedShells.Add(new WeakReference<LuminaShell>(this));
            RegisterShellKey(ShellKey, this);
            Current = this;
        }
    }

    private void UnregisterAttachedShell()
    {
        lock (ShellRegistryLock)
        {
            AttachedShells.RemoveAll(IsThisShellReference);
            UnregisterShellKey(ShellKey, this);
            if (Current == this)
            {
                Current = AttachedShells.Select((WeakReference<LuminaShell> reference) => reference.TryGetTarget(out var target) ? target : null).OfType<LuminaShell>().LastOrDefault();
            }
        }
    }

    private void UpdateShellKey(string? oldKey, string? newKey)
    {
        lock (ShellRegistryLock)
        {
            UnregisterShellKey(oldKey, this);
            RegisterShellKey(newKey, this);
        }
    }

    private static void RegisterShellKey(string? shellKey, LuminaShell shell)
    {
        if (!string.IsNullOrWhiteSpace(shellKey))
        {
            ShellRegistry[shellKey] = new WeakReference<LuminaShell>(shell);
        }
    }

    private static void UnregisterShellKey(string? shellKey, LuminaShell shell)
    {
        if (!string.IsNullOrWhiteSpace(shellKey) && ShellRegistry.TryGetValue(shellKey, out WeakReference<LuminaShell>? reference) && reference.TryGetTarget(out var registeredShell) && registeredShell == shell)
        {
            ShellRegistry.Remove(shellKey);
        }
    }

    private static void CleanupShellRegistry()
    {
        AttachedShells.RemoveAll((WeakReference<LuminaShell> reference) => !reference.TryGetTarget(out LuminaShell? _));
        foreach (string key in ShellRegistry.Keys.ToArray())
        {
            if (!ShellRegistry[key].TryGetTarget(out LuminaShell? _))
            {
                ShellRegistry.Remove(key);
            }
        }
    }

    private bool IsThisShellReference(WeakReference<LuminaShell> reference)
    {
        LuminaShell? shell;
        return reference.TryGetTarget(out shell) && shell == this;
    }
}
