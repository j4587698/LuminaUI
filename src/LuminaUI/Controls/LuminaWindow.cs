using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace LuminaUI.Controls;

public class LuminaWindow : Window
{
	private const double DefaultTitleBarHeight = 40.0;

	private const double DefaultNativeSystemButtonReservedWidth = 72.0;

	private const int DefaultFullScreenTitleBarAutoHideDelay = 1000;

	private const int DefaultFullScreenTitleBarAutoShowDelay = 160;

	private const double FullScreenTitleBarShowEdge = 10.0;

	private const double FullScreenTitleBarHideEdge = 56.0;

	private const string SolidBackgroundResourceKey = "LuminaBackgroundBrush";

	private const string GlassBackgroundResourceKey = "LuminaWindowGlassBackgroundBrush";

	private readonly Grid _rootSurface;

	private readonly LuminaTitleBar _titleBar;

	private readonly ContentControl _contentHost;

	private readonly ContentControl _overlayHost;

	private readonly DispatcherTimer _hideTitleBarTimer;

	private readonly DispatcherTimer _showTitleBarTimer;

	private bool _fullScreenTitleBarVisible;

	private bool _settingInternalContent;

	public static readonly StyledProperty<object?> TitleBarLeftContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>("TitleBarLeftContent");

	public static readonly StyledProperty<object?> TitleBarContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>("TitleBarContent");

	public static readonly StyledProperty<object?> TitleBarRightContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>("TitleBarRightContent");

	public static readonly StyledProperty<double> NativeSystemButtonReservedWidthProperty = AvaloniaProperty.Register<LuminaWindow, double>("NativeSystemButtonReservedWidth", 72.0);

	public static readonly StyledProperty<object?> OverlayContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>("OverlayContent");

	public static readonly StyledProperty<bool> ShowTitleBarProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ShowTitleBar", defaultValue: true);

	public static readonly StyledProperty<bool> ShowSystemButtonsProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ShowSystemButtons", defaultValue: true);

	public static readonly StyledProperty<bool> ShowFullScreenButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ShowFullScreenButton", defaultValue: false);

	public static readonly StyledProperty<bool> ShowPinButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ShowPinButton", defaultValue: false);

	public static readonly StyledProperty<bool> ShowMinimizeButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ShowMinimizeButton", defaultValue: true);

	public static readonly StyledProperty<bool> ShowMaximizeButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ShowMaximizeButton", defaultValue: true);

	public static readonly StyledProperty<bool> ShowCloseButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ShowCloseButton", defaultValue: true);

	public static readonly StyledProperty<LuminaWindowChromeMode> WindowChromeModeProperty = AvaloniaProperty.Register<LuminaWindow, LuminaWindowChromeMode>("WindowChromeMode", LuminaWindowChromeMode.Lumina);

	public static readonly StyledProperty<LuminaExtendedSystemButtonMode> ExtendedSystemButtonModeProperty = AvaloniaProperty.Register<LuminaWindow, LuminaExtendedSystemButtonMode>("ExtendedSystemButtonMode", LuminaExtendedSystemButtonMode.Auto);

	public static readonly StyledProperty<LuminaFullScreenTitleBarBehavior> FullScreenTitleBarBehaviorProperty = AvaloniaProperty.Register<LuminaWindow, LuminaFullScreenTitleBarBehavior>("FullScreenTitleBarBehavior", LuminaFullScreenTitleBarBehavior.AutoHide);

	public static readonly StyledProperty<int> FullScreenTitleBarAutoHideDelayProperty = AvaloniaProperty.Register<LuminaWindow, int>("FullScreenTitleBarAutoHideDelay", 1000);

	public static readonly StyledProperty<int> FullScreenTitleBarAutoShowDelayProperty = AvaloniaProperty.Register<LuminaWindow, int>("FullScreenTitleBarAutoShowDelay", 160);

	public static readonly StyledProperty<bool> UseWindowGlassProperty = AvaloniaProperty.Register<LuminaWindow, bool>("UseWindowGlass", defaultValue: true);

	public static readonly StyledProperty<bool> IsWindowGlassEnabledProperty = AvaloniaProperty.Register<LuminaWindow, bool>("IsWindowGlassEnabled", defaultValue: false);

