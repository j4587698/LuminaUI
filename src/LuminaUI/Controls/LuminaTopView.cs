using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
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

    private Control? _dialogOverlay;

    private Control? _bottomSheetOverlay;

    private ContentPresenter? _toastPresenter;

    private object? _toastContent;

    private CancellationTokenSource? _toastHideCancellation;

    private CancellationTokenSource? _bottomSheetClearCancellation;

    private TimeSpan? _pendingToastDuration;

    public static readonly StyledProperty<string?> TopViewKeyProperty = AvaloniaProperty.Register<LuminaTopView, string?>(nameof(TopViewKey));

    public static readonly StyledProperty<bool> IsDialogOpenProperty = AvaloniaProperty.Register<LuminaTopView, bool>(nameof(IsDialogOpen), defaultValue: false);

    public static readonly StyledProperty<object?> DialogContentProperty = AvaloniaProperty.Register<LuminaTopView, object?>(nameof(DialogContent));

    public static readonly DirectProperty<LuminaTopView, object?> ToastContentProperty = AvaloniaProperty.RegisterDirect("ToastContent", (LuminaTopView topView) => topView.ToastContent, (LuminaTopView topView, object? value) =>
    {
        topView.ToastContent = value;
    });

    public static readonly StyledProperty<bool> IsBottomSheetOpenProperty = AvaloniaProperty.Register<LuminaTopView, bool>(nameof(IsBottomSheetOpen), defaultValue: false);

    public static readonly StyledProperty<object?> BottomSheetContentProperty = AvaloniaProperty.Register<LuminaTopView, object?>(nameof(BottomSheetContent));

    public static readonly StyledProperty<TimeSpan> ToastDurationProperty = AvaloniaProperty.Register<LuminaTopView, TimeSpan>(nameof(ToastDuration), TimeSpan.FromSeconds(3));

    public ICommand CloseDialogCommand { get; }

    public ICommand CloseBottomSheetCommand { get; }

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

    public TimeSpan ToastDuration
    {
        get => GetValue(ToastDurationProperty);
        set => SetValue(ToastDurationProperty, value);
    }

    public LuminaTopView()
    {
        CloseDialogCommand = new LuminaRelayCommand(_ => {
            CloseDialog();
        });
        CloseBottomSheetCommand = new LuminaRelayCommand(_ => {
            CloseBottomSheet();
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
        BottomSheetContent = content;
        IsBottomSheetOpen = content != null;
    }

    public void CloseBottomSheet()
    {
        IsBottomSheetOpen = false;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RegisterAttachedTopView();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelToastHide();
        CancelBottomSheetContentClear();
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
        _toastPresenter = e.NameScope.FindRequired<ContentPresenter>("PART_ToastPresenter");
        if (_toastPresenter != null)
        {
            _toastPresenter.Content = ToastContent;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopViewKeyProperty)
        {
            UpdateTopViewKey(change.GetOldValue<string>(), change.GetNewValue<string>());
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
        }
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
        if (BottomSheetContent != null)
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
            if (_bottomSheetClearCancellation == cancellation && !IsBottomSheetOpen)
            {
                _bottomSheetClearCancellation = null;
                BottomSheetContent = null;
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
