using System;
using System.Collections.Generic;
using System.Linq;
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
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaTopView : ContentControl, ILuminaOverlayHost
{
    private static readonly object TopViewRegistryLock = new object();

    private static readonly List<WeakReference<LuminaTopView>> AttachedTopViews = new List<WeakReference<LuminaTopView>>();

    private static readonly Dictionary<string, WeakReference<LuminaTopView>> TopViewRegistry = new Dictionary<string, WeakReference<LuminaTopView>>(StringComparer.Ordinal);

    private static readonly TimeSpan BottomSheetClearDelay = TimeSpan.FromMilliseconds(360);

    private static readonly TimeSpan DrawerClearDelay = TimeSpan.FromMilliseconds(360);

    private readonly LuminaOverlayInputPaneAvoidance _overlayInputPaneAvoidance;

    private Control? _dialogOverlay;

    private Control? _bottomSheetOverlay;

    private Control? _drawerOverlay;

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

    private Control? _autoSafeAreaTarget;

    private bool _autoSafeAreaPreviousValue;

    private bool _hasAutoSafeAreaOverride;

    private Thickness _safeAreaPadding;

    private Thickness _effectiveContentPadding;

    private Thickness _overlaySafeAreaPadding;

    public static readonly StyledProperty<string?> TopViewKeyProperty = AvaloniaProperty.Register<LuminaTopView, string?>(nameof(TopViewKey));

    public static readonly StyledProperty<bool> IsDialogOpenProperty = AvaloniaProperty.Register<LuminaTopView, bool>(nameof(IsDialogOpen), defaultValue: false);

    public static readonly StyledProperty<object?> DialogContentProperty = AvaloniaProperty.Register<LuminaTopView, object?>(nameof(DialogContent));

    public static readonly DirectProperty<LuminaTopView, object?> ToastContentProperty = AvaloniaProperty.RegisterDirect("ToastContent", (LuminaTopView topView) => topView.ToastContent, (LuminaTopView topView, object? value) =>
    {
        topView.ToastContent = value;
    });

    public static readonly StyledProperty<bool> IsBottomSheetOpenProperty = AvaloniaProperty.Register<LuminaTopView, bool>(nameof(IsBottomSheetOpen), defaultValue: false);

    public static readonly StyledProperty<object?> BottomSheetContentProperty = AvaloniaProperty.Register<LuminaTopView, object?>(nameof(BottomSheetContent));

    public static readonly StyledProperty<bool> IsDrawerOpenProperty = AvaloniaProperty.Register<LuminaTopView, bool>(nameof(IsDrawerOpen), defaultValue: false);

    public static readonly StyledProperty<object?> DrawerContentProperty = AvaloniaProperty.Register<LuminaTopView, object?>(nameof(DrawerContent));

    public static readonly StyledProperty<TimeSpan> ToastDurationProperty = AvaloniaProperty.Register<LuminaTopView, TimeSpan>(nameof(ToastDuration), TimeSpan.FromSeconds(3));

    public static readonly StyledProperty<bool> UseSafeAreaProperty = AvaloniaProperty.Register<LuminaTopView, bool>(nameof(UseSafeArea), defaultValue: true);

    public static readonly DirectProperty<LuminaTopView, Thickness> SafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaTopView, Thickness>(nameof(SafeAreaPadding), (LuminaTopView topView) => topView.SafeAreaPadding);

    public static readonly DirectProperty<LuminaTopView, Thickness> EffectiveContentPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaTopView, Thickness>(nameof(EffectiveContentPadding), (LuminaTopView topView) => topView.EffectiveContentPadding);

    public static readonly DirectProperty<LuminaTopView, Thickness> OverlaySafeAreaPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaTopView, Thickness>(nameof(OverlaySafeAreaPadding), (LuminaTopView topView) => topView.OverlaySafeAreaPadding);

    public ICommand CloseDialogCommand { get; }

    public ICommand CloseBottomSheetCommand { get; }

    public ICommand CloseDrawerCommand { get; }

    public ICommand ClearToastCommand { get; }

    public static LuminaTopView? Current { get; private set; }

    public string? TopViewKey
    {
        get => GetValue(TopViewKeyProperty);
        set => SetValue(TopViewKeyProperty, value);
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

    public Thickness SafeAreaPadding
    {
        get
        {
            return _safeAreaPadding;
        }
        private set
        {
            SetAndRaise(SafeAreaPaddingProperty, ref _safeAreaPadding, value);
            UpdateEffectiveSafeAreaPadding();
        }
    }

    public Thickness EffectiveContentPadding
    {
        get
        {
            return _effectiveContentPadding;
        }
        private set
        {
            SetAndRaise(EffectiveContentPaddingProperty, ref _effectiveContentPadding, value);
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

    public LuminaTopView()
    {
        _overlayInputPaneAvoidance = new LuminaOverlayInputPaneAvoidance(this, () => IsDialogOpen, () => IsBottomSheetOpen);
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

    public static LuminaTopView? FindFor(Control? owner)
    {
        if (owner == null)
        {
            return Current;
        }
        List<LuminaTopView> candidates = GetOwnerChain(owner);
        if (candidates.Count > 0)
        {
            return candidates.OrderByDescending(GetVisualDepth).First();
        }
        return TopLevel.GetTopLevel(owner)?.GetVisualDescendants().OfType<LuminaTopView>().OrderByDescending(GetVisualDepth)
            .FirstOrDefault() ?? Current;
    }

    public static LuminaTopView? FindOuterFor(Control? owner)
    {
        if (owner == null)
        {
            return Current;
        }
        List<LuminaTopView> candidates = GetOwnerChain(owner);
        if (candidates.Count > 0)
        {
            return candidates.OrderBy(GetVisualDepth).First();
        }
        return TopLevel.GetTopLevel(owner)?.GetVisualDescendants().OfType<LuminaTopView>().OrderBy(GetVisualDepth)
            .FirstOrDefault() ?? Current;
    }

    public static LuminaTopView? GetTopView(string topViewKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topViewKey, "topViewKey");
        lock (TopViewRegistryLock)
        {
            CleanupTopViewRegistry();
            WeakReference<LuminaTopView>? reference;
            LuminaTopView? topView;
            return (TopViewRegistry.TryGetValue(topViewKey, out reference) && reference.TryGetTarget(out topView)) ? topView : null;
        }
    }

    public static IReadOnlyList<LuminaTopView> GetOpenTopViews()
    {
        lock (TopViewRegistryLock)
        {
            CleanupTopViewRegistry();
            return AttachedTopViews.Select((WeakReference<LuminaTopView> reference) => reference.TryGetTarget(out var target) ? target : null).OfType<LuminaTopView>().ToArray();
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RegisterAttachedTopView();
        _topLevel = TopLevel.GetTopLevel(this);
        SyncInsetsManagerSubscription();
        UpdateSafeAreaPadding();
        SyncTopLevelSafeAreaMode();
        _overlayInputPaneAvoidance.AttachToVisualTree();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelToastHide();
        CancelBottomSheetContentClear();
        CancelDrawerContentClear();
        DetachInsetsManager();
        RestoreTopLevelAutoSafeAreaPadding();
        _topLevel = null;
        _overlayInputPaneAvoidance.DetachFromVisualTree();
        DetachOverlayHandlers();
        base.OnDetachedFromVisualTree(e);
        UnregisterAttachedTopView();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DetachOverlayHandlers();
        _dialogOverlay = e.NameScope.FindRequired<Control>("PART_DialogOverlay");
        if (_dialogOverlay != null)
        {
            _dialogOverlay.AddHandler(InputElement.PointerPressedEvent, OnDialogOverlayPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        _bottomSheetOverlay = e.NameScope.FindRequired<Control>("PART_BottomSheetOverlay");
        if (_bottomSheetOverlay != null)
        {
            _bottomSheetOverlay.AddHandler(InputElement.PointerPressedEvent, OnBottomSheetOverlayPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
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
            e.NameScope.FindRequired<Control>("PART_BottomSheetContainer"));
        ApplyBottomSheetSafeAreaPadding();
        ApplyDrawerSafeAreaPadding();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopViewKeyProperty)
        {
            UpdateTopViewKey(change.GetOldValue<string>(), change.GetNewValue<string>());
        }
        else if (change.Property == UseSafeAreaProperty)
        {
            SyncTopLevelSafeAreaMode();
            SyncInsetsManagerSubscription();
            UpdateSafeAreaPadding();
        }
        else if (change.Property == PaddingProperty)
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
        if (!UseSafeArea || _topLevel?.InsetsManager is not { } insetsManager)
        {
            return;
        }

        _insetsManager = insetsManager;
        _insetsManager.SafeAreaChanged += OnSafeAreaChanged;
    }

    private void DetachInsetsManager()
    {
        if (_insetsManager != null)
        {
            _insetsManager.SafeAreaChanged -= OnSafeAreaChanged;
            _insetsManager = null;
        }
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
    {
        SafeAreaPadding = e.SafeAreaPadding;
    }

    private void UpdateSafeAreaPadding()
    {
        SafeAreaPadding = UseSafeArea && _insetsManager != null ? _insetsManager.SafeAreaPadding : default;
    }

    private void UpdateEffectiveSafeAreaPadding()
    {
        Thickness safeAreaPadding = UseSafeArea ? SafeAreaPadding : default;
        EffectiveContentPadding = new Thickness(
            Padding.Left + safeAreaPadding.Left,
            Padding.Top + safeAreaPadding.Top,
            Padding.Right + safeAreaPadding.Right,
            Padding.Bottom + safeAreaPadding.Bottom);
        OverlaySafeAreaPadding = safeAreaPadding;
        ApplyBottomSheetSafeAreaPadding();
        ApplyDrawerSafeAreaPadding();
        System.Diagnostics.Debug.WriteLine($"[LuminaTopView] SafeAreaPadding={safeAreaPadding}, Padding={Padding}, EffectiveContentPadding={EffectiveContentPadding}");
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
        if (!UseSafeArea || _topLevel?.Content is not Control content)
        {
            return;
        }

        _autoSafeAreaTarget = content;
        _autoSafeAreaPreviousValue = TopLevel.GetAutoSafeAreaPadding(content);
        _hasAutoSafeAreaOverride = true;
        System.Diagnostics.Debug.WriteLine($"[LuminaTopView] SyncTopLevelSafeAreaMode: _autoSafeAreaPreviousValue={_autoSafeAreaPreviousValue}, SafeAreaPadding={SafeAreaPadding}");
        TopLevel.SetAutoSafeAreaPadding(content, value: false);
    }

    private void RestoreTopLevelAutoSafeAreaPadding()
    {
        if (!_hasAutoSafeAreaOverride || _autoSafeAreaTarget == null)
        {
            return;
        }

        TopLevel.SetAutoSafeAreaPadding(_autoSafeAreaTarget, _autoSafeAreaPreviousValue);
        _autoSafeAreaTarget = null;
        _autoSafeAreaPreviousValue = false;
        _hasAutoSafeAreaOverride = false;
    }

    private static List<LuminaTopView> GetOwnerChain(Control owner)
    {
        List<LuminaTopView> candidates = new List<LuminaTopView>();
        if (owner is LuminaTopView topView)
        {
            candidates.Add(topView);
        }
        candidates.AddRange(owner.GetVisualAncestors().OfType<LuminaTopView>());
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

    private void RegisterAttachedTopView()
    {
        lock (TopViewRegistryLock)
        {
            CleanupTopViewRegistry();
            AttachedTopViews.Add(new WeakReference<LuminaTopView>(this));
            RegisterTopViewKey(TopViewKey, this);
            Current = this;
        }
    }

    private void UnregisterAttachedTopView()
    {
        lock (TopViewRegistryLock)
        {
            AttachedTopViews.RemoveAll(IsThisTopViewReference);
            UnregisterTopViewKey(TopViewKey, this);
            if (Current == this)
            {
                Current = AttachedTopViews.Select((WeakReference<LuminaTopView> reference) => reference.TryGetTarget(out var target) ? target : null).OfType<LuminaTopView>().LastOrDefault();
            }
        }
    }

    private void UpdateTopViewKey(string? oldKey, string? newKey)
    {
        lock (TopViewRegistryLock)
        {
            UnregisterTopViewKey(oldKey, this);
            RegisterTopViewKey(newKey, this);
        }
    }

    private static void RegisterTopViewKey(string? topViewKey, LuminaTopView topView)
    {
        if (!string.IsNullOrWhiteSpace(topViewKey))
        {
            TopViewRegistry[topViewKey] = new WeakReference<LuminaTopView>(topView);
        }
    }

    private static void UnregisterTopViewKey(string? topViewKey, LuminaTopView topView)
    {
        if (!string.IsNullOrWhiteSpace(topViewKey) && TopViewRegistry.TryGetValue(topViewKey, out WeakReference<LuminaTopView>? reference) && reference.TryGetTarget(out var registeredTopView) && registeredTopView == topView)
        {
            TopViewRegistry.Remove(topViewKey);
        }
    }

    private static void CleanupTopViewRegistry()
    {
        AttachedTopViews.RemoveAll((WeakReference<LuminaTopView> reference) => !reference.TryGetTarget(out LuminaTopView? _));
        foreach (string key in TopViewRegistry.Keys.ToArray())
        {
            if (!TopViewRegistry[key].TryGetTarget(out LuminaTopView? _))
            {
                TopViewRegistry.Remove(key);
            }
        }
    }

    private bool IsThisTopViewReference(WeakReference<LuminaTopView> reference)
    {
        LuminaTopView? topView;
        return reference.TryGetTarget(out topView) && topView == this;
    }
}
