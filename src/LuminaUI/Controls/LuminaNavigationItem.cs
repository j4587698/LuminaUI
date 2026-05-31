using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

[TemplatePart("PART_Header", typeof(Control))]
[TemplatePart("PART_ItemsContainer", typeof(Border))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class LuminaNavigationItem : ItemsControl
{
    private static readonly TimeSpan ExpansionDuration = TimeSpan.FromMilliseconds(150);

    private readonly DispatcherTimer _expansionTimer;

    private Control? _headerElement;

    private Border? _itemsContainer;

    private ItemsPresenter? _itemsPresenter;

    private DateTime _expansionStartedAt;

    private double _startHeight;

    private double _targetHeight;

    private double _startOpacity;

    private double _targetOpacity;

    private int _expansionStateVersion;

    private ScrollViewer? _lockedScrollViewer;

    private Vector _lockedScrollOffset;

    private bool _hasAppliedInitialExpansion;

    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<LuminaNavigationItem, object?>(nameof(Header));

    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<LuminaNavigationItem, object?>(nameof(Icon));

    public static readonly StyledProperty<string?> NavigationKeyProperty = AvaloniaProperty.Register<LuminaNavigationItem, string?>(nameof(NavigationKey));

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<LuminaNavigationItem, bool>(nameof(IsSelected), defaultValue: false, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<LuminaNavigationItem, bool>(nameof(IsExpanded), defaultValue: false, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<LuminaNavigationItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<LuminaNavigationItem, object?>(nameof(CommandParameter));

    public static readonly RoutedEvent<RoutedEventArgs> InvokedEvent = RoutedEvent.Register<LuminaNavigationItem, RoutedEventArgs>(nameof(Invoked), RoutingStrategies.Bubble);

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? NavigationKey
    {
        get => GetValue(NavigationKeyProperty);
        set => SetValue(NavigationKeyProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? Invoked
    {
        add
        {
            AddHandler(InvokedEvent, value);
        }
        remove
        {
            RemoveHandler(InvokedEvent, value);
        }
    }

    public LuminaNavigationItem()
    {
        _expansionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _expansionTimer.Tick += OnExpansionTimerTick;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_headerElement != null)
        {
            _headerElement.PointerEntered -= OnHeaderPointerEntered;
            _headerElement.PointerExited -= OnHeaderPointerExited;
            _headerElement.PointerReleased -= OnHeaderPointerReleased;
        }
        base.OnApplyTemplate(e);
        _headerElement = e.NameScope.FindRequired<Control>("PART_Header");
        _itemsContainer = e.NameScope.FindRequired<Border>("PART_ItemsContainer");
        _itemsPresenter = e.NameScope.FindRequired<ItemsPresenter>("PART_ItemsPresenter");
        if (_headerElement != null)
        {
            _headerElement.PointerEntered += OnHeaderPointerEntered;
            _headerElement.PointerExited += OnHeaderPointerExited;
            _headerElement.PointerReleased += OnHeaderPointerReleased;
        }
        PseudoClasses.Set(":headerpointerover", value: false);
        UpdatePseudoClasses();
        ApplyExpansionState(animated: false);
        int version = _expansionStateVersion;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            if (version == _expansionStateVersion)
            {
                ApplyExpansionState(animated: false);
            }
            _hasAppliedInitialExpansion = true;
        }, DispatcherPriority.Loaded);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty || change.Property == ItemsControl.ItemsSourceProperty)
        {
            UpdatePseudoClasses();
        }
        if (change.Property == IsExpandedProperty)
        {
            _expansionStateVersion++;
            UpdatePseudoClasses();
            ApplyExpansionState(_hasAppliedInitialExpansion);
        }
        else if (change.Property == ItemsControl.ItemsSourceProperty && IsExpanded)
        {
            QueueExpansionRefresh();
        }
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdatePseudoClasses();
        QueueExpansionRefresh();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _expansionTimer.Stop();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled)
        {
            if (e.Key == Key.Return || e.Key == Key.Space)
            {
                InvokeOrToggle();
                e.Handled = true;
            }
            else if (e.Key == Key.Right && HasNavigationChildren())
            {
                IsExpanded = true;
                e.Handled = true;
            }
            else if (e.Key == Key.Left && HasNavigationChildren())
            {
                IsExpanded = false;
                e.Handled = true;
            }
        }
    }

    private void OnHeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            InvokeOrToggle();
            e.Handled = true;
        }
    }

    private void OnHeaderPointerEntered(object? sender, PointerEventArgs e)
    {
        PseudoClasses.Set(":headerpointerover", value: true);
    }

    private void OnHeaderPointerExited(object? sender, PointerEventArgs e)
    {
        PseudoClasses.Set(":headerpointerover", value: false);
    }

    private void InvokeOrToggle()
    {
        if (HasNavigationChildren())
        {
            LockScrollOffset();
            IsExpanded = !IsExpanded;
            return;
        }
        object parameter = CommandParameter ?? this;
        ICommand? command = Command;
        if (command != null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
        RaiseEvent(new RoutedEventArgs(InvokedEvent, this));
    }

    public bool HasNavigationChildren()
    {
        return ItemsSource != null || Items.Count > 0;
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":selected", IsSelected);
        PseudoClasses.Set(":expanded", IsExpanded);
        PseudoClasses.Set(":hasitems", HasNavigationChildren());
    }

    private void QueueExpansionRefresh()
    {
        int version = _expansionStateVersion;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            if (version == _expansionStateVersion)
            {
                ApplyExpansionState(animated: false);
            }
        }, DispatcherPriority.Loaded);
    }

    private void ApplyExpansionState(bool animated)
    {
        if (_itemsContainer != null)
        {
            double targetHeight = IsExpanded ? MeasureItemsHeight() : 0.0;
            if (!animated)
            {
                _expansionTimer.Stop();
                _itemsContainer.IsVisible = IsExpanded;
                _itemsContainer.Height = targetHeight;
                _itemsContainer.Opacity = IsExpanded ? 1 : 0;
                return;
            }
            _expansionTimer.Stop();
            _itemsContainer.IsVisible = true;
            _startHeight = GetCurrentExpansionHeight();
            _targetHeight = targetHeight;
            _startOpacity = _itemsContainer.Opacity;
            _targetOpacity = IsExpanded ? 1 : 0;
            _expansionStartedAt = DateTime.UtcNow;
            _expansionTimer.Start();
            RestoreLockedScrollOffset();
        }
    }

    private double MeasureItemsHeight()
    {
        if (_itemsPresenter == null)
        {
            return 0.0;
        }
        double width = _itemsContainer?.Bounds.Width ?? Bounds.Width;
        if (width <= 0.0)
        {
            width = (Bounds.Width > 0.0) ? Bounds.Width : 220.0;
        }
        _itemsPresenter.Measure(new Size(width, double.PositiveInfinity));
        return Math.Ceiling(Math.Max(0.0, _itemsPresenter.DesiredSize.Height));
    }

    private double GetCurrentExpansionHeight()
    {
        if (_itemsContainer == null)
        {
            return 0.0;
        }
        if (!double.IsNaN(_itemsContainer.Height))
        {
            return Math.Max(0.0, _itemsContainer.Height);
        }
        return Math.Max(0.0, _itemsContainer.Bounds.Height);
    }

    private void OnExpansionTimerTick(object? sender, EventArgs e)
    {
        if (_itemsContainer == null)
        {
            _expansionTimer.Stop();
            return;
        }
        double progress = Math.Clamp((DateTime.UtcNow - _expansionStartedAt).TotalMilliseconds / ExpansionDuration.TotalMilliseconds, 0.0, 1.0);
        double eased = 1.0 - Math.Pow(1.0 - progress, 3.0);
        _itemsContainer.Height = Lerp(_startHeight, _targetHeight, eased);
        _itemsContainer.Opacity = Lerp(_startOpacity, _targetOpacity, eased);
        RestoreLockedScrollOffset();
        if (!(progress < 1.0))
        {
            _expansionTimer.Stop();
            _itemsContainer.Height = _targetHeight;
            _itemsContainer.Opacity = _targetOpacity;
            _itemsContainer.IsVisible = IsExpanded;
            ReleaseScrollOffsetLock();
        }
    }

    private void LockScrollOffset()
    {
        _lockedScrollViewer = this.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();
        if (_lockedScrollViewer != null)
        {
            _lockedScrollOffset = _lockedScrollViewer.Offset;
        }
    }

    private void RestoreLockedScrollOffset()
    {
        if (_lockedScrollViewer != null)
        {
            RestoreScrollOffset(_lockedScrollViewer, _lockedScrollOffset);
        }
    }

    private void ReleaseScrollOffsetLock()
    {
        ScrollViewer? scrollViewer = _lockedScrollViewer;
        Vector offset = _lockedScrollOffset;
        _lockedScrollViewer = null;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            RestoreScrollOffset(scrollViewer, offset);
        }, DispatcherPriority.Render);
    }

    private static void RestoreScrollOffset(ScrollViewer? scrollViewer, Vector offset)
    {
        if (scrollViewer != null)
        {
            double maxX = Math.Max(0.0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
            double maxY = Math.Max(0.0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
            scrollViewer.Offset = new Vector(Math.Min(offset.X, maxX), Math.Min(offset.Y, maxY));
        }
    }

    private static double Lerp(double start, double end, double amount)
    {
        return start + (end - start) * amount;
    }
}
