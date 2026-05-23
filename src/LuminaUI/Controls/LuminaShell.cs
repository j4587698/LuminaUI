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
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaShell : ContentControl, ILuminaOverlayHost
{
	private static readonly object ShellRegistryLock = new object();

	private static readonly List<WeakReference<LuminaShell>> AttachedShells = new List<WeakReference<LuminaShell>>();

	private static readonly Dictionary<string, WeakReference<LuminaShell>> ShellRegistry = new Dictionary<string, WeakReference<LuminaShell>>(StringComparer.Ordinal);

	private const double MobileBreakpoint = 768.0;

	private const string WindowGlassClass = "WindowGlass";

	private static readonly TimeSpan BottomSheetClearDelay = TimeSpan.FromMilliseconds(360L);

	private readonly Dictionary<string, Func<Control>> _routeFactories = new Dictionary<string, Func<Control>>(StringComparer.Ordinal);

	private readonly Dictionary<string, Control> _routeCache = new Dictionary<string, Control>(StringComparer.Ordinal);

	private LuminaPage? _activePage;

	private ContentPresenter? _toastPresenter;

	private object? _toastContent;

	private CancellationTokenSource? _toastHideCancellation;

	private CancellationTokenSource? _bottomSheetClearCancellation;

	private TimeSpan? _pendingToastDuration;

	private bool _isNavigating;

	private bool _syncingNavigationKey;

	private bool _effectiveIsMenuOpen = true;

	private double _effectiveOpenPaneLength = 220.0;

	private double _effectiveCompactPaneLength = 48.0;

	public static readonly StyledProperty<bool> IsMenuOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>("IsMenuOpen", defaultValue: true);

	public static readonly StyledProperty<bool> IsShellChromeVisibleProperty = AvaloniaProperty.Register<LuminaShell, bool>("IsShellChromeVisible", defaultValue: true);

	public static readonly DirectProperty<LuminaShell, bool> EffectiveIsMenuOpenProperty = AvaloniaProperty.RegisterDirect<LuminaShell, bool>("EffectiveIsMenuOpen", (LuminaShell shell) => shell.EffectiveIsMenuOpen, null, unsetValue: false);

	public static readonly StyledProperty<string?> ShellKeyProperty = AvaloniaProperty.Register<LuminaShell, string?>("ShellKey");

	public static readonly StyledProperty<object?> MenuContentProperty = AvaloniaProperty.Register<LuminaShell, object?>("MenuContent");

	public static readonly StyledProperty<object?> MenuHeaderProperty = AvaloniaProperty.Register<LuminaShell, object?>("MenuHeader");

	public static readonly StyledProperty<object?> MenuFooterProperty = AvaloniaProperty.Register<LuminaShell, object?>("MenuFooter");

	public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<LuminaShell, object?>("Title");

	public static readonly StyledProperty<object?> DefaultPageTitleProperty = AvaloniaProperty.Register<LuminaShell, object?>("DefaultPageTitle");

	public static readonly StyledProperty<object?> DefaultPageSubtitleProperty = AvaloniaProperty.Register<LuminaShell, object?>("DefaultPageSubtitle");

	public static readonly StyledProperty<object?> DefaultPageActionsProperty = AvaloniaProperty.Register<LuminaShell, object?>("DefaultPageActions");

	public static readonly StyledProperty<object?> ActivePageTitleProperty = AvaloniaProperty.Register<LuminaShell, object?>("ActivePageTitle");

	public static readonly StyledProperty<object?> ActivePageSubtitleProperty = AvaloniaProperty.Register<LuminaShell, object?>("ActivePageSubtitle");

	public static readonly StyledProperty<object?> ActivePageActionsProperty = AvaloniaProperty.Register<LuminaShell, object?>("ActivePageActions");

	public static readonly StyledProperty<bool> AutoApplyPageMetadataProperty = AvaloniaProperty.Register<LuminaShell, bool>("AutoApplyPageMetadata", defaultValue: true);

	public static readonly StyledProperty<bool> AutoNavigateProperty = AvaloniaProperty.Register<LuminaShell, bool>("AutoNavigate", defaultValue: true);

	public static readonly StyledProperty<bool> CachePagesProperty = AvaloniaProperty.Register<LuminaShell, bool>("CachePages", defaultValue: true);

	public static readonly StyledProperty<bool> CloseMenuOnNavigateProperty = AvaloniaProperty.Register<LuminaShell, bool>("CloseMenuOnNavigate", defaultValue: true);

	public static readonly StyledProperty<LuminaPage?> ActivePageProperty = AvaloniaProperty.Register<LuminaShell, LuminaPage?>("ActivePage");

	public static readonly StyledProperty<Control?> ActiveRouteContentProperty = AvaloniaProperty.Register<LuminaShell, Control?>("ActiveRouteContent");

	public static readonly StyledProperty<string?> ActiveNavigationKeyProperty = AvaloniaProperty.Register<LuminaShell, string?>("ActiveNavigationKey", null, inherits: false, BindingMode.TwoWay);

	public static readonly StyledProperty<bool> IsDialogOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>("IsDialogOpen", defaultValue: false);

	public static readonly StyledProperty<object?> DialogContentProperty = AvaloniaProperty.Register<LuminaShell, object?>("DialogContent");

	public static readonly DirectProperty<LuminaShell, object?> ToastContentProperty = AvaloniaProperty.RegisterDirect("ToastContent", (LuminaShell shell) => shell.ToastContent, delegate(LuminaShell shell, object? value)
	{
		shell.ToastContent = value;
	});

	public static readonly StyledProperty<bool> IsBottomSheetOpenProperty = AvaloniaProperty.Register<LuminaShell, bool>("IsBottomSheetOpen", defaultValue: false);

	public static readonly StyledProperty<object?> BottomSheetContentProperty = AvaloniaProperty.Register<LuminaShell, object?>("BottomSheetContent");

	public static readonly StyledProperty<TimeSpan> ToastDurationProperty = AvaloniaProperty.Register<LuminaShell, TimeSpan>("ToastDuration", TimeSpan.FromSeconds(3L));

	public static readonly StyledProperty<IBrush?> PaneBackgroundProperty = AvaloniaProperty.Register<LuminaShell, IBrush?>("PaneBackground");

	public static readonly StyledProperty<double> OpenPaneLengthProperty = AvaloniaProperty.Register<LuminaShell, double>("OpenPaneLength", 220.0);

	public static readonly DirectProperty<LuminaShell, double> EffectiveOpenPaneLengthProperty = AvaloniaProperty.RegisterDirect<LuminaShell, double>("EffectiveOpenPaneLength", (LuminaShell shell) => shell.EffectiveOpenPaneLength, null, 0.0);

	public static readonly StyledProperty<double> CompactPaneLengthProperty = AvaloniaProperty.Register<LuminaShell, double>("CompactPaneLength", 48.0);

	public static readonly DirectProperty<LuminaShell, double> EffectiveCompactPaneLengthProperty = AvaloniaProperty.RegisterDirect<LuminaShell, double>("EffectiveCompactPaneLength", (LuminaShell shell) => shell.EffectiveCompactPaneLength, null, 0.0);

	public static readonly StyledProperty<bool> IsWindowGlassEnabledProperty = AvaloniaProperty.Register<LuminaShell, bool>("IsWindowGlassEnabled", defaultValue: false);

	public static LuminaShell? Current { get; private set; }

	public ICommand NavigateCommand { get; }

	public ICommand CloseDialogCommand { get; }

	public ICommand CloseBottomSheetCommand { get; }

	public ICommand ClearToastCommand { get; }

	public bool IsMenuOpen
	{
		get
		{
			return GetValue(IsMenuOpenProperty);
		}
		set
		{
			SetValue(IsMenuOpenProperty, value);
		}
	}

	public bool IsShellChromeVisible
	{
		get
		{
			return GetValue(IsShellChromeVisibleProperty);
		}
		set
		{
			SetValue(IsShellChromeVisibleProperty, value);
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

	public string? ShellKey
	{
		get
		{
			return GetValue(ShellKeyProperty);
		}
		set
		{
			SetValue(ShellKeyProperty, value);
		}
	}

	public object? MenuContent
	{
		get
		{
			return GetValue(MenuContentProperty);
		}
		set
		{
			SetValue(MenuContentProperty, value);
		}
	}

	public object? MenuHeader
	{
		get
		{
			return GetValue(MenuHeaderProperty);
		}
		set
		{
			SetValue(MenuHeaderProperty, value);
		}
	}

	public object? MenuFooter
	{
		get
		{
			return GetValue(MenuFooterProperty);
		}
		set
		{
			SetValue(MenuFooterProperty, value);
		}
	}

	public object? Title
	{
		get
		{
			return GetValue(TitleProperty);
		}
		set
		{
			SetValue(TitleProperty, value);
		}
	}

	public object? DefaultPageTitle
	{
		get
		{
			return GetValue(DefaultPageTitleProperty);
		}
		set
		{
			SetValue(DefaultPageTitleProperty, value);
		}
	}

	public object? DefaultPageSubtitle
	{
		get
		{
			return GetValue(DefaultPageSubtitleProperty);
		}
		set
		{
			SetValue(DefaultPageSubtitleProperty, value);
		}
	}

	public object? DefaultPageActions
	{
		get
		{
			return GetValue(DefaultPageActionsProperty);
		}
		set
		{
			SetValue(DefaultPageActionsProperty, value);
		}
	}

	public object? ActivePageTitle
	{
		get
		{
			return GetValue(ActivePageTitleProperty);
		}
		set
		{
			SetValue(ActivePageTitleProperty, value);
		}
	}

	public object? ActivePageSubtitle
	{
		get
		{
			return GetValue(ActivePageSubtitleProperty);
		}
		set
		{
			SetValue(ActivePageSubtitleProperty, value);
		}
	}

	public object? ActivePageActions
	{
		get
		{
			return GetValue(ActivePageActionsProperty);
		}
		set
		{
			SetValue(ActivePageActionsProperty, value);
		}
	}

	public bool AutoApplyPageMetadata
	{
		get
		{
			return GetValue(AutoApplyPageMetadataProperty);
		}
		set
		{
			SetValue(AutoApplyPageMetadataProperty, value);
		}
	}

	public bool AutoNavigate
	{
		get
		{
			return GetValue(AutoNavigateProperty);
		}
		set
		{
			SetValue(AutoNavigateProperty, value);
		}
	}

	public bool CachePages
	{
		get
		{
			return GetValue(CachePagesProperty);
		}
		set
		{
			SetValue(CachePagesProperty, value);
		}
	}

	public bool CloseMenuOnNavigate
	{
		get
		{
			return GetValue(CloseMenuOnNavigateProperty);
		}
		set
		{
			SetValue(CloseMenuOnNavigateProperty, value);
		}
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
		get
		{
			return GetValue(ActiveNavigationKeyProperty);
		}
		set
		{
			SetValue(ActiveNavigationKeyProperty, value);
		}
	}

	public IEnumerable<Control> CachedRouteContents => _routeCache.Values;

	public bool IsDialogOpen
	{
		get
		{
			return GetValue(IsDialogOpenProperty);
		}
		set
		{
			SetValue(IsDialogOpenProperty, value);
		}
	}

	public object? DialogContent
	{
		get
		{
			return GetValue(DialogContentProperty);
		}
		set
		{
			SetValue(DialogContentProperty, value);
		}
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
		get
		{
			return GetValue(IsBottomSheetOpenProperty);
		}
		set
		{
			SetValue(IsBottomSheetOpenProperty, value);
		}
	}

	public object? BottomSheetContent
	{
		get
		{
			return GetValue(BottomSheetContentProperty);
		}
		set
		{
			SetValue(BottomSheetContentProperty, value);
		}
	}

	public TimeSpan ToastDuration
	{
		get
		{
			return GetValue(ToastDurationProperty);
		}
		set
		{
			SetValue(ToastDurationProperty, value);
		}
	}

	public IBrush? PaneBackground
	{
		get
		{
			return GetValue(PaneBackgroundProperty);
		}
		set
		{
			SetValue(PaneBackgroundProperty, value);
		}
	}

	public double OpenPaneLength
	{
		get
		{
			return GetValue(OpenPaneLengthProperty);
		}
		set
		{
			SetValue(OpenPaneLengthProperty, value);
		}
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
		get
		{
			return GetValue(CompactPaneLengthProperty);
		}
		set
		{
			SetValue(CompactPaneLengthProperty, value);
		}
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

	public bool IsWindowGlassEnabled
	{
		get
		{
			return GetValue(IsWindowGlassEnabledProperty);
		}
		set
		{
			SetValue(IsWindowGlassEnabledProperty, value);
		}
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		RegisterAttachedShell();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (_activePage != null)
		{
			_activePage.PropertyChanged -= OnActivePagePropertyChanged;
			_activePage = null;
		}
		ActivePage = null;
		ActiveRouteContent = null;
		CancelToastHide();
		CancelBottomSheetContentClear();
		base.OnDetachedFromVisualTree(e);
		UnregisterAttachedShell();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		Control dialogOverlay = e.NameScope.FindRequired<Control>("PART_DialogOverlay");
		if (dialogOverlay != null)
		{
			dialogOverlay.AddHandler(InputElement.PointerPressedEvent, delegate(object? s, PointerPressedEventArgs ev)
			{
				if (!IsPointerSourceInsidePart(ev.Source, "PART_DialogRoot"))
				{
					IsDialogOpen = false;
				}
			}, RoutingStrategies.Tunnel, handledEventsToo: true);
		}
		Control bottomSheetOverlay = e.NameScope.FindRequired<Control>("PART_BottomSheetOverlay");
		if (bottomSheetOverlay != null)
		{
			bottomSheetOverlay.AddHandler(InputElement.PointerPressedEvent, delegate(object? s, PointerPressedEventArgs ev)
			{
				if (!IsPointerSourceInsidePart(ev.Source, "PART_BottomSheetContainer"))
				{
					IsBottomSheetOpen = false;
				}
			}, RoutingStrategies.Tunnel, handledEventsToo: true);
		}
		_toastPresenter = e.NameScope.FindRequired<ContentPresenter>("PART_ToastPresenter");
		if (_toastPresenter != null)
		{
			_toastPresenter.Content = ToastContent;
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
		NavigateCommand = new LuminaRelayCommand(delegate(object? parameter)
		{
			if (parameter is string navigationKey)
			{
				NavigateTo(navigationKey);
			}
		}, (object? parameter) => parameter is string value && !string.IsNullOrWhiteSpace(value));
		CloseDialogCommand = new LuminaRelayCommand(delegate
		{
			CloseDialog();
		});
		CloseBottomSheetCommand = new LuminaRelayCommand(delegate
		{
			CloseBottomSheet();
		});
		ClearToastCommand = new LuminaRelayCommand(delegate
		{
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
		BottomSheetContent = content;
		IsBottomSheetOpen = content != null;
	}

	public void CloseBottomSheet()
	{
		IsBottomSheetOpen = false;
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
			base.Content = content;
			SetActiveNavigationKey(navigationKey);
		}
		finally
		{
			_isNavigating = false;
		}
		if (closeMenuOnNavigate && base.Bounds.Width < 768.0)
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
		}
		else if (change.Property == AutoApplyPageMetadataProperty)
		{
			SetActivePage(base.Content);
		}
		else if (change.Property == DefaultPageTitleProperty || change.Property == DefaultPageSubtitleProperty || change.Property == DefaultPageActionsProperty)
		{
			ApplyActivePageMetadata();
		}
		else if (change.Property == IsMenuOpenProperty || change.Property == IsShellChromeVisibleProperty || change.Property == OpenPaneLengthProperty || change.Property == CompactPaneLengthProperty)
		{
			UpdateEffectiveShellChrome();
		}
		else if (change.Property == IsWindowGlassEnabledProperty)
		{
			if (change.GetNewValue<bool>())
			{
				if (!base.Classes.Contains("WindowGlass"))
				{
					base.Classes.Add("WindowGlass");
				}
			}
			else
			{
				base.Classes.Remove("WindowGlass");
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
			await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(delegate
			{
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
		await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(delegate
		{
			if (_bottomSheetClearCancellation == cancellation && !IsBottomSheetOpen)
			{
				_bottomSheetClearCancellation = null;
				BottomSheetContent = null;
			}
			cancellation.Dispose();
		});
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		base.OnSizeChanged(e);
		bool isMobile = e.NewSize.Width < 768.0;
		base.PseudoClasses.Set(":mobile", isMobile);
		if (!IsShellChromeVisible)
		{
			UpdateEffectiveShellChrome();
			return;
		}
		if (isMobile)
		{
			if (e.PreviousSize.Width >= 768.0 || e.PreviousSize.Width <= 0.0)
			{
				IsMenuOpen = false;
			}
		}
		else if (e.PreviousSize.Width < 768.0 || e.PreviousSize.Width <= 0.0)
		{
			IsMenuOpen = true;
		}
		UpdateEffectiveShellChrome();
	}

	private void UpdateEffectiveShellChrome()
	{
		EffectiveIsMenuOpen = IsShellChromeVisible && IsMenuOpen;
		EffectiveOpenPaneLength = (IsShellChromeVisible ? OpenPaneLength : 0.0);
		EffectiveCompactPaneLength = (IsShellChromeVisible ? CompactPaneLength : 0.0);
		base.PseudoClasses.Set(":chromeless", !IsShellChromeVisible);
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
		if (!(content is LuminaPage page))
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
		if (e.Property == LuminaPage.ShellTitleProperty || e.Property == LuminaPage.ShellSubtitleProperty || e.Property == LuminaPage.ShellActionsProperty || e.Property == LuminaPage.ShowShellChromeProperty || e.Property == LuminaPage.NavigationKeyProperty)
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
		ActivePageTitle = _activePage.ShellTitle ?? _activePage.Header ?? DefaultPageTitle;
		ActivePageSubtitle = _activePage.ShellSubtitle ?? DefaultPageSubtitle;
		ActivePageActions = _activePage.ShellActions ?? DefaultPageActions;
		IsShellChromeVisible = _activePage.ShowShellChrome;
		if (!string.IsNullOrWhiteSpace(_activePage.NavigationKey))
		{
			SetActiveNavigationKey(_activePage.NavigationKey);
		}
		if (IsShellChromeVisible && _activePage.CloseShellMenuOnNavigate && base.Bounds.Width < 768.0)
		{
			IsMenuOpen = false;
		}
	}

	private void ApplyDefaultPageMetadata()
	{
		ActivePageTitle = DefaultPageTitle;
		ActivePageSubtitle = DefaultPageSubtitle;
		ActivePageActions = DefaultPageActions;
		IsShellChromeVisible = true;
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
		string[] array = ShellRegistry.Keys.ToArray();
		foreach (string key in array)
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
