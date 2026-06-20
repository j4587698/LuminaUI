using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
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

    private const string WindowGlassClass = "WindowGlass";

    private static readonly TimeSpan BottomSheetClearDelay = TimeSpan.FromMilliseconds(360);

    private static readonly TimeSpan DrawerClearDelay = TimeSpan.FromMilliseconds(360);

    private readonly Dictionary<string, Func<Control>> _routeFactories = new Dictionary<string, Func<Control>>(StringComparer.Ordinal);

    private readonly Dictionary<string, Control> _routeCache = new Dictionary<string, Control>(StringComparer.Ordinal);

    private readonly LuminaOverlayInputPaneAvoidance _overlayInputPaneAvoidance;

    private LuminaPage? _activePage;

    private ContentPresenter? _toastPresenter;

    private Control? _dialogOverlay;

    private Control? _bottomSheetOverlay;

    private Control? _drawerOverlay;

    private object? _toastContent;

    private CancellationTokenSource? _toastHideCancellation;

    private CancellationTokenSource? _bottomSheetClearCancellation;

    private CancellationTokenSource? _drawerClearCancellation;

    private bool _ownsBottomSheetContent;

    private bool _ownsDrawerContent;

    private bool _settingOwnedBottomSheetContent;

    private bool _settingOwnedDrawerContent;

    private LuminaTopView? _topMenuDrawerHost;

    private LuminaDrawer? _topMenuDrawer;

    private ContentPresenter? _topMenuDrawerHeaderPresenter;

    private ContentPresenter? _topMenuDrawerContentPresenter;

    private ContentPresenter? _topMenuDrawerFooterPresenter;

    private IDisposable? _topMenuDrawerHeaderBinding;

    private IDisposable? _topMenuDrawerContentBinding;

    private IDisposable? _topMenuDrawerFooterBinding;

    private TimeSpan? _pendingToastDuration;

    private bool _isNavigating;

    private bool _syncingNavigationKey;

    private bool _syncingTopMenuDrawer;

    private bool _isTopMenuDrawerMode;

    private bool _hasWideScreenMenuStateBeforeSmallScreen;

    private bool _wideScreenMenuWasOpen;

    private bool _wideScreenCompactMenuWasEnabled;

    private bool _effectiveIsMenuOpen = true;

    private bool _effectiveIsShellChromeVisible = true;

    private bool _effectiveIsShellHeaderVisible = true;

    private bool _effectiveIsPaneToggleVisible;

    private bool _effectiveIsMenuCompact;

    private LuminaShellPaneDisplayMode _effectivePaneDisplayMode = LuminaShellPaneDisplayMode.Left;

    private object? _effectiveHeaderTitle;

    private double _effectiveOpenPaneLength = 220.0;

    private double _effectiveCompactPaneLength = 48.0;

    private object? _effectiveMenuHeader;

    private object? _effectiveMenuContent;

    private object? _effectiveMenuFooter;

    private Thickness _layoutSafeAreaPadding;

    private Thickness _overlaySafeAreaPadding;

    private Thickness _effectivePageContentPadding;

    public static readonly StyledProperty<bool> IsMenuOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsMenuOpen), defaultValue: true);

    public static readonly StyledProperty<bool> IsShellChromeVisibleProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsShellChromeVisible), defaultValue: true);

    public static readonly StyledProperty<bool> IsShellHeaderVisibleProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsShellHeaderVisible), defaultValue: true);

    public static readonly StyledProperty<bool> IsCompactMenuEnabledProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsCompactMenuEnabled), defaultValue: false);

    public static readonly StyledProperty<bool> CanCompactMenuProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(CanCompactMenu), defaultValue: true);

    public static readonly StyledProperty<bool> IsMenuAutoResponsiveProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsMenuAutoResponsive), defaultValue: true);

    public static readonly StyledProperty<LuminaShellPaneDisplayMode> PaneDisplayModeProperty = AvaloniaProperty.Register<LuminaShell, LuminaShellPaneDisplayMode>(nameof(PaneDisplayMode), LuminaShellPaneDisplayMode.Auto);

    public static readonly StyledProperty<Thickness> PageContentPaddingProperty = AvaloniaProperty.Register<LuminaShell, Thickness>(nameof(PageContentPadding));

    public static readonly StyledProperty<Thickness?> HeaderedPageContentPaddingProperty = AvaloniaProperty.Register<LuminaShell, Thickness?>(nameof(HeaderedPageContentPadding));

    public static readonly AttachedProperty<bool> IsMenuCompactProperty = AvaloniaProperty.RegisterAttached<LuminaShell, Control, bool>("IsMenuCompact", defaultValue: false, inherits: true);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsMenuOpenProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsMenuOpen), (LuminaShell shell) => shell.EffectiveIsMenuOpen, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsShellChromeVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsShellChromeVisible), (LuminaShell shell) => shell.EffectiveIsShellChromeVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsShellHeaderVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsShellHeaderVisible), (LuminaShell shell) => shell.EffectiveIsShellHeaderVisible, null, unsetValue: false);

    public static readonly DirectProperty<LuminaShell, bool> EffectiveIsPaneToggleVisibleProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>(nameof(EffectiveIsPaneToggleVisible), (LuminaShell shell) => shell.EffectiveIsPaneToggleVisible, null, unsetValue: false);

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

    public static readonly DirectProperty<LuminaShell, Thickness> OverlaySafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaShell, Thickness>(nameof(OverlaySafeAreaPadding), shell => shell.OverlaySafeAreaPadding);

    public static readonly StyledProperty<bool> IsWindowGlassEnabledProperty = AvaloniaProperty.Register<LuminaShell, bool>(nameof(IsWindowGlassEnabled), defaultValue: false);

    public static LuminaShell? Current { get; private set; }

    public ICommand NavigateCommand { get; }

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
            SetAndRaise(EffectiveIsPaneToggleVisibleProperty, ref _effectiveIsPaneToggleVisible, value);
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
            SetAndRaise(LayoutSafeAreaPaddingProperty, ref _layoutSafeAreaPadding, value);
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
        _overlayInputPaneAvoidance.AttachToVisualTree();
        UpdateEffectiveSafeAreaPadding();
        UpdateEffectiveShellChrome();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CloseTopMenuDrawer(forceClearContent: true);
        if (_activePage != null)
        {
            _activePage.PropertyChanged -= OnActivePagePropertyChanged;
            _activePage = null;
        }
        ActivePage = null;
        ActiveRouteContent = null;
        CancelToastHide();
        CancelBottomSheetContentClear();
        CancelDrawerContentClear();
        _overlayInputPaneAvoidance.DetachFromVisualTree();
        if (_dialogOverlay != null)
        {
            _dialogOverlay.RemoveHandler(InputElement.PointerPressedEvent, OnDialogOverlayPointerPressed);
            _dialogOverlay = null;
        }
        if (_bottomSheetOverlay != null)
        {
            _bottomSheetOverlay.RemoveHandler(InputElement.PointerPressedEvent, OnBottomSheetOverlayPointerPressed);
            _bottomSheetOverlay = null;
        }
        if (_drawerOverlay != null)
        {
            _drawerOverlay.RemoveHandler(InputElement.PointerPressedEvent, OnDrawerOverlayPointerPressed);
            _drawerOverlay = null;
        }
        base.OnDetachedFromVisualTree(e);
        UnregisterAttachedShell();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_dialogOverlay != null)
        {
            _dialogOverlay.RemoveHandler(InputElement.PointerPressedEvent, OnDialogOverlayPointerPressed);
        }
        _dialogOverlay = e.NameScope.FindRequired<Control>("PART_DialogOverlay");
        if (_dialogOverlay != null)
        {
            _dialogOverlay.AddHandler(InputElement.PointerPressedEvent, OnDialogOverlayPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        if (_bottomSheetOverlay != null)
        {
            _bottomSheetOverlay.RemoveHandler(InputElement.PointerPressedEvent, OnBottomSheetOverlayPointerPressed);
        }
        _bottomSheetOverlay = e.NameScope.FindRequired<Control>("PART_BottomSheetOverlay");
        if (_bottomSheetOverlay != null)
        {
            _bottomSheetOverlay.AddHandler(InputElement.PointerPressedEvent, OnBottomSheetOverlayPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        if (_drawerOverlay != null)
        {
            _drawerOverlay.RemoveHandler(InputElement.PointerPressedEvent, OnDrawerOverlayPointerPressed);
        }
        _drawerOverlay = e.NameScope.FindRequired<Control>("PART_DrawerOverlay");
        if (_drawerOverlay != null)
        {
            _drawerOverlay.AddHandler(InputElement.PointerPressedEvent, OnDrawerOverlayPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        _toastPresenter = e.NameScope.FindRequired<ContentPresenter>("PART_ToastPresenter");
        if (_toastPresenter != null)
        {
            _toastPresenter.Content = ToastContent;
        }
        _overlayInputPaneAvoidance.ApplyTemplate(
            e.NameScope.FindRequired<Control>("PART_DialogContainer"),
            e.NameScope.FindRequired<Control>("PART_BottomSheetContainer"),
            e.NameScope.FindRequired<Control>("PART_DrawerContainer"));
        ApplyBottomSheetSafeAreaPadding();
        ApplyDrawerSafeAreaPadding();
    }

    private void OnDialogOverlayPointerPressed(object? sender, PointerPressedEventArgs ev)
    {
        if (!IsPointerSourceInsidePart(ev.Source, "PART_DialogRoot"))
        {
            IsDialogOpen = false;
        }
    }

    private void OnBottomSheetOverlayPointerPressed(object? sender, PointerPressedEventArgs ev)
    {
        if (!IsPointerSourceInsidePart(ev.Source, "PART_BottomSheetContainer"))
        {
            IsBottomSheetOpen = false;
        }
    }

    private void OnDrawerOverlayPointerPressed(object? sender, PointerPressedEventArgs ev)
    {
        if (!IsPointerSourceInsidePart(ev.Source, "PART_DrawerContainer"))
        {
            IsDrawerOpen = false;
        }
    }

    private static bool IsPointerSourceInsidePart(object? source, string partName)
    {
        if (source is not Control control)
        {
            return false;
        }

        return control.Name == partName || control.GetVisualAncestors().OfType<Control>().Any(ancestor => ancestor.Name == partName);
    }

    public LuminaShell()
    {
        _overlayInputPaneAvoidance = new LuminaOverlayInputPaneAvoidance(this, () => IsDialogOpen, () => IsBottomSheetOpen, () => IsDrawerOpen);
        NavigateCommand = new LuminaRelayCommand((object? parameter) =>
        {
            if (parameter is string navigationKey)
            {
                NavigateTo(navigationKey);
            }
        }, (object? parameter) => parameter is string value && !string.IsNullOrWhiteSpace(value));
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
        if (content == null)
        {
            ClearToast();
            return;
        }
        _pendingToastDuration = duration;
        if (ToastContent == content)
        {
            _pendingToastDuration = null;
            ScheduleToastHide(content, duration);
        }
        else
        {
            ToastContent = content;
        }
    }

    public void ClearToast()
    {
        _pendingToastDuration = null;
        ToastContent = null;
    }

    public void ShowDialog(object? content)
    {
        DialogContent = content;
        IsDialogOpen = content != null;
    }

    public void CloseDialog()
    {
        IsDialogOpen = false;
    }

    public void ShowBottomSheet(object? content)
    {
        LuminaBottomSheet? bottomSheet = LuminaBottomSheet.EnsureSheet(content);
        if (bottomSheet != null)
        {
            bottomSheet.SafeAreaPadding = OverlaySafeAreaPadding;
        }

        _settingOwnedBottomSheetContent = true;
        try
        {
            _ownsBottomSheetContent = bottomSheet != null;
            BottomSheetContent = bottomSheet;
        }
        finally
        {
            _settingOwnedBottomSheetContent = false;
        }
        IsBottomSheetOpen = bottomSheet != null;
    }

    public void CloseBottomSheet()
    {
        IsBottomSheetOpen = false;
    }

    public void ShowDrawer(object? content)
    {
        LuminaDrawer? drawer = LuminaDrawer.EnsureDrawer(content);
        if (drawer != null)
        {
            drawer.SafeAreaPadding = OverlaySafeAreaPadding;
        }

        _settingOwnedDrawerContent = true;
        try
        {
            _ownsDrawerContent = drawer != null;
            DrawerContent = drawer;
        }
        finally
        {
            _settingOwnedDrawerContent = false;
        }
        IsDrawerOpen = drawer != null;
    }

    public void CloseDrawer()
    {
        IsDrawerOpen = false;
    }

    public void RegisterRoute(string navigationKey, Func<Control> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(navigationKey, "navigationKey");
        ArgumentNullException.ThrowIfNull(factory, "factory");
        _routeFactories[navigationKey] = factory;
        _routeCache.Remove(navigationKey);
    }

    public bool UnregisterRoute(string navigationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(navigationKey, "navigationKey");
        _routeCache.Remove(navigationKey);
        return _routeFactories.Remove(navigationKey);
    }

    public void ClearRoutes()
    {
        _routeFactories.Clear();
        _routeCache.Clear();
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
        Control content = GetRouteContent(navigationKey, factory);
        ApplyNavigationKey(content, navigationKey);
        _isNavigating = true;
        try
        {
            Content = content;
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
            DisablePageAutoSafeArea(change.GetNewValue<object>());
            SetActivePage(change.GetNewValue<object>());
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
            SetActivePage(Content);
        }
        else if (change.Property == DefaultPageTitleProperty || change.Property == DefaultPageSubtitleProperty || change.Property == DefaultPageActionsProperty)
        {
            ApplyActivePageMetadata();
        }
        else if (change.Property == TitleProperty || change.Property == ActivePageTitleProperty || change.Property == ActivePageSubtitleProperty || change.Property == ActivePageActionsProperty)
        {
            UpdateEffectiveShellChrome();
        }
        else if (change.Property == IsMenuOpenProperty || change.Property == IsShellChromeVisibleProperty || change.Property == IsShellHeaderVisibleProperty || change.Property == IsCompactMenuEnabledProperty || change.Property == CanCompactMenuProperty || change.Property == IsMenuAutoResponsiveProperty || change.Property == PaneDisplayModeProperty || change.Property == PageContentPaddingProperty || change.Property == HeaderedPageContentPaddingProperty || change.Property == OpenPaneLengthProperty || change.Property == CompactPaneLengthProperty)
        {
            UpdateEffectiveShellChrome();
        }
        else if (change.Property == MenuHeaderProperty || change.Property == MenuContentProperty || change.Property == MenuFooterProperty)
        {
            UpdateEffectiveMenuSlots();
            UpdateEffectiveShellChrome();
        }
        else if (change.Property == SafeAreaModeProperty || change.Property == UseSafeAreaForOverlaysProperty || change.Property == LuminaInsets.SafeAreaPaddingProperty)
        {
            UpdateEffectiveSafeAreaPadding();
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
            object content = change.GetNewValue<object>();
            if (_toastPresenter != null)
            {
                _toastPresenter.Content = content;
            }
            if (content == null)
            {
                CancelToastHide();
                _pendingToastDuration = null;
            }
            else
            {
                TimeSpan duration = _pendingToastDuration ?? ToastDuration;
                _pendingToastDuration = null;
                ScheduleToastHide(content, duration);
            }
        }
        else if (change.Property == IsDialogOpenProperty)
        {
            _overlayInputPaneAvoidance.UpdateOverlayState();
        }
        else if (change.Property == IsBottomSheetOpenProperty)
        {
            if (change.GetNewValue<bool>())
            {
                CancelBottomSheetContentClear();
            }
            else
            {
                ScheduleBottomSheetContentClear();
            }
            _overlayInputPaneAvoidance.UpdateOverlayState();
        }
        else if (change.Property == IsDrawerOpenProperty)
        {
            if (change.GetNewValue<bool>())
            {
                CancelDrawerContentClear();
            }
            else
            {
                ScheduleDrawerContentClear();
            }
            _overlayInputPaneAvoidance.UpdateOverlayState();
        }
        else if (change.Property == BottomSheetContentProperty)
        {
            if (!_settingOwnedBottomSheetContent)
            {
                _ownsBottomSheetContent = false;
                CancelBottomSheetContentClear();
            }
            ApplyBottomSheetSafeAreaPadding();
        }
        else if (change.Property == DrawerContentProperty)
        {
            if (!_settingOwnedDrawerContent)
            {
                _ownsDrawerContent = false;
                CancelDrawerContentClear();
            }
            ApplyDrawerSafeAreaPadding();
        }
    }

    private void UpdateEffectiveSafeAreaPadding()
    {
        Thickness safeAreaPadding = LuminaInsets.GetSafeAreaPadding(this);
        LayoutSafeAreaPadding = ShouldApplyLayoutSafeArea() ? safeAreaPadding : default;
        // Overlay layers span the whole shell. When an ancestor shell already insets this
        // shell via its own layout safe area, applying it again here would double the padding.
        OverlaySafeAreaPadding = UseSafeAreaForOverlays && !IsSafeAreaProvidedByAncestorShell() ? safeAreaPadding : default;
        ApplyBottomSheetSafeAreaPadding();
        ApplyDrawerSafeAreaPadding();
    }

    private bool ShouldApplyLayoutSafeArea()
    {
        return SafeAreaMode switch
        {
            LuminaSafeAreaMode.Enabled => true,
            LuminaSafeAreaMode.Disabled => false,
            _ => !this.GetVisualAncestors().OfType<LuminaShell>().Any()
        };
    }

    private bool IsSafeAreaProvidedByAncestorShell()
    {
        foreach (LuminaShell ancestor in this.GetVisualAncestors().OfType<LuminaShell>())
        {
            if (ancestor.ShouldApplyLayoutSafeArea())
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyBottomSheetSafeAreaPadding()
    {
        if (BottomSheetContent is LuminaBottomSheet bottomSheet)
        {
            bottomSheet.SafeAreaPadding = OverlaySafeAreaPadding;
        }
    }

    private void ApplyDrawerSafeAreaPadding()
    {
        if (DrawerContent is LuminaDrawer drawer)
        {
            drawer.SafeAreaPadding = OverlaySafeAreaPadding;
        }
    }

    private void ScheduleToastHide(object content, TimeSpan duration)
    {
        CancelToastHide();
        if (duration <= TimeSpan.Zero)
        {
            ToastContent = null;
        }
        else
        {
            _ = HideToastAsync(content, duration, _toastHideCancellation = new CancellationTokenSource());
        }
    }

    private void CancelToastHide()
    {
        CancellationTokenSource? cancellation = _toastHideCancellation;
        _toastHideCancellation = null;
        cancellation?.Cancel();
    }

    private async Task HideToastAsync(object content, TimeSpan duration, CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(duration, cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                if (_toastHideCancellation == cancellation && object.Equals(ToastContent, content))
                {
                    _toastHideCancellation = null;
                    ToastContent = null;
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private void ScheduleBottomSheetContentClear()
    {
        CancelBottomSheetContentClear();
        if (_ownsBottomSheetContent && BottomSheetContent != null)
        {
            _ = ClearBottomSheetContentAsync(_bottomSheetClearCancellation = new CancellationTokenSource());
        }
    }

    private void CancelBottomSheetContentClear()
    {
        CancellationTokenSource? cancellation = _bottomSheetClearCancellation;
        _bottomSheetClearCancellation = null;
        cancellation?.Cancel();
    }

    private void ScheduleDrawerContentClear()
    {
        CancelDrawerContentClear();
        if (_ownsDrawerContent && DrawerContent != null)
        {
            _ = ClearDrawerContentAsync(_drawerClearCancellation = new CancellationTokenSource());
        }
    }

    private void CancelDrawerContentClear()
    {
        CancellationTokenSource? cancellation = _drawerClearCancellation;
        _drawerClearCancellation = null;
        cancellation?.Cancel();
    }

    private async Task ClearBottomSheetContentAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(BottomSheetClearDelay, cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (TaskCanceledException)
        {
            cancellation.Dispose();
            return;
        }
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            if (_bottomSheetClearCancellation == cancellation && !IsBottomSheetOpen && _ownsBottomSheetContent)
            {
                _bottomSheetClearCancellation = null;
                _ownsBottomSheetContent = false;
                BottomSheetContent = null;
            }
            cancellation.Dispose();
        });
    }

    private async Task ClearDrawerContentAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(DrawerClearDelay, cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (TaskCanceledException)
        {
            cancellation.Dispose();
            return;
        }
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            if (_drawerClearCancellation == cancellation && !IsDrawerOpen && _ownsDrawerContent)
            {
                _drawerClearCancellation = null;
                _ownsDrawerContent = false;
                DrawerContent = null;
            }
            cancellation.Dispose();
        });
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _overlayInputPaneAvoidance.UpdateOverlayState();
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
        bool isShellChromeEffectiveVisible = IsShellChromeVisible && (_activePage?.ShowShellChrome ?? true);
        LuminaShellPaneDisplayMode paneDisplayMode = isShellChromeEffectiveVisible ? ResolveEffectivePaneDisplayMode() : LuminaShellPaneDisplayMode.Left;
        bool isSmallScreen = Bounds.Width < SmallScreenBreakpoint;
        bool isLeftCompact = paneDisplayMode == LuminaShellPaneDisplayMode.LeftCompact;
        bool isMenuCompact = isShellChromeEffectiveVisible && !isSmallScreen && isLeftCompact && !IsMenuOpen;
        bool hasMenu = HasHeaderValue(MenuHeader) || HasHeaderValue(MenuContent) || HasHeaderValue(MenuFooter);
        bool isPaneToggleVisible = isShellChromeEffectiveVisible && hasMenu;
        bool isShellHeaderAllowed = isShellChromeEffectiveVisible && IsShellHeaderVisible && (_activePage?.ShowShellHeader ?? true);
        object? effectiveHeaderTitle = NormalizeHeaderValue(Title) ?? NormalizeHeaderValue(ActivePageTitle);
        bool hasHeaderContent = HasHeaderValue(effectiveHeaderTitle) || HasHeaderValue(ActivePageSubtitle) || HasHeaderValue(ActivePageActions);
        bool isShellHeaderEffectiveVisible = isShellHeaderAllowed && (hasHeaderContent || isPaneToggleVisible);
        LuminaTopView? topMenuDrawerHost = isShellChromeEffectiveVisible && isSmallScreen ? FindOuterTopViewHost() : null;
        bool useTopMenuDrawer = topMenuDrawerHost != null;
        SetTopMenuDrawerMode(useTopMenuDrawer);
        bool isMenuEffectiveOpen = isShellChromeEffectiveVisible && !useTopMenuDrawer && IsMenuOpen;
        EffectiveIsShellChromeVisible = isShellChromeEffectiveVisible;
        EffectiveIsShellHeaderVisible = isShellHeaderEffectiveVisible;
        EffectiveIsMenuOpen = isMenuEffectiveOpen;
        EffectiveIsMenuCompact = isMenuCompact;
        EffectivePaneDisplayMode = paneDisplayMode;
        EffectiveIsPaneToggleVisible = isPaneToggleVisible && isShellHeaderEffectiveVisible;
        EffectiveHeaderTitle = effectiveHeaderTitle;
        EffectivePageContentPadding = ResolveEffectivePageContentPadding(isShellChromeEffectiveVisible, isShellHeaderEffectiveVisible);
        EffectiveOpenPaneLength = isShellChromeEffectiveVisible ? OpenPaneLength : 0.0;
        EffectiveCompactPaneLength = isShellChromeEffectiveVisible ? CompactPaneLength : 0.0;
        PseudoClasses.Set(":chromeless", !isShellChromeEffectiveVisible);
        PseudoClasses.Set(":headerless", !isShellHeaderEffectiveVisible);
        PseudoClasses.Set(":menucompact", isMenuCompact);
        PseudoClasses.Set(":pane-left", paneDisplayMode == LuminaShellPaneDisplayMode.Left);
        PseudoClasses.Set(":pane-left-compact", isLeftCompact);
        SyncTopMenuDrawer(topMenuDrawerHost);
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

    private void SetTopMenuDrawerMode(bool value)
    {
        if (_isTopMenuDrawerMode == value)
        {
            UpdateEffectiveMenuSlots();
            return;
        }

        if (!value)
        {
            CloseTopMenuDrawer(forceClearContent: true);
        }

        _isTopMenuDrawerMode = value;
        UpdateEffectiveMenuSlots();
    }

    private void UpdateEffectiveMenuSlots()
    {
        EffectiveMenuHeader = _isTopMenuDrawerMode ? null : MenuHeader;
        EffectiveMenuContent = _isTopMenuDrawerMode ? null : MenuContent;
        EffectiveMenuFooter = _isTopMenuDrawerMode ? null : MenuFooter;
    }

    private void SyncTopMenuDrawer(LuminaTopView? host)
    {
        if (_syncingTopMenuDrawer)
        {
            return;
        }

        if (!_isTopMenuDrawerMode || !EffectiveIsShellChromeVisible || !IsMenuOpen)
        {
            CloseTopMenuDrawer(forceClearContent: false);
            return;
        }

        if (host == null)
        {
            CloseTopMenuDrawer(forceClearContent: true);
            return;
        }

        if (_topMenuDrawerHost != host)
        {
            CloseTopMenuDrawer(forceClearContent: true);
            _topMenuDrawerHost = host;
            _topMenuDrawerHost.PropertyChanged += OnTopMenuDrawerHostPropertyChanged;
        }

        OpenTopMenuDrawer(host);
    }

    private void OpenTopMenuDrawer(LuminaTopView host)
    {
        if (_syncingTopMenuDrawer || !_isTopMenuDrawerMode || !EffectiveIsShellChromeVisible || !IsMenuOpen || !ReferenceEquals(host, _topMenuDrawerHost))
        {
            return;
        }

        if (_topMenuDrawer == null)
        {
            _topMenuDrawer = CreateTopMenuDrawer();
        }

        if (ReferenceEquals(host.DrawerContent, _topMenuDrawer))
        {
            _topMenuDrawer.SafeAreaPadding = host.OverlaySafeAreaPadding;
            if (!host.IsDrawerOpen)
            {
                host.IsDrawerOpen = true;
            }
            return;
        }

        _syncingTopMenuDrawer = true;
        try
        {
            host.ShowDrawer(_topMenuDrawer);
        }
        finally
        {
            _syncingTopMenuDrawer = false;
        }
    }

    private LuminaDrawer CreateTopMenuDrawer()
    {
        LuminaDrawer drawer = new LuminaDrawer
        {
            Placement = DrawerPlacement.Left,
            ContentPadding = default,
            BorderThickness = LuminaPickerResources.Thickness("LuminaShellTopMenuDrawerBorderThickness", new Thickness(0, 0, 1, 0)),
            Content = CreateTopMenuDrawerContent()
        };
        LuminaPickerResources.BindResource(drawer, TemplatedControl.CornerRadiusProperty, "LuminaShellTopMenuDrawerCornerRadius");
        drawer.Bind(TemplatedControl.BackgroundProperty, this.GetObservable(PaneBackgroundProperty));
        drawer.Bind(LuminaDrawer.DrawerLengthProperty, this.GetObservable(OpenPaneLengthProperty));
        return drawer;
    }

    private Control CreateTopMenuDrawerContent()
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
        _topMenuDrawerHeaderPresenter = headerPresenter;
        _topMenuDrawerHeaderBinding = headerPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(MenuHeaderProperty));

        Border header = new Border
        {
            Name = "PART_TopMenuDrawerHeader",
            Margin = LuminaPickerResources.Thickness("LuminaShellTopMenuDrawerHeaderMargin", new Thickness(16, 10, 16, 8)),
            Height = LuminaPickerResources.Double("LuminaShellTopMenuDrawerHeaderHeight", 44),
            ClipToBounds = true,
            Child = headerPresenter
        };

        ContentPresenter menuContentPresenter = new ContentPresenter();
        _topMenuDrawerContentPresenter = menuContentPresenter;
        _topMenuDrawerContentBinding = menuContentPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(MenuContentProperty));

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
        _topMenuDrawerFooterPresenter = footerPresenter;
        _topMenuDrawerFooterBinding = footerPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(MenuFooterProperty));

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

    private void CloseTopMenuDrawer(bool forceClearContent)
    {
        LuminaTopView? host = _topMenuDrawerHost;
        LuminaDrawer? drawer = _topMenuDrawer;
        if (host == null)
        {
            return;
        }

        bool previousSyncingTopMenuDrawer = _syncingTopMenuDrawer;
        if (forceClearContent)
        {
            _syncingTopMenuDrawer = true;
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
            _syncingTopMenuDrawer = previousSyncingTopMenuDrawer;
        }

        if (forceClearContent)
        {
            ReleaseTopMenuDrawerReference();
        }
    }

    private void ReleaseTopMenuDrawerReference()
    {
        if (_topMenuDrawerHost != null)
        {
            _topMenuDrawerHost.PropertyChanged -= OnTopMenuDrawerHostPropertyChanged;
        }
        ClearTopMenuDrawerSlots();
        _topMenuDrawerHost = null;
        _topMenuDrawer = null;
    }

    private void ClearTopMenuDrawerSlots()
    {
        _topMenuDrawerHeaderBinding?.Dispose();
        _topMenuDrawerContentBinding?.Dispose();
        _topMenuDrawerFooterBinding?.Dispose();
        _topMenuDrawerHeaderBinding = null;
        _topMenuDrawerContentBinding = null;
        _topMenuDrawerFooterBinding = null;

        if (_topMenuDrawerHeaderPresenter != null)
        {
            _topMenuDrawerHeaderPresenter.Content = null;
            _topMenuDrawerHeaderPresenter = null;
        }
        if (_topMenuDrawerContentPresenter != null)
        {
            _topMenuDrawerContentPresenter.Content = null;
            _topMenuDrawerContentPresenter = null;
        }
        if (_topMenuDrawerFooterPresenter != null)
        {
            _topMenuDrawerFooterPresenter.Content = null;
            _topMenuDrawerFooterPresenter = null;
        }
        if (_topMenuDrawer != null)
        {
            _topMenuDrawer.Content = null;
        }
    }

    private void OnTopMenuDrawerHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncingTopMenuDrawer || !ReferenceEquals(sender, _topMenuDrawerHost))
        {
            return;
        }

        if (sender is not LuminaTopView host)
        {
            return;
        }

        if (e.Property == LuminaTopView.IsDrawerOpenProperty && !host.IsDrawerOpen && ReferenceEquals(host.DrawerContent, _topMenuDrawer))
        {
            SetMenuOpenFromTopDrawer(false);
        }
        else if (e.Property == LuminaTopView.DrawerContentProperty && host.DrawerContent != null && !ReferenceEquals(host.DrawerContent, _topMenuDrawer))
        {
            bool wasMenuOpen = IsMenuOpen;
            ReleaseTopMenuDrawerReference();
            if (wasMenuOpen)
            {
                SetMenuOpenFromTopDrawer(false);
            }
        }
    }

    private void SetMenuOpenFromTopDrawer(bool value)
    {
        _syncingTopMenuDrawer = true;
        try
        {
            IsMenuOpen = value;
        }
        finally
        {
            _syncingTopMenuDrawer = false;
        }
        UpdateEffectiveShellChrome();
    }

    private LuminaTopView? FindOuterTopViewHost()
    {
        return this.GetVisualAncestors().OfType<LuminaTopView>().OrderBy(GetVisualDepth).FirstOrDefault();
    }

    private static int GetVisualDepth(Control control)
    {
        return control.GetVisualAncestors().Count();
    }

    private void DisablePageAutoSafeArea(object? content)
    {
        if (content is ContentPage page)
        {
            page.AutomaticallyApplySafeAreaPadding = false;
        }
    }

    private void SetActivePage(object? content)
    {
        ActiveRouteContent = content as Control;
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

    private Control GetRouteContent(string navigationKey, Func<Control> factory)
    {
        if (CachePages && _routeCache.TryGetValue(navigationKey, out Control? cachedContent))
        {
            return cachedContent;
        }
        Control content = factory() ?? throw new InvalidOperationException("Route '" + navigationKey + "' returned null content.");
        if (CachePages)
        {
            _routeCache[navigationKey] = content;
        }
        return content;
    }

    private static void ApplyNavigationKey(Control content, string navigationKey)
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