	public static readonly StyledProperty<bool> IsNativeSystemButtonAreaOnLeftProperty = AvaloniaProperty.Register<LuminaWindow, bool>("IsNativeSystemButtonAreaOnLeft", defaultValue: false);

	public static readonly StyledProperty<bool> ApplyWindowChromeDefaultsProperty = AvaloniaProperty.Register<LuminaWindow, bool>("ApplyWindowChromeDefaults", defaultValue: true);

	public static readonly StyledProperty<IBrush?> ContentBackgroundProperty = AvaloniaProperty.Register<LuminaWindow, IBrush?>("ContentBackground");

	public static readonly StyledProperty<IBrush?> GlassContentBackgroundProperty = AvaloniaProperty.Register<LuminaWindow, IBrush?>("GlassContentBackground");

	public LuminaTitleBar TitleBar => _titleBar;

	public object? TitleBarLeftContent
	{
		get
		{
			return GetValue(TitleBarLeftContentProperty);
		}
		set
		{
			SetValue(TitleBarLeftContentProperty, value);
		}
	}

	public object? TitleBarContent
	{
		get
		{
			return GetValue(TitleBarContentProperty);
		}
		set
		{
			SetValue(TitleBarContentProperty, value);
		}
	}

	public object? TitleBarRightContent
	{
		get
		{
			return GetValue(TitleBarRightContentProperty);
		}
		set
		{
			SetValue(TitleBarRightContentProperty, value);
		}
	}

	public double NativeSystemButtonReservedWidth
	{
		get
		{
			return GetValue(NativeSystemButtonReservedWidthProperty);
		}
		set
		{
			SetValue(NativeSystemButtonReservedWidthProperty, value);
		}
	}

	public object? OverlayContent
	{
		get
		{
			return GetValue(OverlayContentProperty);
		}
		set
		{
			SetValue(OverlayContentProperty, value);
		}
	}

	public bool ShowTitleBar
	{
		get
		{
			return GetValue(ShowTitleBarProperty);
		}
		set
		{
			SetValue(ShowTitleBarProperty, value);
		}
	}

	public bool ShowSystemButtons
	{
		get
		{
			return GetValue(ShowSystemButtonsProperty);
		}
		set
		{
			SetValue(ShowSystemButtonsProperty, value);
		}
	}

	public bool ShowFullScreenButton
	{
		get
		{
			return GetValue(ShowFullScreenButtonProperty);
		}
		set
		{
			SetValue(ShowFullScreenButtonProperty, value);
		}
	}

	public bool ShowPinButton
	{
		get
		{
			return GetValue(ShowPinButtonProperty);
		}
		set
		{
			SetValue(ShowPinButtonProperty, value);
		}
	}

	public bool ShowMinimizeButton
	{
		get
		{
			return GetValue(ShowMinimizeButtonProperty);
		}
		set
		{
			SetValue(ShowMinimizeButtonProperty, value);
		}
	}

	public bool ShowMaximizeButton
	{
		get
		{
			return GetValue(ShowMaximizeButtonProperty);
		}
		set
		{
			SetValue(ShowMaximizeButtonProperty, value);
		}
	}

	public bool ShowCloseButton
	{
		get
		{
			return GetValue(ShowCloseButtonProperty);
		}
		set
		{
			SetValue(ShowCloseButtonProperty, value);
		}
	}

	public LuminaWindowChromeMode WindowChromeMode
	{
		get
		{
			return GetValue(WindowChromeModeProperty);
		}
		set
		{
			SetValue(WindowChromeModeProperty, value);
		}
	}

	public LuminaExtendedSystemButtonMode ExtendedSystemButtonMode
	{
		get
		{
			return GetValue(ExtendedSystemButtonModeProperty);
		}
		set
		{
			SetValue(ExtendedSystemButtonModeProperty, value);
		}
	}

	public LuminaFullScreenTitleBarBehavior FullScreenTitleBarBehavior
	{
		get
		{
			return GetValue(FullScreenTitleBarBehaviorProperty);
		}
		set
		{
			SetValue(FullScreenTitleBarBehaviorProperty, value);
		}
	}

	public int FullScreenTitleBarAutoHideDelay
	{
		get
		{
			return GetValue(FullScreenTitleBarAutoHideDelayProperty);
		}
		set
		{
			SetValue(FullScreenTitleBarAutoHideDelayProperty, value);
		}
	}

