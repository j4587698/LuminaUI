using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace LuminaUI.Controls;

public class LuminaNavigationView : ItemsControl
{
    private bool _updatingSelection;

    public static readonly StyledProperty<object?> SelectedItemProperty = AvaloniaProperty.Register<LuminaNavigationView, object?>(nameof(SelectedItem), null, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<string?> SelectedKeyProperty = AvaloniaProperty.Register<LuminaNavigationView, string?>(nameof(SelectedKey), null, inherits: false, BindingMode.TwoWay);

    protected override Type StyleKeyOverride => typeof(ItemsControl);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string? SelectedKey
    {
        get => GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? SelectionChanged;

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        AddHandler(LuminaNavigationItem.InvokedEvent, OnItemInvoked, RoutingStrategies.Bubble);
        Avalonia.Threading.Dispatcher.UIThread.Post(ApplySelectedKey, DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        RemoveHandler(LuminaNavigationItem.InvokedEvent, OnItemInvoked);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!_updatingSelection)
        {
            if (change.Property == SelectedKeyProperty)
            {
                ApplySelectedKey();
            }
            else if (change.Property == SelectedItemProperty)
            {
                SelectItem(change.GetNewValue<object>() as LuminaNavigationItem, updateKey: true, raiseSelectionChanged: false);
            }
            else if (change.Property == ItemsControl.ItemsSourceProperty)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(ApplySelectedKey, DispatcherPriority.Loaded);
            }
        }
    }

    private void OnItemInvoked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is LuminaNavigationItem item && !item.HasNavigationChildren())
        {
            SelectItem(item, updateKey: true, raiseSelectionChanged: true, e);
        }
    }

    public void SelectByKey(string? selectedKey)
    {
        SelectedKey = selectedKey;
    }

    private void ApplySelectedKey()
    {
        if (!_updatingSelection)
        {
            string? selectedKey = SelectedKey;
            LuminaNavigationItem? item;
            IReadOnlyList<LuminaNavigationItem> ancestors;
            if (string.IsNullOrWhiteSpace(selectedKey))
            {
                SelectItem(null, updateKey: false, raiseSelectionChanged: false);
            }
            else if (TryFindItemByKey(this, selectedKey, out item, out ancestors))
            {
                SelectItem(item, updateKey: false, raiseSelectionChanged: false);
                ExpandAncestors(ancestors);
            }
            else
            {
                SelectItem(null, updateKey: false, raiseSelectionChanged: false);
            }
        }
    }

    private void SelectItem(LuminaNavigationItem? item, bool updateKey, bool raiseSelectionChanged, RoutedEventArgs? eventArgs = null)
    {
        _updatingSelection = true;
        try
        {
            foreach (LuminaNavigationItem navigationItem in EnumerateNavigationItems(this))
            {
                navigationItem.IsSelected = navigationItem == item;
            }
            SelectedItem = item;
            if (updateKey)
            {
                SelectedKey = (item == null) ? null : GetItemKey(item);
            }
        }
        finally
        {
            _updatingSelection = false;
        }
        if (raiseSelectionChanged)
        {
            SelectionChanged?.Invoke(this, eventArgs ?? new RoutedEventArgs());
        }
    }

    private static void ExpandAncestors(IEnumerable<LuminaNavigationItem> ancestors)
    {
        foreach (LuminaNavigationItem ancestor in ancestors)
        {
            ancestor.IsExpanded = true;
        }
    }

    private static string? GetItemKey(LuminaNavigationItem item)
    {
        return string.IsNullOrWhiteSpace(item.NavigationKey) ? item.Name : item.NavigationKey;
    }

    private static bool TryFindItemByKey(ItemsControl owner, string selectedKey, [NotNullWhen(true)] out LuminaNavigationItem? item, out IReadOnlyList<LuminaNavigationItem> ancestors)
    {
        List<LuminaNavigationItem> stack = new List<LuminaNavigationItem>();
        item = FindItemByKey(owner, selectedKey, stack);
        ancestors = stack;
        return item != null;
    }

    private static LuminaNavigationItem? FindItemByKey(ItemsControl owner, string selectedKey, List<LuminaNavigationItem> ancestors)
    {
        foreach (LuminaNavigationItem item in EnumerateDirectNavigationItems(owner))
        {
            if (string.Equals(GetItemKey(item), selectedKey, StringComparison.Ordinal))
            {
                return item;
            }
            ancestors.Add(item);
            LuminaNavigationItem? child = FindItemByKey(item, selectedKey, ancestors);
            if (child != null)
            {
                return child;
            }
            ancestors.RemoveAt(ancestors.Count - 1);
        }
        return null;
    }

    private static IEnumerable<LuminaNavigationItem> EnumerateNavigationItems(ItemsControl owner)
    {
        foreach (LuminaNavigationItem item in EnumerateDirectNavigationItems(owner))
        {
            yield return item;
            foreach (LuminaNavigationItem descendant in EnumerateNavigationItems(item))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<LuminaNavigationItem> EnumerateDirectNavigationItems(ItemsControl owner)
    {
        foreach (object? item in EnumerateItems(owner.Items))
        {
            if (item is LuminaNavigationItem navigationItem)
            {
                yield return navigationItem;
            }
        }
        if (owner.ItemsSource == null)
        {
            yield break;
        }
        foreach (object? item in EnumerateItems(owner.ItemsSource))
        {
            if (item is LuminaNavigationItem navigationItem)
            {
                yield return navigationItem;
            }
        }
    }

    private static IEnumerable<object?> EnumerateItems(IEnumerable source)
    {
        foreach (object item in source)
        {
            yield return item;
        }
    }
}
