using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Localization;

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

    private const string AvaloniaDefaultApplicationName = "Avalonia Application";

    private readonly Grid _rootSurface;

    private readonly LuminaTitleBar _titleBar;

    private readonly ContentControl _contentHost;

    private readonly ContentControl _overlayHost;

    private readonly DispatcherTimer _hideTitleBarTimer;

    private readonly DispatcherTimer _showTitleBarTimer;

    private bool _fullScreenTitleBarVisible;

    private bool _settingInternalContent;

    private bool _canClose;

    public static readonly StyledProperty<object?> TitleBarLeftContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>(nameof(TitleBarLeftContent));

    public static readonly StyledProperty<object?> TitleBarContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>(nameof(TitleBarContent));

    public static readonly StyledProperty<object?> TitleBarRightContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>(nameof(TitleBarRightContent));

    public static readonly StyledProperty<double> NativeSystemButtonReservedWidthProperty = AvaloniaProperty.Register<LuminaWindow, double>(nameof(NativeSystemButtonReservedWidth), 72.0);

    public static readonly StyledProperty<object?> OverlayContentProperty = AvaloniaProperty.Register<LuminaWindow, object?>(nameof(OverlayContent));

    public static readonly StyledProperty<bool> ShowTitleBarProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ShowTitleBar), defaultValue: true);

    public static readonly StyledProperty<bool> ShowSystemButtonsProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ShowSystemButtons), defaultValue: true);

    public static readonly StyledProperty<bool> ShowFullScreenButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ShowFullScreenButton), defaultValue: false);

    public static readonly StyledProperty<bool> ShowPinButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ShowPinButton), defaultValue: false);

    public static readonly StyledProperty<bool> ShowMinimizeButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ShowMinimizeButton), defaultValue: true);

    public static readonly StyledProperty<bool> ShowMaximizeButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ShowMaximizeButton), defaultValue: true);

    public static readonly StyledProperty<bool> ShowCloseButtonProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ShowCloseButton), defaultValue: true);

    public static readonly StyledProperty<LuminaWindowChromeMode> WindowChromeModeProperty = AvaloniaProperty.Register<LuminaWindow, LuminaWindowChromeMode>(nameof(WindowChromeMode), LuminaWindowChromeMode.Lumina);

    public static readonly StyledProperty<LuminaExtendedSystemButtonMode> ExtendedSystemButtonModeProperty = AvaloniaProperty.Register<LuminaWindow, LuminaExtendedSystemButtonMode>(nameof(ExtendedSystemButtonMode), LuminaExtendedSystemButtonMode.Auto);

    public static readonly StyledProperty<LuminaFullScreenTitleBarBehavior> FullScreenTitleBarBehaviorProperty = AvaloniaProperty.Register<LuminaWindow, LuminaFullScreenTitleBarBehavior>(nameof(FullScreenTitleBarBehavior), LuminaFullScreenTitleBarBehavior.AutoHide);

    public static readonly StyledProperty<int> FullScreenTitleBarAutoHideDelayProperty = AvaloniaProperty.Register<LuminaWindow, int>(nameof(FullScreenTitleBarAutoHideDelay), 1000);

    public static readonly StyledProperty<int> FullScreenTitleBarAutoShowDelayProperty = AvaloniaProperty.Register<LuminaWindow, int>(nameof(FullScreenTitleBarAutoShowDelay), 160);

    public static readonly StyledProperty<bool> UseWindowGlassProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(UseWindowGlass), defaultValue: true);

    public static readonly StyledProperty<bool> IsWindowGlassEnabledProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(IsWindowGlassEnabled), defaultValue: false);

    public static readonly StyledProperty<bool> IsNativeSystemButtonAreaOnLeftProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(IsNativeSystemButtonAreaOnLeft), defaultValue: false);

    public static readonly StyledProperty<bool> ApplyWindowChromeDefaultsProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(ApplyWindowChromeDefaults), defaultValue: true);

    public static readonly StyledProperty<bool> UseApplicationNameAsDefaultTitleProperty = AvaloniaProperty.Register<LuminaWindow, bool>(nameof(UseApplicationNameAsDefaultTitle), defaultValue: true);

    public static readonly StyledProperty<IBrush?> ContentBackgroundProperty = AvaloniaProperty.Register<LuminaWindow, IBrush?>(nameof(ContentBackground));

    public static readonly StyledProperty<IBrush?> GlassContentBackgroundProperty = AvaloniaProperty.Register<LuminaWindow, IBrush?>(nameof(GlassContentBackground));

    public LuminaTitleBar TitleBar => _titleBar;

    public object? TitleBarLeftContent
    {
        get => GetValue(TitleBarLeftContentProperty);
        set => SetValue(TitleBarLeftContentProperty, value);
    }

    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    public object? TitleBarRightContent
    {
        get => GetValue(TitleBarRightContentProperty);
        set => SetValue(TitleBarRightContentProperty, value);
    }

    public double NativeSystemButtonReservedWidth
    {
        get => GetValue(NativeSystemButtonReservedWidthProperty);
        set => SetValue(NativeSystemButtonReservedWidthProperty, value);
    }

    public object? OverlayContent
    {
        get => GetValue(OverlayContentProperty);
        set => SetValue(OverlayContentProperty, value);
    }

    public bool ShowTitleBar
    {
        get => GetValue(ShowTitleBarProperty);
        set => SetValue(ShowTitleBarProperty, value);
    }

    public bool ShowSystemButtons
    {
        get => GetValue(ShowSystemButtonsProperty);
        set => SetValue(ShowSystemButtonsProperty, value);
    }

    public bool ShowFullScreenButton
    {
        get => GetValue(ShowFullScreenButtonProperty);
        set => SetValue(ShowFullScreenButtonProperty, value);
    }

    public bool ShowPinButton
    {
        get => GetValue(ShowPinButtonProperty);
        set => SetValue(ShowPinButtonProperty, value);
    }

    public bool ShowMinimizeButton
    {
        get => GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    public LuminaWindowChromeMode WindowChromeMode
    {
        get => GetValue(WindowChromeModeProperty);
        set => SetValue(WindowChromeModeProperty, value);
    }

    public LuminaExtendedSystemButtonMode ExtendedSystemButtonMode
    {
        get => GetValue(ExtendedSystemButtonModeProperty);
        set => SetValue(ExtendedSystemButtonModeProperty, value);
    }

    public LuminaFullScreenTitleBarBehavior FullScreenTitleBarBehavior
    {
        get => GetValue(FullScreenTitleBarBehaviorProperty);
        set => SetValue(FullScreenTitleBarBehaviorProperty, value);
    }

    public int FullScreenTitleBarAutoHideDelay
    {
        get => GetValue(FullScreenTitleBarAutoHideDelayProperty);
        set => SetValue(FullScreenTitleBarAutoHideDelayProperty, value);
    }

    public int FullScreenTitleBarAutoShowDelay
    {
        get => GetValue(FullScreenTitleBarAutoShowDelayProperty);
        set => SetValue(FullScreenTitleBarAutoShowDelayProperty, value);
    }

    public bool UseWindowGlass
    {
        get => GetValue(UseWindowGlassProperty);
        set => SetValue(UseWindowGlassProperty, value);
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
        get => GetValue(ApplyWindowChromeDefaultsProperty);
        set => SetValue(ApplyWindowChromeDefaultsProperty, value);
    }

    public bool UseApplicationNameAsDefaultTitle
    {
        get => GetValue(UseApplicationNameAsDefaultTitleProperty);
        set => SetValue(UseApplicationNameAsDefaultTitleProperty, value);
    }

    public IBrush? ContentBackground
    {
        get => GetValue(ContentBackgroundProperty);
        set => SetValue(ContentBackgroundProperty, value);
    }

    public IBrush? GlassContentBackground
    {
        get => GetValue(GlassContentBackgroundProperty);
        set => SetValue(GlassContentBackgroundProperty, value);
    }

    public LuminaWindow()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        _hideTitleBarTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000)
        };
        _showTitleBarTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(160)
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
        SizeChanged += (_, _) => {
            SyncRootSurfaceSize();
        };
        Opened += (_, _) => {
            ApplyDefaultTitle();
            UpdateWindowMaterial();
        };
        Loaded += (_, _) => {
            SyncRootSurfaceSize();
            UpdateWindowMaterial();
        };
        Closed += (_, _) => {
            StopFullScreenTitleBarTimers();
        };
        PointerMoved += OnWindowPointerMoved;
        _hideTitleBarTimer.Tick += OnHideTitleBarTimerTick;
        _showTitleBarTimer.Tick += OnShowTitleBarTimerTick;
        ApplyChromeDefaults();
        ApplyDefaultTitle();
        SyncChromeSlots();
        UpdateWindowMaterial();
        SetWindowContent(_rootSurface);
        SetupDefaultNativeMenu();
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
        else if (change.Property == UseApplicationNameAsDefaultTitleProperty)
        {
            ApplyDefaultTitle();
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
        _titleBar.ShowMaximizeButton = ShowMaximizeButton && WindowState != WindowState.FullScreen;
        _titleBar.ShowCloseButton = ShowCloseButton;
        _titleBar.IsPinned = Topmost;
        _titleBar.IsFullScreen = WindowState == WindowState.FullScreen;
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
            double nativeHeight = WindowDecorationMargin.Top;
            if (nativeHeight > 0.0)
            {
                return nativeHeight;
            }
        }
        return 40.0;
    }

    private Thickness GetEffectiveLeftContentMargin()
    {
        return IsNativeSystemButtonAreaOnLeft ? new Thickness(GetEffectiveNativeSystemButtonReservedWidth() + 16.0, 0.0, 16.0, 0.0) : new Thickness(16.0, 0.0, 16.0, 0.0);
    }

    private double GetEffectiveNativeSystemButtonReservedWidth()
    {
        return Math.Max(Math.Max(0.0, NativeSystemButtonReservedWidth), Math.Max(0.0, WindowDecorationMargin.Left));
    }

    private void SyncChromeLayout()
    {
        if (WindowState == WindowState.FullScreen || WindowChromeMode == LuminaWindowChromeMode.Platform)
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
            return WindowState != WindowState.FullScreen;
        }
        if (WindowState != WindowState.FullScreen)
        {
            return true;
        }
        LuminaFullScreenTitleBarBehavior fullScreenTitleBarBehavior = FullScreenTitleBarBehavior;
        bool result = fullScreenTitleBarBehavior switch
        {
            LuminaFullScreenTitleBarBehavior.Visible => true, 
            LuminaFullScreenTitleBarBehavior.AutoHide => _fullScreenTitleBarVisible, 
            _ => false, 
        };
        return result;
    }

    private void UpdateWindowMaterial()
    {
        if (IsInternalChromeReady())
        {
            bool glassEnabled = (IsWindowGlassEnabled = UseWindowGlass && SupportsHardwareBlur(ActualTransparencyLevel));
            _rootSurface.Background = glassEnabled ? (GlassContentBackground ?? FindBrush("LuminaWindowGlassBackgroundBrush")) : (ContentBackground ?? FindBrush("LuminaBackgroundBrush"));
            LuminaTitleBar titleBar = _titleBar;
            object background;
            if (WindowState != WindowState.FullScreen)
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
        return this.FindResource(ActualThemeVariant, key) as IBrush;
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
                ExtendClientAreaToDecorationsHint = false;
                ExtendClientAreaTitleBarHeightHint = -1.0;
                WindowDecorations = WindowDecorations.Full;
                Background = ContentBackground ?? FindBrush("LuminaBackgroundBrush") ?? Brushes.Transparent;
                return;
            }
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = (WindowChromeMode == LuminaWindowChromeMode.Extended && UsesNativeExtendedSystemButtons()) ? (-1.0) : 40.0;
            TransparencyLevelHint = new[]
            {
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.Transparent
            };
            Background = Brushes.Transparent;
            WindowDecorations = (WindowChromeMode == LuminaWindowChromeMode.Extended) ? GetExtendedWindowDecorations() : WindowDecorations.None;
        }
    }

    private void ApplyDefaultTitle()
    {
        if (!UseApplicationNameAsDefaultTitle || !ShouldReplaceDefaultTitle(Title))
        {
            return;
        }

        var applicationName = ResolveApplicationDisplayName();
        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            Title = applicationName;
        }
    }

    private static bool ShouldReplaceDefaultTitle(string? title)
    {
        return string.IsNullOrWhiteSpace(title) || string.Equals(title, AvaloniaDefaultApplicationName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveApplicationDisplayName()
    {
        var applicationName = Application.Current?.Name;
        if (!ShouldReplaceDefaultTitle(applicationName))
        {
            return applicationName;
        }

        var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
        if (!string.IsNullOrWhiteSpace(entryAssemblyName))
        {
            return entryAssemblyName;
        }

        return Application.Current?.GetType().Assembly.GetName().Name;
    }

    private bool UsesLuminaSystemButtons()
    {
        return WindowChromeMode == LuminaWindowChromeMode.Lumina || (WindowChromeMode == LuminaWindowChromeMode.Extended && !UsesNativeExtendedSystemButtons());
    }

    private bool UsesNativeExtendedSystemButtons()
    {
        LuminaExtendedSystemButtonMode extendedSystemButtonMode = ExtendedSystemButtonMode;
        bool result = extendedSystemButtonMode switch
        {
            LuminaExtendedSystemButtonMode.Native => true, 
            LuminaExtendedSystemButtonMode.Lumina => false, 
            _ => RuntimeInformation.IsOSPlatform(OSPlatform.OSX), 
        };
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
        if (WindowState == WindowState.FullScreen)
        {
            _fullScreenTitleBarVisible = FullScreenTitleBarBehavior == LuminaFullScreenTitleBarBehavior.Visible;
        }
    }

    private void OnWindowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (WindowChromeMode != LuminaWindowChromeMode.Lumina || WindowState != WindowState.FullScreen || FullScreenTitleBarBehavior != LuminaFullScreenTitleBarBehavior.AutoHide || !ShowTitleBar)
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
            Size layoutSize = Bounds.Size;
            double width = (layoutSize.Width > 0.0) ? layoutSize.Width : ClientSize.Width;
            double height = (layoutSize.Height > 0.0) ? layoutSize.Height : ClientSize.Height;
            
            if (width > 0.0 && SizeToContent != SizeToContent.Width && SizeToContent != SizeToContent.WidthAndHeight)
            {
                _rootSurface.Width = width;
            }
            else
            {
                _rootSurface.Width = double.NaN;
            }
            
            if (height > 0.0 && SizeToContent != SizeToContent.Height && SizeToContent != SizeToContent.WidthAndHeight)
            {
                _rootSurface.Height = height;
            }
            else
            {
                _rootSurface.Height = double.NaN;
            }
        }
    }

    private void SetWindowContent(object? content)
    {
        _settingInternalContent = true;
        try
        {
            Content = content;
        }
        finally
        {
            _settingInternalContent = false;
        }
    }

    /// <summary>
    /// Determines whether the window can close.
    /// Override this method to show a confirmation dialog asynchronously before closing.
    /// </summary>
    /// <returns>A task that resolves to true if the window can close; otherwise, false.</returns>
    protected virtual async Task<bool> CanClose()
    {
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Handles the window closing event and allows asynchronous cancellation.
    /// </summary>
    /// <param name="e">The event arguments for the closing event.</param>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (!_canClose)
        {
            e.Cancel = true;
            _canClose = await CanClose();
            if (_canClose)
            {
                Close();
                return;
            }
        }
        base.OnClosing(e);
    }

    private static bool s_appMenuInitialized;
    private static NativeMenuItem? s_aboutMenuItem;

    private void SetupDefaultNativeMenu()
    {
        if (!OperatingSystem.IsMacOS() || Application.Current is null || s_appMenuInitialized)
        {
            return;
        }

        s_appMenuInitialized = true;

        // On macOS the "About" entry belongs to the application menu (the bold app-name
        // menu), which is an application-level menu rather than a per-window one. Avalonia
        // automatically appends the standard Hide/Services/Quit items after the entries
        // provided here. See AvaloniaNativeMenuExporter.CreateDefaultAppMenu for reference.
        s_aboutMenuItem = new NativeMenuItem();
        UpdateDefaultAboutHeader(null, EventArgs.Empty);
        s_aboutMenuItem.Click += OnSharedAboutRequested;
        LuminaLocalization.LanguageChanged += UpdateDefaultAboutHeader;

        var appMenu = new NativeMenu();
        appMenu.Add(s_aboutMenuItem);
        NativeMenu.SetMenu(Application.Current, appMenu);
    }

    private static void UpdateDefaultAboutHeader(object? sender, EventArgs e)
    {
        if (s_aboutMenuItem != null)
        {
            s_aboutMenuItem.Header = string.Format(
                LuminaLocalization.Get(LuminaLocalizationKeys.MenuAbout),
                ResolveApplicationDisplayName() ?? "Application");
        }
    }

    private static void OnSharedAboutRequested(object? sender, EventArgs e)
    {
        ResolveActiveLuminaWindow()?.ShowDefaultAboutDialog();
    }

    private static LuminaWindow? ResolveActiveLuminaWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window.IsActive && window is LuminaWindow active)
                {
                    return active;
                }
            }

            if (desktop.MainWindow is LuminaWindow main)
            {
                return main;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates the UI content for the default About dialog.
    /// Override this method to provide a custom UI layout instead of the default text.
    /// </summary>
    protected virtual object? CreateDefaultAboutDialogContent()
    {
        var appName = ResolveApplicationDisplayName() ?? "Application";
        var stack = new StackPanel { Spacing = 8, Margin = new Thickness(16) };

        stack.Children.Add(new TextBlock
        {
            Text = appName,
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Based on LuminaUI Framework",
            TextWrapping = TextWrapping.Wrap
        });

        return stack;
    }

    /// <summary>
    /// Shows the default About dialog for this window. Override to provide a completely custom dialog,
    /// or override <see cref="CreateDefaultAboutDialogContent"/> to only customize its content.
    /// </summary>
    protected virtual void ShowDefaultAboutDialog()
    {
        var appName = ResolveApplicationDisplayName() ?? "Application";

        var dialog = new LuminaWindowDialog
        {
            Title = string.Format(LuminaLocalization.Get(LuminaLocalizationKeys.MenuAbout), appName),
            Content = CreateDefaultAboutDialogContent(),
            Buttons = LuminaDialogButtons.Ok,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        _ = dialog.ShowDialog<LuminaDialogResult>(this);
    }
}