	public int FullScreenTitleBarAutoShowDelay
	{
		get
		{
			return GetValue(FullScreenTitleBarAutoShowDelayProperty);
		}
		set
		{
			SetValue(FullScreenTitleBarAutoShowDelayProperty, value);
		}
	}

	public bool UseWindowGlass
	{
		get
		{
			return GetValue(UseWindowGlassProperty);
		}
		set
		{
			SetValue(UseWindowGlassProperty, value);
		}
	}

	public bool IsWindowGlassEnabled
	{
		get
		{
			return GetValue(IsWindowGlassEnabledProperty);
		}
		private set
		{
			SetValue(IsWindowGlassEnabledProperty, value);
		}
	}

	public bool IsNativeSystemButtonAreaOnLeft
	{
		get
		{
			return GetValue(IsNativeSystemButtonAreaOnLeftProperty);
		}
		private set
		{
			SetValue(IsNativeSystemButtonAreaOnLeftProperty, value);
		}
	}

	public bool ApplyWindowChromeDefaults
	{
		get
		{
			return GetValue(ApplyWindowChromeDefaultsProperty);
		}
		set
		{
			SetValue(ApplyWindowChromeDefaultsProperty, value);
		}
	}

	public IBrush? ContentBackground
	{
		get
		{
			return GetValue(ContentBackgroundProperty);
		}
		set
		{
			SetValue(ContentBackgroundProperty, value);
		}
	}

	public IBrush? GlassContentBackground
	{
		get
		{
			return GetValue(GlassContentBackgroundProperty);
		}
		set
		{
			SetValue(GlassContentBackgroundProperty, value);
		}
	}

