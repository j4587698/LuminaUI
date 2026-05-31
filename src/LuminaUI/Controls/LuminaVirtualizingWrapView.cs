using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

public class LuminaVirtualizingWrapView : ItemsControl
{
    private readonly FuncTemplate<Panel?> _itemsPanelTemplate;

    private LuminaVirtualizingWrapPanel? _itemsPanel;

    public static readonly StyledProperty<double> MinItemWidthProperty = LuminaVirtualizingWrapPanel.MinItemWidthProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<double> MaxItemWidthProperty = LuminaVirtualizingWrapPanel.MaxItemWidthProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<double> ItemWidthProperty = LuminaVirtualizingWrapPanel.ItemWidthProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<double> EstimatedItemHeightProperty = LuminaVirtualizingWrapPanel.EstimatedItemHeightProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<double> HorizontalSpacingProperty = LuminaVirtualizingWrapPanel.HorizontalSpacingProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<double> VerticalSpacingProperty = LuminaVirtualizingWrapPanel.VerticalSpacingProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<int> MaxColumnsProperty = LuminaVirtualizingWrapPanel.MaxColumnsProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<double> CacheLengthProperty = LuminaVirtualizingWrapPanel.CacheLengthProperty.AddOwner<LuminaVirtualizingWrapView>();

    public static readonly StyledProperty<LuminaVirtualizingWrapLayoutMode> LayoutModeProperty = LuminaVirtualizingWrapPanel.LayoutModeProperty.AddOwner<LuminaVirtualizingWrapView>();

    protected override Type StyleKeyOverride => typeof(ItemsControl);

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double MaxItemWidth
    {
        get => GetValue(MaxItemWidthProperty);
        set => SetValue(MaxItemWidthProperty, value);
    }

    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double EstimatedItemHeight
    {
        get => GetValue(EstimatedItemHeightProperty);
        set => SetValue(EstimatedItemHeightProperty, value);
    }

    public double HorizontalSpacing
    {
        get => GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    public int MaxColumns
    {
        get => GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    public double CacheLength
    {
        get => GetValue(CacheLengthProperty);
        set => SetValue(CacheLengthProperty, value);
    }

    public LuminaVirtualizingWrapLayoutMode LayoutMode
    {
        get => GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
    }

    public int FirstRealizedIndex => _itemsPanel?.FirstRealizedIndex ?? -1;

    public int LastRealizedIndex => _itemsPanel?.LastRealizedIndex ?? -1;

    public LuminaVirtualizingWrapView()
    {
        _itemsPanelTemplate = new FuncTemplate<Panel?>(CreateItemsPanel);
        SetCurrentValue(ItemsPanelProperty, _itemsPanelTemplate);
    }

    public Control? BringIndexIntoView(int index)
    {
        return _itemsPanel?.BringIndexIntoView(index);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (IsLayoutProperty(change.Property))
        {
            SyncItemsPanelProperties();
        }
    }

    private Panel CreateItemsPanel()
    {
        _itemsPanel = new LuminaVirtualizingWrapPanel();
        SyncItemsPanelProperties();
        return _itemsPanel;
    }

    private void SyncItemsPanelProperties()
    {
        if (_itemsPanel == null)
        {
            return;
        }

        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.MinItemWidthProperty, MinItemWidth);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.MaxItemWidthProperty, MaxItemWidth);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.ItemWidthProperty, ItemWidth);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.EstimatedItemHeightProperty, EstimatedItemHeight);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.HorizontalSpacingProperty, HorizontalSpacing);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.VerticalSpacingProperty, VerticalSpacing);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.MaxColumnsProperty, MaxColumns);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.CacheLengthProperty, CacheLength);
        _itemsPanel.SetCurrentValue(LuminaVirtualizingWrapPanel.LayoutModeProperty, LayoutMode);
    }

    private static bool IsLayoutProperty(AvaloniaProperty property)
    {
        return property == MinItemWidthProperty ||
            property == MaxItemWidthProperty ||
            property == ItemWidthProperty ||
            property == EstimatedItemHeightProperty ||
            property == HorizontalSpacingProperty ||
            property == VerticalSpacingProperty ||
            property == MaxColumnsProperty ||
            property == CacheLengthProperty ||
            property == LayoutModeProperty;
    }
}