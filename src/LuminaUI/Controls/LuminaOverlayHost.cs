using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Enums;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaOverlayHost : ContentControl, ILuminaOverlayHost
{
    private static readonly object OverlayHostRegistryLock = new object();

    private static readonly List<WeakReference<LuminaOverlayHost>> AttachedOverlayHosts = new List<WeakReference<LuminaOverlayHost>>();

    private static readonly Dictionary<string, WeakReference<LuminaOverlayHost>> OverlayHostRegistry = new Dictionary<string, WeakReference<LuminaOverlayHost>>(StringComparer.Ordinal);

    private static readonly object SafeAreaOverrideLock = new object();

    private static readonly ConditionalWeakTable<Control, AutoSafeAreaOverrideState> AutoSafeAreaOverrideStates = new ConditionalWeakTable<Control, AutoSafeAreaOverrideState>();

    private static readonly ConditionalWeakTable<IInsetsManager, EdgeToEdgeOverrideState> EdgeToEdgeOverrideStates = new ConditionalWeakTable<IInsetsManager, EdgeToEdgeOverrideState>();

    private static readonly TimeSpan BottomSheetClearDelay = TimeSpan.FromMilliseconds(360);

    private static readonly TimeSpan DrawerClearDelay = TimeSpan.FromMilliseconds(360);

    private readonly LuminaOverlayInputPaneAvoidance _overlayInputPaneAvoidance;

    private Control? _dialogOverlay;

    private Control? _bottomSheetOverlay;

    private Control? _drawerOverlay;

    private LuminaBackdropBlur? _dialogBackdropBlur;

    private LuminaBackdropBlur? _bottomSheetBackdropBlur;

    private LuminaBackdropBlur? _drawerBackdropBlur;

    private ContentPresenter? _toastPresenter;

    private object? _toastContent;

    private CancellationTokenSource? _toastHideCancellation;

    private CancellationTokenSource? _bottomSheetClearCancellation;

    private CancellationTokenSource? _drawerClearCancellation;

    private bool _ownsBottomSheetContent;

    private bool _ownsDrawerContent;

    private bool _settingOwnedBottomSheetContent;

    private bool _settingOwnedDrawerContent;

    private TimeSpan? _pendingToastDuration;

    private TopLevel? _topLevel;

    private IInsetsManager? _insetsManager;

    private IInsetsManager? _edgeToEdgeInsetsManager;

    private bool _hasEdgeToEdgePreferenceOverride;

    private bool _hasTransparentSystemBarColorOverride;

    private Control? _autoSafeAreaTarget;

    private bool _hasAutoSafeAreaOverride;

    private IDisposable? _backRegistration;

    private Thickness _safeAreaPadding;

    private Thickness _layoutSafeAreaPadding;

    private Thickness _overlaySafeAreaPadding;

    public static readonly StyledProperty<string?> OverlayHostKeyProperty = AvaloniaProperty.Register<LuminaOverlayHost, string?>(nameof(OverlayHostKey));

    public static readonly StyledProperty<LuminaGlassMode> GlassModeProperty = AvaloniaProperty.Register<LuminaOverlayHost, LuminaGlassMode>(
        nameof(GlassMode),
        LuminaGlassMode.AcrylicDynamic);

    public static readonly StyledProperty<bool> IsDialogOpenProperty = AvaloniaProperty.Register<LuminaOverlayHost, bool>(nameof(IsDialogOpen), defaultValue: false);

    public static readonly StyledProperty<object?> DialogContentProperty = AvaloniaProperty.Register<LuminaOverlayHost, object?>(nameof(DialogContent));

    public static readonly DirectProperty<LuminaOverlayHost, object?> ToastContentProperty = AvaloniaProperty.RegisterDirect("ToastContent", (LuminaOverlayHost overlayHost) => overlayHost.ToastContent, (LuminaOverlayHost overlayHost, object? value) =>
    {
        overlayHost.ToastContent = value;
    });

    public static readonly StyledProperty<bool> IsBottomSheetOpenProperty = AvaloniaProperty.Register<LuminaOverlayHost, bool>(nameof(IsBottomSheetOpen), defaultValue: false);

    public static readonly StyledProperty<object?> BottomSheetContentProperty = AvaloniaProperty.Register<LuminaOverlayHost, object?>(nameof(BottomSheetContent));

    public static readonly StyledProperty<bool> IsDrawerOpenProperty = AvaloniaProperty.Register<LuminaOverlayHost, bool>(nameof(IsDrawerOpen), defaultValue: false);

    public static readonly StyledProperty<object?> DrawerContentProperty = AvaloniaProperty.Register<LuminaOverlayHost, object?>(nameof(DrawerContent));

    public static readonly StyledProperty<TimeSpan> ToastDurationProperty = AvaloniaProperty.Register<LuminaOverlayHost, TimeSpan>(nameof(ToastDuration), TimeSpan.FromSeconds(3));

    public static readonly StyledProperty<bool> UseSafeAreaProperty = AvaloniaProperty.Register<LuminaOverlayHost, bool>(nameof(UseSafeArea), defaultValue: true);

    public static readonly StyledProperty<LuminaSafeAreaMode> SafeAreaModeProperty = AvaloniaProperty.Register<LuminaOverlayHost, LuminaSafeAreaMode>(nameof(SafeAreaMode), LuminaSafeAreaMode.Auto);

    public static readonly StyledProperty<bool> UseSafeAreaForOverlaysProperty = AvaloniaProperty.Register<LuminaOverlayHost, bool>(nameof(UseSafeAreaForOverlays), defaultValue: true);

    /// <summary>
    /// Gets or sets whether edge-to-edge system bars use a transparent background.
    /// This also disables the Android navigation-bar contrast scrim where supported.
    /// </summary>
    public static readonly StyledProperty<bool> UseTransparentSystemBarsProperty = AvaloniaProperty.Register<LuminaOverlayHost, bool>(nameof(UseTransparentSystemBars), defaultValue: true);

    public static readonly StyledProperty<bool> HandlesSystemBackRequestedProperty = AvaloniaProperty.Register<LuminaOverlayHost, bool>(nameof(HandlesSystemBackRequested), defaultValue: true);

    public static readonly DirectProperty<LuminaOverlayHost, Thickness> SafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaOverlayHost, Thickness>(nameof(SafeAreaPadding), (LuminaOverlayHost overlayHost) => overlayHost.SafeAreaPadding);

    public static readonly DirectProperty<LuminaOverlayHost, Thickness> LayoutSafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaOverlayHost, Thickness>(nameof(LayoutSafeAreaPadding), (LuminaOverlayHost overlayHost) => overlayHost.LayoutSafeAreaPadding);

    public static readonly DirectProperty<LuminaOverlayHost, Thickness> OverlaySafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaOverlayHost, Thickness>(nameof(OverlaySafeAreaPadding), (LuminaOverlayHost overlayHost) => overlayHost.OverlaySafeAreaPadding);

    public ICommand CloseDialogCommand { get; }

    public ICommand CloseBottomSheetCommand { get; }

    public ICommand CloseDrawerCommand { get; }

    public ICommand ClearToastCommand { get; }

    public static LuminaOverlayHost? Current { get; private set; }

    private sealed class AutoSafeAreaOverrideState
    {
        public int Count { get; set; }

        public bool PreviousValue { get; set; }
    }

    private sealed class EdgeToEdgeOverrideState
    {
        public int Count { get; set; }

        public int TransparentSystemBarCount { get; set; }

        public bool PreviousValue { get; set; }

        public Color? PreviousSystemBarColor { get; set; }
    }

    public string? OverlayHostKey
    {
        get => GetValue(OverlayHostKeyProperty);
        set => SetValue(OverlayHostKeyProperty, value);
    }

    public LuminaGlassMode GlassMode
    {
        get => GetValue(GlassModeProperty);
        set => SetValue(GlassModeProperty, value);
    }

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

    public bool UseSafeArea
    {
        get => GetValue(UseSafeAreaProperty);
        set => SetValue(UseSafeAreaProperty, value);
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

    public bool UseTransparentSystemBars
    {
        get => GetValue(UseTransparentSystemBarsProperty);
        set => SetValue(UseTransparentSystemBarsProperty, value);
    }

    public bool HandlesSystemBackRequested
    {
        get => GetValue(HandlesSystemBackRequestedProperty);
        set => SetValue(HandlesSystemBackRequestedProperty, value);
    }

    public Thickness SafeAreaPadding
    {
        get
        {
            return _safeAreaPadding;
        }
        private set
        {
            if (SetAndRaise(SafeAreaPaddingProperty, ref _safeAreaPadding, value))
            {
                UpdateEffectiveSafeAreaPadding();
            }
        }
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

    public LuminaOverlayHost()
    {
        _overlayInputPaneAvoidance = new LuminaOverlayInputPaneAvoidance(this, () => IsDialogOpen, () => IsBottomSheetOpen, () => IsDrawerOpen);
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

    public static LuminaOverlayHost? FindFor(Control? owner)
    {
        if (owner == null)
        {
            return Current;
        }
        List<LuminaOverlayHost> candidates = GetOwnerChain(owner);
        if (candidates.Count > 0)
        {
            return candidates.OrderByDescending(GetVisualDepth).First();
        }
        return TopLevel.GetTopLevel(owner)?.GetVisualDescendants().OfType<LuminaOverlayHost>().OrderByDescending(GetVisualDepth)
            .FirstOrDefault() ?? Current;
    }

    public static LuminaOverlayHost? FindOuterFor(Control? owner)
    {
        if (owner == null)
        {
            return Current;
        }
        List<LuminaOverlayHost> candidates = GetOwnerChain(owner);
        if (candidates.Count > 0)
        {
            return candidates.OrderBy(GetVisualDepth).First();
        }
        return TopLevel.GetTopLevel(owner)?.GetVisualDescendants().OfType<LuminaOverlayHost>().OrderBy(GetVisualDepth)
            .FirstOrDefault() ?? Current;
    }

    public static LuminaOverlayHost? GetOverlayHost(string overlayHostKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayHostKey, "overlayHostKey");
        lock (OverlayHostRegistryLock)
        {
            CleanupOverlayHostRegistry();
            WeakReference<LuminaOverlayHost>? reference;
            LuminaOverlayHost? overlayHost;
            return (OverlayHostRegistry.TryGetValue(overlayHostKey, out reference) && reference.TryGetTarget(out overlayHost)) ? overlayHost : null;
        }
    }

    public static IReadOnlyList<LuminaOverlayHost> GetOpenOverlayHosts()
    {
        lock (OverlayHostRegistryLock)
        {
            CleanupOverlayHostRegistry();
            return AttachedOverlayHosts.Select((WeakReference<LuminaOverlayHost> reference) => reference.TryGetTarget(out var target) ? target : null).OfType<LuminaOverlayHost>().ToArray();
        }
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

    public void RefreshBackdrop()
    {
        _dialogBackdropBlur?.RefreshBackdrop();
        _bottomSheetBackdropBlur?.RefreshBackdrop();
        _drawerBackdropBlur?.RefreshBackdrop();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RegisterAttachedOverlayHost();
        _topLevel = TopLevel.GetTopLevel(this);
        SyncTopLevelSafeAreaMode();
        SyncInsetsManagerSubscription();
        UpdateSafeAreaPadding();
        _overlayInputPaneAvoidance.AttachToVisualTree();

        if (_topLevel != null)
        {
            _topLevel.ScalingChanged += OnTopLevelScalingChanged;
            SyncBackRequestedSubscription();
        }
    }

    private void OnTopLevelScalingChanged(object? sender, EventArgs e)
    {
        UpdateSafeAreaPadding();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelToastHide();
        CancelBottomSheetContentClear();
        CancelDrawerContentClear();
        DetachInsetsManager();
        RestoreTopLevelAutoSafeAreaPadding();
        if (_topLevel != null)
        {
            _topLevel.ScalingChanged -= OnTopLevelScalingChanged;
            DetachBackRequestedSubscription();
        }
        _topLevel = null;
        _overlayInputPaneAvoidance.DetachFromVisualTree();
        DetachOverlayHandlers();
        base.OnDetachedFromVisualTree(e);
        UnregisterAttachedOverlayHost();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DetachOverlayHandlers();
        _dialogOverlay = e.NameScope.FindRequired<Control>("PART_DialogOverlay");
        _dialogBackdropBlur = e.NameScope.FindRequired<LuminaBackdropBlur>("PART_DialogBackdropBlur");
        if (_dialogOverlay != null)
        {
            _dialogOverlay.AddHandler(InputElement.PointerPressedEvent, OnDialogOverlayPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        _bottomSheetOverlay = e.NameScope.FindRequired<Control>("PART_BottomSheetOverlay");
        _bottomSheetBackdropBlur = e.NameScope.FindRequired<LuminaBackdropBlur>("PART_BottomSheetBackdropBlur");
        if (_bottomSheetOverlay != null)
        {
            _bottomSheetOverlay.AddHandler(InputElement.PointerPressedEvent, OnBottomSheetOverlayPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        _drawerOverlay = e.NameScope.FindRequired<Control>("PART_DrawerOverlay");
        _drawerBackdropBlur = e.NameScope.FindRequired<LuminaBackdropBlur>("PART_DrawerBackdropBlur");
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

    internal bool TryHandleSystemBackRequested()
    {
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

        return false;
    }

    private void SyncBackRequestedSubscription()
    {
        DetachBackRequestedSubscription();
        if (_topLevel == null)
        {
            return;
        }

        if (HandlesSystemBackRequested)
        {
            _backRegistration = LuminaBackDispatcher.GetFor(_topLevel).Register(
                this,
                TryHandleSystemBackRequested,
                LuminaBackDispatcher.ModalPriority);
        }
    }

    private void DetachBackRequestedSubscription()
    {
        _backRegistration?.Dispose();
        _backRegistration = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OverlayHostKeyProperty)
        {
            UpdateOverlayHostKey(change.GetOldValue<string>(), change.GetNewValue<string>());
        }
        else if (change.Property == UseSafeAreaProperty || change.Property == SafeAreaModeProperty || change.Property == UseSafeAreaForOverlaysProperty || change.Property == UseTransparentSystemBarsProperty)
        {
            SyncTopLevelSafeAreaMode();
            SyncInsetsManagerSubscription();
            UpdateSafeAreaPadding();
            UpdateEffectiveSafeAreaPadding();
        }
        else if (change.Property == HandlesSystemBackRequestedProperty)
        {
            SyncBackRequestedSubscription();
        }
        else if (change.Property == LuminaInsets.SafeAreaPaddingProperty)
        {
            UpdateEffectiveSafeAreaPadding();
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
            if (change.GetNewValue<bool>())
            {
                _dialogBackdropBlur?.RefreshBackdrop();
            }
            _overlayInputPaneAvoidance.UpdateOverlayState();
        }
        else if (change.Property == IsBottomSheetOpenProperty)
        {
            if (change.GetNewValue<bool>())
            {
                CancelBottomSheetContentClear();
                _bottomSheetBackdropBlur?.RefreshBackdrop();
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
                _drawerBackdropBlur?.RefreshBackdrop();
            }
            else
            {
                ScheduleDrawerContentClear();
            }
            _overlayInputPaneAvoidance.UpdateOverlayState();
        }
        else if (change.Property == GlassModeProperty)
        {
            RefreshBackdrop();
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

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _overlayInputPaneAvoidance.UpdateOverlayState();
    }

    private void SyncInsetsManagerSubscription()
    {
        DetachInsetsManager();
        if (!ShouldProvideSafeArea() || _topLevel?.InsetsManager is not { } insetsManager)
        {
            return;
        }

        _insetsManager = insetsManager;
        AcquireInsetsManagerEdgeToEdgePreference(insetsManager);
        _insetsManager.SafeAreaChanged += OnSafeAreaChanged;
    }

    private void DetachInsetsManager()
    {
        if (_insetsManager != null)
        {
            _insetsManager.SafeAreaChanged -= OnSafeAreaChanged;
            ReleaseInsetsManagerEdgeToEdgePreference();
            _insetsManager = null;
        }
    }

    private void AcquireInsetsManagerEdgeToEdgePreference(IInsetsManager insetsManager)
    {
        ReleaseInsetsManagerEdgeToEdgePreference();
        lock (SafeAreaOverrideLock)
        {
            bool isFirstOverride = false;
            if (!EdgeToEdgeOverrideStates.TryGetValue(insetsManager, out EdgeToEdgeOverrideState? state))
            {
                state = new EdgeToEdgeOverrideState
                {
                    PreviousValue = insetsManager.DisplayEdgeToEdgePreference,
                    PreviousSystemBarColor = insetsManager.SystemBarColor
                };
                EdgeToEdgeOverrideStates.Add(insetsManager, state);
                isFirstOverride = true;
            }

            if (UseTransparentSystemBars)
            {
                if (state.TransparentSystemBarCount == 0)
                {
                    insetsManager.SystemBarColor = Colors.Transparent;
                }

                state.TransparentSystemBarCount++;
                _hasTransparentSystemBarColorOverride = true;
            }

            if (isFirstOverride)
            {
                insetsManager.DisplayEdgeToEdgePreference = true;
            }

            state.Count++;
        }

        _edgeToEdgeInsetsManager = insetsManager;
        _hasEdgeToEdgePreferenceOverride = true;
    }

    private void ReleaseInsetsManagerEdgeToEdgePreference()
    {
        if (!_hasEdgeToEdgePreferenceOverride || _edgeToEdgeInsetsManager == null)
        {
            return;
        }

        IInsetsManager insetsManager = _edgeToEdgeInsetsManager;
        lock (SafeAreaOverrideLock)
        {
            if (EdgeToEdgeOverrideStates.TryGetValue(insetsManager, out EdgeToEdgeOverrideState? state))
            {
                if (_hasTransparentSystemBarColorOverride)
                {
                    state.TransparentSystemBarCount--;
                    if (state.TransparentSystemBarCount <= 0)
                    {
                        state.TransparentSystemBarCount = 0;
                        insetsManager.SystemBarColor = state.PreviousSystemBarColor;
                    }
                }

                state.Count--;
                if (state.Count <= 0)
                {
                    insetsManager.DisplayEdgeToEdgePreference = state.PreviousValue;
                    EdgeToEdgeOverrideStates.Remove(insetsManager);
                }
            }
        }

        _edgeToEdgeInsetsManager = null;
        _hasEdgeToEdgePreferenceOverride = false;
        _hasTransparentSystemBarColorOverride = false;
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
    {
        SafeAreaPadding = e.SafeAreaPadding;
    }

    private void UpdateSafeAreaPadding()
    {
        SafeAreaPadding = ShouldProvideSafeArea() && _insetsManager != null ? _insetsManager.SafeAreaPadding : default;
    }

    private void UpdateEffectiveSafeAreaPadding()
    {
        Thickness providedSafeAreaPadding = ShouldProvideSafeArea() ? SafeAreaPadding : default;
        if (ShouldProvideSafeArea())
        {
            LuminaInsets.SetSafeAreaPadding(this, providedSafeAreaPadding);
        }
        else
        {
            ClearValue(LuminaInsets.SafeAreaPaddingProperty);
        }

        Thickness inheritedSafeAreaPadding = LuminaInsets.GetSafeAreaPadding(this);
        LayoutSafeAreaPadding = ShouldApplyLayoutSafeArea() ? inheritedSafeAreaPadding : default;
        OverlaySafeAreaPadding = UseSafeAreaForOverlays && ShouldApplyOverlaySafeArea() ? inheritedSafeAreaPadding : default;
        ApplyBottomSheetSafeAreaPadding();
        ApplyDrawerSafeAreaPadding();
    }

    private bool ShouldProvideSafeArea()
    {
        if (_topLevel == null || !UseSafeArea || SafeAreaMode == LuminaSafeAreaMode.Disabled)
        {
            return false;
        }

        return SafeAreaMode == LuminaSafeAreaMode.Enabled || IsRootSafeAreaBoundary();
    }

    private bool ShouldApplyLayoutSafeArea()
    {
        return SafeAreaMode switch
        {
            LuminaSafeAreaMode.Enabled => UseSafeArea,
            LuminaSafeAreaMode.Disabled => false,
            _ => UseSafeArea && IsRootSafeAreaBoundary()
        };
    }

    private bool ShouldApplyOverlaySafeArea()
    {
        return SafeAreaMode switch
        {
            LuminaSafeAreaMode.Enabled => UseSafeArea,
            LuminaSafeAreaMode.Disabled => false,
            _ => UseSafeArea && IsRootSafeAreaBoundary()
        };
    }

    private bool ShouldOverrideTopLevelAutoSafeAreaPadding()
    {
        if (_topLevel == null)
        {
            return false;
        }

        return ShouldProvideSafeArea() || (IsRootSafeAreaBoundary() && (!UseSafeArea || SafeAreaMode == LuminaSafeAreaMode.Disabled));
    }

    private bool IsRootSafeAreaBoundary()
    {
        return !this.GetVisualAncestors().OfType<LuminaOverlayHost>().Any();
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

    private void SyncTopLevelSafeAreaMode()
    {
        RestoreTopLevelAutoSafeAreaPadding();
        if (!ShouldOverrideTopLevelAutoSafeAreaPadding() || _topLevel?.Content is not Control content)
        {
            return;
        }

        AcquireTopLevelAutoSafeAreaPaddingOverride(content);
    }

    private void AcquireTopLevelAutoSafeAreaPaddingOverride(Control target)
    {
        RestoreTopLevelAutoSafeAreaPadding();
        lock (SafeAreaOverrideLock)
        {
            if (!AutoSafeAreaOverrideStates.TryGetValue(target, out AutoSafeAreaOverrideState? state))
            {
                state = new AutoSafeAreaOverrideState
                {
                    PreviousValue = TopLevel.GetAutoSafeAreaPadding(target)
                };
                AutoSafeAreaOverrideStates.Add(target, state);
                TopLevel.SetAutoSafeAreaPadding(target, value: false);
            }

            state.Count++;
        }

        _autoSafeAreaTarget = target;
        _hasAutoSafeAreaOverride = true;
    }

    private void RestoreTopLevelAutoSafeAreaPadding()
    {
        if (!_hasAutoSafeAreaOverride || _autoSafeAreaTarget == null)
        {
            return;
        }

        Control target = _autoSafeAreaTarget;
        lock (SafeAreaOverrideLock)
        {
            if (AutoSafeAreaOverrideStates.TryGetValue(target, out AutoSafeAreaOverrideState? state))
            {
                state.Count--;
                if (state.Count <= 0)
                {
                    TopLevel.SetAutoSafeAreaPadding(target, state.PreviousValue);
                    AutoSafeAreaOverrideStates.Remove(target);
                }
            }
        }

        _autoSafeAreaTarget = null;
        _hasAutoSafeAreaOverride = false;
    }

    private static List<LuminaOverlayHost> GetOwnerChain(Control owner)
    {
        List<LuminaOverlayHost> candidates = new List<LuminaOverlayHost>();
        if (owner is LuminaOverlayHost overlayHost)
        {
            candidates.Add(overlayHost);
        }
        candidates.AddRange(owner.GetVisualAncestors().OfType<LuminaOverlayHost>());
        return candidates;
    }

    private static int GetVisualDepth(Control control)
    {
        return control.GetVisualAncestors().Count();
    }

    private void OnDialogOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsPointerSourceInsidePart(e.Source, "PART_DialogRoot"))
        {
            IsDialogOpen = false;
        }
    }

    private void OnBottomSheetOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsPointerSourceInsidePart(e.Source, "PART_BottomSheetContainer"))
        {
            IsBottomSheetOpen = false;
        }
    }

    private void OnDrawerOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsPointerSourceInsidePart(e.Source, "PART_DrawerContainer"))
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

    private void DetachOverlayHandlers()
    {
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
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                if (_bottomSheetClearCancellation == cancellation && !IsBottomSheetOpen && _ownsBottomSheetContent)
                {
                    _bottomSheetClearCancellation = null;
                    _ownsBottomSheetContent = false;
                    BottomSheetContent = null;
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

    private async Task ClearDrawerContentAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(DrawerClearDelay, cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                if (_drawerClearCancellation == cancellation && !IsDrawerOpen && _ownsDrawerContent)
                {
                    _drawerClearCancellation = null;
                    _ownsDrawerContent = false;
                    DrawerContent = null;
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

    private void RegisterAttachedOverlayHost()
    {
        lock (OverlayHostRegistryLock)
        {
            CleanupOverlayHostRegistry();
            AttachedOverlayHosts.Add(new WeakReference<LuminaOverlayHost>(this));
            RegisterOverlayHostKey(OverlayHostKey, this);
            Current = this;
        }
    }

    private void UnregisterAttachedOverlayHost()
    {
        lock (OverlayHostRegistryLock)
        {
            AttachedOverlayHosts.RemoveAll(IsThisOverlayHostReference);
            UnregisterOverlayHostKey(OverlayHostKey, this);
            if (Current == this)
            {
                Current = AttachedOverlayHosts.Select((WeakReference<LuminaOverlayHost> reference) => reference.TryGetTarget(out var target) ? target : null).OfType<LuminaOverlayHost>().LastOrDefault();
            }
        }
    }

    private void UpdateOverlayHostKey(string? oldKey, string? newKey)
    {
        lock (OverlayHostRegistryLock)
        {
            UnregisterOverlayHostKey(oldKey, this);
            RegisterOverlayHostKey(newKey, this);
        }
    }

    private static void RegisterOverlayHostKey(string? overlayHostKey, LuminaOverlayHost overlayHost)
    {
        if (!string.IsNullOrWhiteSpace(overlayHostKey))
        {
            OverlayHostRegistry[overlayHostKey] = new WeakReference<LuminaOverlayHost>(overlayHost);
        }
    }

    private static void UnregisterOverlayHostKey(string? overlayHostKey, LuminaOverlayHost overlayHost)
    {
        if (!string.IsNullOrWhiteSpace(overlayHostKey) && OverlayHostRegistry.TryGetValue(overlayHostKey, out WeakReference<LuminaOverlayHost>? reference) && reference.TryGetTarget(out var registeredOverlayHost) && registeredOverlayHost == overlayHost)
        {
            OverlayHostRegistry.Remove(overlayHostKey);
        }
    }

    private static void CleanupOverlayHostRegistry()
    {
        AttachedOverlayHosts.RemoveAll((WeakReference<LuminaOverlayHost> reference) => !reference.TryGetTarget(out LuminaOverlayHost? _));
        foreach (string key in OverlayHostRegistry.Keys.ToArray())
        {
            if (!OverlayHostRegistry[key].TryGetTarget(out LuminaOverlayHost? _))
            {
                OverlayHostRegistry.Remove(key);
            }
        }
    }

    private bool IsThisOverlayHostReference(WeakReference<LuminaOverlayHost> reference)
    {
        LuminaOverlayHost? overlayHost;
        return reference.TryGetTarget(out overlayHost) && overlayHost == this;
    }
}