	public LuminaWindow()
	{
		base.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		base.VerticalContentAlignment = VerticalAlignment.Stretch;
		_hideTitleBarTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(1000L)
		};
		_showTitleBarTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(160L)
		};
		_rootSurface = new Grid
		{
			Name = "PART_RootSurface",
			RowDefinitions = new RowDefinitions("Auto,*"),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_titleBar = new LuminaTitleBar
		{
			Name = "PART_TitleBar",
			Background = Brushes.Transparent
		};
		Grid.SetRow(_titleBar, 0);
		_contentHost = new ContentControl
		{
			Name = "PART_ContentHost",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch
		};
		Grid.SetRow(_contentHost, 1);
		_overlayHost = new ContentControl
		{
			Name = "PART_OverlayHost",
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch,
			ZIndex = 3000
		};
		Grid.SetRow(_overlayHost, 0);
		Grid.SetRowSpan(_overlayHost, 2);
		_rootSurface.Children.Add(_titleBar);
		_rootSurface.Children.Add(_contentHost);
		_rootSurface.Children.Add(_overlayHost);
		base.SizeChanged += delegate
		{
			SyncRootSurfaceSize();
		};
		base.Opened += delegate
		{
			UpdateWindowMaterial();
		};
		base.Loaded += delegate
		{
			SyncRootSurfaceSize();
			UpdateWindowMaterial();
		};
		base.Closed += delegate
		{
			StopFullScreenTitleBarTimers();
		};
		base.PointerMoved += OnWindowPointerMoved;
		_hideTitleBarTimer.Tick += OnHideTitleBarTimerTick;
		_showTitleBarTimer.Tick += OnShowTitleBarTimerTick;
		ApplyChromeDefaults();
		SyncChromeSlots();
		UpdateWindowMaterial();
		SetWindowContent(_rootSurface);
	}

	public void RefreshWindowMaterial()
	{
		UpdateWindowMaterial();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (!IsInternalChromeReady())
		{
			return;
		}
		if (change.Property == ContentControl.ContentProperty && !_settingInternalContent)
		{
			object content = change.GetNewValue<object>();
			if (content != _rootSurface)
			{
				SetWindowContent(_rootSurface);
				_contentHost.Content = content;
			}
		}
		else if (change.Property == Window.WindowStateProperty)
		{
			if (change.OldValue is WindowState oldState && change.NewValue is WindowState newState)
			{
				HandleWindowStateChanged(oldState, newState);
			}
			SyncChromeSlots();
			UpdateWindowMaterial();
		}
		else if (change.Property == WindowChromeModeProperty)
		{
			ApplyChromeDefaults();
			SyncChromeSlots();
			UpdateWindowMaterial();
		}
		else if (change.Property == ExtendedSystemButtonModeProperty)
		{
			ApplyChromeDefaults();
			SyncChromeSlots();
			UpdateWindowMaterial();
		}
		else if (change.Property == Window.WindowDecorationMarginProperty)
		{
			SyncChromeSlots();
		}
		else if (change.Property == FullScreenTitleBarBehaviorProperty)
		{
			ResetFullScreenTitleBarVisibility();
			SyncChromeSlots();
		}
		else if (change.Property == FullScreenTitleBarAutoHideDelayProperty)
		{
			_hideTitleBarTimer.Interval = TimeSpan.FromMilliseconds(FullScreenTitleBarAutoHideDelay);
		}
		else if (change.Property == FullScreenTitleBarAutoShowDelayProperty)
		{
			_showTitleBarTimer.Interval = TimeSpan.FromMilliseconds(FullScreenTitleBarAutoShowDelay);
		}
		else if (change.Property == TitleBarLeftContentProperty || change.Property == TitleBarContentProperty || change.Property == TitleBarRightContentProperty || change.Property == NativeSystemButtonReservedWidthProperty || change.Property == ShowTitleBarProperty || change.Property == ShowSystemButtonsProperty || change.Property == ShowFullScreenButtonProperty || change.Property == ShowPinButtonProperty || change.Property == ShowMinimizeButtonProperty || change.Property == ShowMaximizeButtonProperty || change.Property == ShowCloseButtonProperty || change.Property == WindowBase.TopmostProperty)
		{
			SyncChromeSlots();
		}
		else if (change.Property == OverlayContentProperty)
		{
			_overlayHost.Content = change.GetNewValue<object>();
		}
		else if (change.Property == UseWindowGlassProperty || change.Property == ContentBackgroundProperty || change.Property == GlassContentBackgroundProperty)
		{
			UpdateWindowMaterial();
		}
		else if (change.Property == ApplyWindowChromeDefaultsProperty)
		{
			ApplyChromeDefaults();
		}
		else if (change.Property.Name == "ActualTransparencyLevel" || change.Property.Name == "ActualThemeVariant")
		{
			UpdateWindowMaterial();
		}
	}

	private void SyncChromeSlots()
	{
		SyncTitleBarMetrics();
		IsNativeSystemButtonAreaOnLeft = UsesNativeSystemButtonAreaOnLeft();
		_titleBar.LeftContent = TitleBarLeftContent;
		_titleBar.LeftContentMargin = GetEffectiveLeftContentMargin();
		_titleBar.Content = TitleBarContent;
		_titleBar.RightContent = TitleBarRightContent;
		_titleBar.ShowSystemButtons = UsesLuminaSystemButtons() && ShowSystemButtons;
		_titleBar.ShowPinButton = ShowPinButton;
		_titleBar.ShowFullScreenButton = ShowFullScreenButton;
		_titleBar.ShowMinimizeButton = ShowMinimizeButton;
		_titleBar.ShowMaximizeButton = ShowMaximizeButton && base.WindowState != WindowState.FullScreen;
		_titleBar.ShowCloseButton = ShowCloseButton;
		_titleBar.IsPinned = base.Topmost;
		_titleBar.IsFullScreen = base.WindowState == WindowState.FullScreen;
		_titleBar.IsVisible = ShouldShowTitleBar();
		SyncChromeLayout();
	}

	private void SyncTitleBarMetrics()
	{
		_titleBar.Height = GetEffectiveTitleBarHeight();
	}

	private double GetEffectiveTitleBarHeight()
	{
		if (WindowChromeMode == LuminaWindowChromeMode.Extended && UsesNativeExtendedSystemButtons())
		{
			double nativeHeight = base.WindowDecorationMargin.Top;
			if (nativeHeight > 0.0)
			{
				return nativeHeight;
			}
		}
		return 40.0;
	}

	private Thickness GetEffectiveLeftContentMargin()
	{
		return IsNativeSystemButtonAreaOnLeft ? new Thickness(NativeSystemButtonReservedWidth + 16.0, 0.0, 16.0, 0.0) : new Thickness(16.0, 0.0, 16.0, 0.0);
	}

	private void SyncChromeLayout()
	{
		if (base.WindowState == WindowState.FullScreen || WindowChromeMode == LuminaWindowChromeMode.Platform)
		{
			Grid.SetRow(_contentHost, 0);
			Grid.SetRowSpan(_contentHost, 2);
			_contentHost.ZIndex = 0;
			_titleBar.ZIndex = 1;
		}
		else
		{
			Grid.SetRow(_contentHost, 1);
			Grid.SetRowSpan(_contentHost, 1);
			_contentHost.ZIndex = 0;
			_titleBar.ZIndex = 1;
		}
	}

	private bool ShouldShowTitleBar()
	{
		if (WindowChromeMode == LuminaWindowChromeMode.Platform || !ShowTitleBar)
		{
			return false;
		}
		if (WindowChromeMode == LuminaWindowChromeMode.Extended)
		{
			return base.WindowState != WindowState.FullScreen;
		}
		if (base.WindowState != WindowState.FullScreen)
		{
			return true;
		}
		LuminaFullScreenTitleBarBehavior fullScreenTitleBarBehavior = FullScreenTitleBarBehavior;
		if (1 == 0)
		{
		}
		bool result = fullScreenTitleBarBehavior switch
		{
			LuminaFullScreenTitleBarBehavior.Visible => true, 
			LuminaFullScreenTitleBarBehavior.AutoHide => _fullScreenTitleBarVisible, 
			_ => false, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void UpdateWindowMaterial()
	{
		if (IsInternalChromeReady())
		{
			bool glassEnabled = (IsWindowGlassEnabled = UseWindowGlass && SupportsHardwareBlur(base.ActualTransparencyLevel));
			_rootSurface.Background = (glassEnabled ? (GlassContentBackground ?? FindBrush("LuminaWindowGlassBackgroundBrush")) : (ContentBackground ?? FindBrush("LuminaBackgroundBrush")));
			LuminaTitleBar titleBar = _titleBar;
			object background;
			if (base.WindowState != WindowState.FullScreen)
			{
				IBrush transparent = Brushes.Transparent;
				background = transparent;
			}
			else
			{
				background = GlassContentBackground ?? FindBrush("LuminaWindowGlassBackgroundBrush") ?? ContentBackground ?? FindBrush("LuminaBackgroundBrush") ?? Brushes.Transparent;
			}
			titleBar.Background = (IBrush?)background;
		}
	}

	private bool IsInternalChromeReady()
	{
		return _rootSurface != null && _titleBar != null && _contentHost != null && _overlayHost != null;
	}

	private IBrush? FindBrush(string key)
	{
		return this.FindResource(base.ActualThemeVariant, key) as IBrush;
	}

	private static bool SupportsHardwareBlur(WindowTransparencyLevel actualTransparencyLevel)
	{
		return actualTransparencyLevel == WindowTransparencyLevel.AcrylicBlur || actualTransparencyLevel == WindowTransparencyLevel.Blur;
	}

	private void ApplyChromeDefaults()
	{
		if (ApplyWindowChromeDefaults)
		{
			if (WindowChromeMode == LuminaWindowChromeMode.Platform)
			{
				base.ExtendClientAreaToDecorationsHint = false;
				base.ExtendClientAreaTitleBarHeightHint = -1.0;
				base.WindowDecorations = WindowDecorations.Full;
				base.Background = ContentBackground ?? FindBrush("LuminaBackgroundBrush") ?? Brushes.Transparent;
				return;
			}
			base.ExtendClientAreaToDecorationsHint = true;
			base.ExtendClientAreaTitleBarHeightHint = ((WindowChromeMode == LuminaWindowChromeMode.Extended && UsesNativeExtendedSystemButtons()) ? (-1.0) : 40.0);
			base.TransparencyLevelHint = new[]
			{
				WindowTransparencyLevel.AcrylicBlur,
				WindowTransparencyLevel.Blur,
				WindowTransparencyLevel.Transparent
			};
			base.Background = Brushes.Transparent;
			base.WindowDecorations = ((WindowChromeMode == LuminaWindowChromeMode.Extended) ? GetExtendedWindowDecorations() : WindowDecorations.None);
		}
	}

	private bool UsesLuminaSystemButtons()
	{
		return WindowChromeMode == LuminaWindowChromeMode.Lumina || (WindowChromeMode == LuminaWindowChromeMode.Extended && !UsesNativeExtendedSystemButtons());
	}

	private bool UsesNativeExtendedSystemButtons()
	{
		LuminaExtendedSystemButtonMode extendedSystemButtonMode = ExtendedSystemButtonMode;
		if (1 == 0)
		{
		}
		bool result = extendedSystemButtonMode switch
		{
			LuminaExtendedSystemButtonMode.Native => true, 
			LuminaExtendedSystemButtonMode.Lumina => false, 
			_ => RuntimeInformation.IsOSPlatform(OSPlatform.OSX), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private bool UsesNativeSystemButtonAreaOnLeft()
	{
		return WindowChromeMode == LuminaWindowChromeMode.Extended && UsesNativeExtendedSystemButtons() && RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
	}

	private WindowDecorations GetExtendedWindowDecorations()
	{
		return (!UsesNativeExtendedSystemButtons()) ? WindowDecorations.BorderOnly : WindowDecorations.Full;
	}

	private void HandleWindowStateChanged(WindowState oldState, WindowState newState)
	{
		StopFullScreenTitleBarTimers();
		if (newState == WindowState.FullScreen)
		{
			_fullScreenTitleBarVisible = FullScreenTitleBarBehavior == LuminaFullScreenTitleBarBehavior.Visible;
		}
		else if (oldState == WindowState.FullScreen)
		{
			_fullScreenTitleBarVisible = false;
		}
	}

	private void ResetFullScreenTitleBarVisibility()
	{
		StopFullScreenTitleBarTimers();
		if (base.WindowState == WindowState.FullScreen)
		{
			_fullScreenTitleBarVisible = FullScreenTitleBarBehavior == LuminaFullScreenTitleBarBehavior.Visible;
		}
	}

	private void OnWindowPointerMoved(object? sender, PointerEventArgs e)
	{
		if (WindowChromeMode != LuminaWindowChromeMode.Lumina || base.WindowState != WindowState.FullScreen || FullScreenTitleBarBehavior != LuminaFullScreenTitleBarBehavior.AutoHide || !ShowTitleBar)
		{
			return;
		}
		double y = e.GetPosition(this).Y;
		if (y <= 10.0)
		{
			_hideTitleBarTimer.Stop();
			if (!_fullScreenTitleBarVisible)
			{
				_showTitleBarTimer.Start();
			}
		}
		else if (y >= 56.0)
		{
			_showTitleBarTimer.Stop();
			if (_fullScreenTitleBarVisible)
			{
				_hideTitleBarTimer.Start();
			}
		}
	}

	private void OnHideTitleBarTimerTick(object? sender, EventArgs e)
	{
		_hideTitleBarTimer.Stop();
		SetFullScreenTitleBarVisible(isVisible: false);
	}

	private void OnShowTitleBarTimerTick(object? sender, EventArgs e)
	{
		_showTitleBarTimer.Stop();
		SetFullScreenTitleBarVisible(isVisible: true);
	}

	private void SetFullScreenTitleBarVisible(bool isVisible)
	{
		if (_fullScreenTitleBarVisible != isVisible)
		{
			_fullScreenTitleBarVisible = isVisible;
			SyncChromeSlots();
			UpdateWindowMaterial();
		}
	}

	private void StopFullScreenTitleBarTimers()
	{
		_hideTitleBarTimer.Stop();
		_showTitleBarTimer.Stop();
	}

	private void SyncRootSurfaceSize()
	{
		if (IsInternalChromeReady())
		{
			Size layoutSize = base.Bounds.Size;
			double width = ((layoutSize.Width > 0.0) ? layoutSize.Width : base.ClientSize.Width);
			double height = ((layoutSize.Height > 0.0) ? layoutSize.Height : base.ClientSize.Height);
			if (width > 0.0)
			{
				_rootSurface.Width = width;
			}
			if (height > 0.0)
			{
				_rootSurface.Height = height;
			}
		}
	}

	private void SetWindowContent(object? content)
	{
		_settingInternalContent = true;
		try
		{
			base.Content = content;
		}
		finally
		{
			_settingInternalContent = false;
		}
	}
}
