using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace LuminaUI.Controls;

public class LuminaPlatformMenu : Menu
{
    private sealed class NativeMenuItemPresenter : MenuItem
    {
        protected override Type StyleKeyOverride => typeof(MenuItem);

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return CreateContainerForNativeItem(item) ?? base.CreateContainerForItemOverride(item, index, recycleKey);
        }
    }

    private sealed class NativeMenuExportObserver(LuminaPlatformMenu owner) : IObserver<bool>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(bool value)
        {
            owner.RefreshNativeMenuExportState();
        }
    }

    private Window? _nativeMenuWindow;

    private IDisposable? _nativeMenuExportedSubscription;

    private Window? _appliedNativeMenuWindow;

    private NativeMenu? _appliedNativeMenu;

    private bool _isNativeMenuExported;

    public static readonly StyledProperty<NativeMenu?> NativeMenuProperty = AvaloniaProperty.Register<LuminaPlatformMenu, NativeMenu?>(nameof(NativeMenu));

    public static readonly StyledProperty<LuminaNativeMenuMode> NativeMenuModeProperty = AvaloniaProperty.Register<LuminaPlatformMenu, LuminaNativeMenuMode>(nameof(NativeMenuMode), LuminaNativeMenuMode.MacOnly);

    public static readonly StyledProperty<bool> HideVisualMenuWhenNativeProperty = AvaloniaProperty.Register<LuminaPlatformMenu, bool>(nameof(HideVisualMenuWhenNative), defaultValue: true);

    public static readonly DirectProperty<LuminaPlatformMenu, bool> IsNativeMenuExportedProperty = AvaloniaProperty.RegisterDirect<LuminaPlatformMenu, bool>(nameof(IsNativeMenuExported), (LuminaPlatformMenu menu) => menu.IsNativeMenuExported, null, unsetValue: false);

    public NativeMenu? NativeMenu
    {
        get => GetValue(NativeMenuProperty);
        set => SetValue(NativeMenuProperty, value);
    }

    public LuminaNativeMenuMode NativeMenuMode
    {
        get => GetValue(NativeMenuModeProperty);
        set => SetValue(NativeMenuModeProperty, value);
    }

    public bool HideVisualMenuWhenNative
    {
        get => GetValue(HideVisualMenuWhenNativeProperty);
        set => SetValue(HideVisualMenuWhenNativeProperty, value);
    }

    public bool IsNativeMenuExported
    {
        get
        {
            return _isNativeMenuExported;
        }
        private set
        {
            SetAndRaise(IsNativeMenuExportedProperty, ref _isNativeMenuExported, value);
        }
    }

    protected override Type StyleKeyOverride => typeof(Menu);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateNativeMenu();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClearNativeMenu();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NativeMenuProperty)
        {
            UpdateVisualMenuSource();
            UpdateNativeMenu();
        }
        else if (change.Property == NativeMenuModeProperty || change.Property == HideVisualMenuWhenNativeProperty)
        {
            UpdateNativeMenu();
        }
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return CreateContainerForNativeItem(item) ?? base.CreateContainerForItemOverride(item, index, recycleKey);
    }

    private static Control? CreateContainerForNativeItem(object? item)
    {
        if (item is NativeMenuItemSeparator)
        {
            return new Separator();
        }

        if (item is not NativeMenuItem nativeItem)
        {
            return null;
        }

        var menuItem = new NativeMenuItemPresenter
        {
            ItemsSource = nativeItem.Menu?.Items,
            [!HeaderedSelectingItemsControl.HeaderProperty] = nativeItem.GetObservable(NativeMenuItem.HeaderProperty)
                .ToBinding(),
            Icon = nativeItem.Icon is null ? null : new Image { Source = nativeItem.Icon },
            [!!MenuItem.IsCheckedProperty] = nativeItem[!!NativeMenuItem.IsCheckedProperty],
            [!MenuItem.IsEnabledProperty] = nativeItem.GetObservable(NativeMenuItem.IsEnabledProperty).ToBinding(),
            [!MenuItem.IsVisibleProperty] = nativeItem.GetObservable(NativeMenuItem.IsVisibleProperty).ToBinding(),
            [!MenuItem.CommandProperty] = nativeItem.GetObservable(NativeMenuItem.CommandProperty).ToBinding(),
            [!MenuItem.CommandParameterProperty] = nativeItem.GetObservable(NativeMenuItem.CommandParameterProperty).ToBinding(),
            [!MenuItem.InputGestureProperty] = nativeItem.GetObservable(NativeMenuItem.GestureProperty).ToBinding(),
            [!MenuItem.ToggleTypeProperty] = nativeItem.GetObservable(NativeMenuItem.ToggleTypeProperty).ToBinding(),
            [!ToolTip.TipProperty] = nativeItem.GetObservable(NativeMenuItem.ToolTipProperty).ToBinding(),
        };
        menuItem.Click += NativeMenuItemPresenterOnClick;
        return menuItem;
    }

    private static void NativeMenuItemPresenterOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is NativeMenuItemPresenter { DataContext: NativeMenuItem item } &&
            item.HasClickHandlers &&
            item is INativeMenuItemExporterEventsImplBridge bridge)
        {
            bridge.RaiseClicked();
        }
    }

    private void UpdateVisualMenuSource()
    {
        ItemsSource = NativeMenu?.Items;
    }

    private void UpdateNativeMenu()
    {
        UpdateVisualMenuSource();
        bool shouldAttemptNative = ShouldAttemptNativeMenu();
        Window? window = shouldAttemptNative ? TopLevel.GetTopLevel(this) as Window : null;
        if (_nativeMenuWindow != window)
        {
            ClearNativeMenu();
            _nativeMenuWindow = window;
            if (_nativeMenuWindow != null)
            {
                _nativeMenuExportedSubscription = _nativeMenuWindow.GetObservable(Avalonia.Controls.NativeMenu.IsNativeMenuExportedProperty).Subscribe(new NativeMenuExportObserver(this));
            }
        }
        if (_nativeMenuWindow != null &&
            NativeMenu != null &&
            (!ReferenceEquals(_appliedNativeMenuWindow, _nativeMenuWindow) || !ReferenceEquals(_appliedNativeMenu, NativeMenu)))
        {
            Avalonia.Controls.NativeMenu.SetMenu(_nativeMenuWindow, NativeMenu);
            _appliedNativeMenuWindow = _nativeMenuWindow;
            _appliedNativeMenu = NativeMenu;
        }
        RefreshNativeMenuExportState(shouldAttemptNative);
    }

    private void ClearNativeMenu()
    {
        _nativeMenuExportedSubscription?.Dispose();
        _nativeMenuExportedSubscription = null;
        _nativeMenuWindow = null;
        _appliedNativeMenuWindow = null;
        _appliedNativeMenu = null;
        IsNativeMenuExported = false;
        IsVisible = true;
    }

    private bool ShouldAttemptNativeMenu()
    {
        if (NativeMenu == null)
        {
            return false;
        }
        LuminaNativeMenuMode nativeMenuMode = NativeMenuMode;
        return nativeMenuMode switch
        {
            LuminaNativeMenuMode.Never => false, 
            LuminaNativeMenuMode.MacOnly => OperatingSystem.IsMacOS(), 
            _ => OperatingSystem.IsMacOS() || OperatingSystem.IsLinux(), 
        };
    }

    private void RefreshNativeMenuExportState()
    {
        RefreshNativeMenuExportState(ShouldAttemptNativeMenu());
    }

    private void RefreshNativeMenuExportState(bool shouldAttemptNative)
    {
        bool isExported = (IsNativeMenuExported = shouldAttemptNative && _nativeMenuWindow != null && Avalonia.Controls.NativeMenu.GetIsNativeMenuExported(_nativeMenuWindow));
        IsVisible = !HideVisualMenuWhenNative || !isExported;
    }
}
