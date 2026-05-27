using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Input;
using Avalonia.Layout;

namespace LuminaUI.Controls;

public class LuminaVirtualizingWrapPanel : VirtualizingPanel
{
	private const double DefaultViewportHeight = 600.0;

	private static readonly object ItemIsOwnContainerKey = new object();

	private static readonly AttachedProperty<object?> RecycleKeyProperty = AvaloniaProperty.RegisterAttached<LuminaVirtualizingWrapPanel, Control, object?>("RecycleKey");

	private static readonly AttachedProperty<int> ElementIndexProperty = AvaloniaProperty.RegisterAttached<LuminaVirtualizingWrapPanel, Control, int>("ElementIndex", -1);

	private readonly Dictionary<int, Control> _realizedElements = new Dictionary<int, Control>();

	private readonly Dictionary<object, Stack<Control>> _recyclePool = new Dictionary<object, Stack<Control>>();

	private readonly List<double> _itemHeights = new List<double>();

	private readonly List<ItemLayout> _layouts = new List<ItemLayout>();

	private Rect _viewport;

	private Size _lastMeasureConstraint;

	private LayoutMetrics _lastMetrics = new LayoutMetrics(1, 1.0, 0.0, 0.0);

	private double _extentHeight;

	private int _realizingIndex = -1;

	private Control? _realizingElement;

	public static readonly StyledProperty<double> MinItemWidthProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, double>("MinItemWidth", 220.0);

	public static readonly StyledProperty<double> MaxItemWidthProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, double>("MaxItemWidth", double.PositiveInfinity);

	public static readonly StyledProperty<double> ItemWidthProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, double>("ItemWidth", double.NaN);

	public static readonly StyledProperty<double> EstimatedItemHeightProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, double>("EstimatedItemHeight", 160.0);

	public static readonly StyledProperty<double> HorizontalSpacingProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, double>("HorizontalSpacing", 12.0);

	public static readonly StyledProperty<double> VerticalSpacingProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, double>("VerticalSpacing", 12.0);

	public static readonly StyledProperty<int> MaxColumnsProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, int>("MaxColumns", int.MaxValue);

	public static readonly StyledProperty<double> CacheLengthProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, double>("CacheLength", 0.75);

	public static readonly StyledProperty<LuminaVirtualizingWrapLayoutMode> LayoutModeProperty = AvaloniaProperty.Register<LuminaVirtualizingWrapPanel, LuminaVirtualizingWrapLayoutMode>("LayoutMode", LuminaVirtualizingWrapLayoutMode.Wrap);

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

	public int FirstRealizedIndex => _realizedElements.Count == 0 ? -1 : _realizedElements.Keys.Min();

	public int LastRealizedIndex => _realizedElements.Count == 0 ? -1 : _realizedElements.Keys.Max();

	public LuminaVirtualizingWrapPanel()
	{
		EffectiveViewportChanged += OnEffectiveViewportChanged;
	}

	public Control? BringIndexIntoView(int index)
	{
		return ScrollIntoView(index);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var items = Items;
		var itemCount = items.Count;

		if (itemCount == 0)
		{
			ResetLayoutState();
			return default;
		}

		_lastMeasureConstraint = availableSize;
		EnsureItemHeightCache(itemCount);

		var availableWidth = ResolveAvailableWidth(availableSize);
		_lastMetrics = CalculateLayoutMetrics(availableWidth, itemCount);
		BuildLayouts(itemCount, _lastMetrics);

		var viewport = GetEffectiveViewport(availableSize, availableWidth);
		RealizeViewport(items, viewport, _lastMetrics);

		return new Size(availableWidth, _extentHeight);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (var pair in _realizedElements)
		{
			if (pair.Key >= 0 && pair.Key < _layouts.Count)
			{
				pair.Value.Arrange(_layouts[pair.Key].Bounds);
			}
		}

		return finalSize;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == MinItemWidthProperty ||
			change.Property == MaxItemWidthProperty ||
			change.Property == ItemWidthProperty ||
			change.Property == EstimatedItemHeightProperty ||
			change.Property == HorizontalSpacingProperty ||
			change.Property == VerticalSpacingProperty ||
			change.Property == MaxColumnsProperty ||
			change.Property == CacheLengthProperty ||
			change.Property == LayoutModeProperty)
		{
			if (change.Property == EstimatedItemHeightProperty || change.Property == LayoutModeProperty)
			{
				ResetHeightEstimates();
			}

			InvalidateMeasure();
		}
	}

	protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
	{
		base.OnItemsChanged(items, e);
		RecycleAllElements(clearItemHeightCache: true);
		InvalidateMeasure();
	}

	protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
	{
		var itemCount = Items.Count;
		if (itemCount == 0)
		{
			return null;
		}

		var fromIndex = from is Control control ? IndexFromContainer(control) : -1;
		var targetIndex = direction switch
		{
			NavigationDirection.First => 0,
			NavigationDirection.Last => itemCount - 1,
			NavigationDirection.Next => fromIndex + 1,
			NavigationDirection.Previous => fromIndex - 1,
			NavigationDirection.Left => fromIndex - 1,
			NavigationDirection.Right => fromIndex + 1,
			NavigationDirection.Up => fromIndex - Math.Max(1, _lastMetrics.Columns),
			NavigationDirection.Down => fromIndex + Math.Max(1, _lastMetrics.Columns),
			_ => fromIndex
		};

		if (wrap)
		{
			if (targetIndex < 0)
			{
				targetIndex = itemCount - 1;
			}
			else if (targetIndex >= itemCount)
			{
				targetIndex = 0;
			}
		}

		return targetIndex >= 0 && targetIndex < itemCount ? ScrollIntoView(targetIndex) : null;
	}

	protected override IEnumerable<Control> GetRealizedContainers()
	{
		return _realizedElements.OrderBy(pair => pair.Key).Select(pair => pair.Value);
	}

	protected override Control? ContainerFromIndex(int index)
	{
		if (_realizingIndex == index)
		{
			return _realizingElement;
		}

		return _realizedElements.TryGetValue(index, out var element) ? element : null;
	}

	protected override int IndexFromContainer(Control container)
	{
		return container.GetValue(ElementIndexProperty);
	}

	protected override Control? ScrollIntoView(int index)
	{
		if (index < 0 || index >= Items.Count)
		{
			return null;
		}

		if (_layouts.Count != Items.Count)
		{
			var availableWidth = ResolveAvailableWidth(_lastMeasureConstraint);
			_lastMetrics = CalculateLayoutMetrics(availableWidth, Items.Count);
			BuildLayouts(Items.Count, _lastMetrics);
		}

		if (!_realizedElements.TryGetValue(index, out var element))
		{
			element = GetOrCreateElement(Items, index);
		}

		element.Measure(new Size(_lastMetrics.ItemWidth, double.PositiveInfinity));
		UpdateMeasuredHeight(index, element.DesiredSize.Height);
		BuildLayouts(Items.Count, _lastMetrics);

		if (index < _layouts.Count)
		{
			element.Arrange(_layouts[index].Bounds);
		}

		element.BringIntoView();
		InvalidateMeasure();
		return element;
	}

	private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
	{
		var nextViewport = e.EffectiveViewport;
		if (!AreRectsClose(_viewport, nextViewport))
		{
			_viewport = nextViewport;
			InvalidateMeasure();
		}
	}

	private void RealizeViewport(IReadOnlyList<object?> items, Rect viewport, LayoutMetrics metrics)
	{
		var targetIndices = GetTargetIndices(viewport);
		RecycleElementsOutside(targetIndices);

		var heightsChanged = false;
		foreach (var index in targetIndices)
		{
			var element = GetOrCreateElement(items, index);
			element.Measure(new Size(metrics.ItemWidth, double.PositiveInfinity));
			heightsChanged |= UpdateMeasuredHeight(index, element.DesiredSize.Height);
		}

		if (!heightsChanged)
		{
			return;
		}

		BuildLayouts(items.Count, metrics);
		var updatedTargetIndices = GetTargetIndices(viewport);
		RecycleElementsOutside(updatedTargetIndices);

		foreach (var index in updatedTargetIndices)
		{
			if (!_realizedElements.ContainsKey(index))
			{
				var element = GetOrCreateElement(items, index);
				element.Measure(new Size(metrics.ItemWidth, double.PositiveInfinity));
				UpdateMeasuredHeight(index, element.DesiredSize.Height);
			}
		}
	}

	private HashSet<int> GetTargetIndices(Rect viewport)
	{
		var result = new HashSet<int>();
		for (var index = 0; index < _layouts.Count; index++)
		{
			if (_layouts[index].Bounds.Intersects(viewport))
			{
				result.Add(index);
			}
		}

		if (result.Count == 0 && _layouts.Count > 0)
		{
			result.Add(0);
		}

		return result;
	}

	private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
	{
		if (_realizedElements.TryGetValue(index, out var realizedElement))
		{
			return realizedElement;
		}

		_realizingIndex = index;
		try
		{
			var item = items[index];
			var generator = GetRequiredItemContainerGenerator();

			Control element;
			if (generator.NeedsContainer(item, index, out var recycleKey))
			{
				element = GetRecycledElement(item, index, recycleKey) ?? CreateElement(item, index, recycleKey);
			}
			else
			{
				element = GetItemAsOwnContainer(item, index);
			}

			element.SetValue(ElementIndexProperty, index);
			_realizedElements[index] = element;
			_realizingElement = element;
			return element;
		}
		finally
		{
			_realizingIndex = -1;
			_realizingElement = null;
		}
	}

	private Control CreateElement(object? item, int index, object? recycleKey)
	{
		var generator = GetRequiredItemContainerGenerator();
		var element = generator.CreateContainer(item, index, recycleKey);
		element.SetValue(RecycleKeyProperty, recycleKey);
		generator.PrepareItemContainer(element, item, index);
		AddInternalChild(element);
		generator.ItemContainerPrepared(element, item, index);
		return element;
	}

	private Control? GetRecycledElement(object? item, int index, object? recycleKey)
	{
		if (recycleKey == null || !_recyclePool.TryGetValue(recycleKey, out var pool) || pool.Count == 0)
		{
			return null;
		}

		var element = pool.Pop();
		element.IsVisible = true;
		var generator = GetRequiredItemContainerGenerator();
		generator.PrepareItemContainer(element, item, index);
		AddInternalChild(element);
		generator.ItemContainerPrepared(element, item, index);
		return element;
	}

	private Control GetItemAsOwnContainer(object? item, int index)
	{
		if (item is not Control control)
		{
			throw new InvalidOperationException("Items that do not need generated containers must be Avalonia controls.");
		}

		if (!control.IsSet(RecycleKeyProperty))
		{
			var generator = GetRequiredItemContainerGenerator();
			generator.PrepareItemContainer(control, control, index);
			AddInternalChild(control);
			control.SetValue(RecycleKeyProperty, ItemIsOwnContainerKey);
			generator.ItemContainerPrepared(control, item, index);
		}

		control.IsVisible = true;
		return control;
	}

	private void RecycleElementsOutside(HashSet<int> targetIndices)
	{
		var indexesToRecycle = _realizedElements.Keys.Where(index => !targetIndices.Contains(index)).ToArray();
		foreach (var index in indexesToRecycle)
		{
			RecycleElement(index);
		}
	}

	private void RecycleElement(int index)
	{
		if (!_realizedElements.Remove(index, out var element))
		{
			return;
		}

		var recycleKey = element.GetValue(RecycleKeyProperty);
		if (ReferenceEquals(recycleKey, ItemIsOwnContainerKey))
		{
			element.IsVisible = false;
			return;
		}

		GetRequiredItemContainerGenerator().ClearItemContainer(element);
		RemoveInternalChild(element);
		element.SetValue(ElementIndexProperty, -1);

		if (recycleKey != null)
		{
			if (!_recyclePool.TryGetValue(recycleKey, out var pool))
			{
				pool = new Stack<Control>();
				_recyclePool[recycleKey] = pool;
			}

			element.IsVisible = false;
			pool.Push(element);
		}
	}

	private ItemContainerGenerator GetRequiredItemContainerGenerator()
	{
		return ItemContainerGenerator ?? throw new InvalidOperationException("LuminaVirtualizingWrapPanel must be hosted by an ItemsControl before it can generate containers.");
	}

	private void RecycleAllElements(bool clearItemHeightCache)
	{
		foreach (var index in _realizedElements.Keys.ToArray())
		{
			RecycleElement(index);
		}

		if (clearItemHeightCache)
		{
			RemoveRemainingInternalChildren();
		}

		_recyclePool.Clear();
		_layouts.Clear();
		_extentHeight = 0.0;

		if (clearItemHeightCache)
		{
			_itemHeights.Clear();
		}
	}

	private void RemoveRemainingInternalChildren()
	{
		var children = Children.ToArray();
		foreach (var child in children)
		{
			if (!ReferenceEquals(child.GetValue(RecycleKeyProperty), ItemIsOwnContainerKey))
			{
				GetRequiredItemContainerGenerator().ClearItemContainer(child);
			}

			child.ClearValue(RecycleKeyProperty);
			child.SetValue(ElementIndexProperty, -1);
			RemoveInternalChild(child);
		}
	}

	private void ResetLayoutState()
	{
		RecycleAllElements(clearItemHeightCache: true);
		_viewport = default;
	}

	private void ResetHeightEstimates()
	{
		for (var index = 0; index < _itemHeights.Count; index++)
		{
			_itemHeights[index] = 0.0;
		}
	}

	private bool UpdateMeasuredHeight(int index, double measuredHeight)
	{
		if (index < 0 || index >= _itemHeights.Count)
		{
			return false;
		}

		var safeHeight = Math.Max(1.0, measuredHeight);
		if (Math.Abs(_itemHeights[index] - safeHeight) < 0.5)
		{
			return false;
		}

		_itemHeights[index] = safeHeight;
		return true;
	}

	private void EnsureItemHeightCache(int itemCount)
	{
		while (_itemHeights.Count < itemCount)
		{
			_itemHeights.Add(0.0);
		}

		while (_itemHeights.Count > itemCount)
		{
			_itemHeights.RemoveAt(_itemHeights.Count - 1);
		}
	}

	private void BuildLayouts(int itemCount, LayoutMetrics metrics)
	{
		_layouts.Clear();
		if (itemCount == 0)
		{
			_extentHeight = 0.0;
			return;
		}

		if (LayoutMode == LuminaVirtualizingWrapLayoutMode.Masonry)
		{
			BuildMasonryLayouts(itemCount, metrics);
		}
		else
		{
			BuildWrapLayouts(itemCount, metrics);
		}
	}

	private void BuildWrapLayouts(int itemCount, LayoutMetrics metrics)
	{
		var estimatedHeight = GetEstimatedItemHeight();
		var rowHeights = new List<double>();

		for (var rowStartIndex = 0; rowStartIndex < itemCount; rowStartIndex += metrics.Columns)
		{
			var itemsInRow = Math.Min(metrics.Columns, itemCount - rowStartIndex);
			var rowHeight = estimatedHeight;

			for (var column = 0; column < itemsInRow; column++)
			{
				rowHeight = Math.Max(rowHeight, GetCachedItemHeight(rowStartIndex + column, estimatedHeight));
			}

			rowHeights.Add(rowHeight);
		}

		var y = 0.0;
		for (var row = 0; row < rowHeights.Count; row++)
		{
			var rowHeight = rowHeights[row];
			var rowStartIndex = row * metrics.Columns;
			var itemsInRow = Math.Min(metrics.Columns, itemCount - rowStartIndex);

			for (var column = 0; column < itemsInRow; column++)
			{
				var index = rowStartIndex + column;
				var x = column * (metrics.ItemWidth + metrics.HorizontalSpacing);
				_layouts.Add(new ItemLayout(new Rect(x, y, metrics.ItemWidth, rowHeight)));
			}

			y += rowHeight + metrics.VerticalSpacing;
		}

		_extentHeight = Math.Max(0.0, y - metrics.VerticalSpacing);
	}

	private void BuildMasonryLayouts(int itemCount, LayoutMetrics metrics)
	{
		var estimatedHeight = GetEstimatedItemHeight();
		var columnHeights = new double[metrics.Columns];

		for (var index = 0; index < itemCount; index++)
		{
			var column = GetShortestColumn(columnHeights);
			var itemHeight = GetCachedItemHeight(index, estimatedHeight);
			var x = column * (metrics.ItemWidth + metrics.HorizontalSpacing);
			var y = columnHeights[column];

			_layouts.Add(new ItemLayout(new Rect(x, y, metrics.ItemWidth, itemHeight)));
			columnHeights[column] += itemHeight + metrics.VerticalSpacing;
		}

		_extentHeight = Math.Max(0.0, columnHeights.Max() - metrics.VerticalSpacing);
	}

	private double ResolveAvailableWidth(Size availableSize)
	{
		if (availableSize.Width > 0.0 && !double.IsInfinity(availableSize.Width))
		{
			return availableSize.Width;
		}

		if (_viewport.Width > 0.0 && !double.IsInfinity(_viewport.Width))
		{
			return _viewport.Width;
		}

		return Math.Max(1.0, GetResolvedItemWidth());
	}

	private Rect GetEffectiveViewport(Size availableSize, double availableWidth)
	{
		var viewport = _viewport;
		if (viewport.Width <= 0.0 || viewport.Height <= 0.0)
		{
			var viewportHeight = availableSize.Height > 0.0 && !double.IsInfinity(availableSize.Height) ? availableSize.Height : DefaultViewportHeight;
			viewport = new Rect(0.0, 0.0, availableWidth, viewportHeight);
		}

		var cacheHeight = Math.Max(0.0, viewport.Height * CacheLength);
		var top = Math.Max(0.0, viewport.Top - cacheHeight);
		var bottom = Math.Min(Math.Max(_extentHeight, viewport.Bottom + cacheHeight), viewport.Bottom + cacheHeight);
		return new Rect(0.0, top, Math.Max(availableWidth, viewport.Width), Math.Max(0.0, bottom - top));
	}

	private LayoutMetrics CalculateLayoutMetrics(double availableWidth, int itemCount)
	{
		var horizontalSpacing = Math.Max(0.0, HorizontalSpacing);
		var verticalSpacing = Math.Max(0.0, VerticalSpacing);
		var maxColumns = MaxColumns <= 0 ? int.MaxValue : MaxColumns;
		var fixedItemWidth = GetResolvedItemWidth();

		if (!double.IsNaN(ItemWidth) && ItemWidth > 0.0)
		{
			var fixedColumns = Math.Max(1, (int)Math.Floor((availableWidth + horizontalSpacing) / (fixedItemWidth + horizontalSpacing)));
			return new LayoutMetrics(Math.Max(1, Math.Min(Math.Min(fixedColumns, itemCount), maxColumns)), fixedItemWidth, horizontalSpacing, verticalSpacing);
		}

		var minItemWidth = Math.Max(1.0, MinItemWidth);
		var availableColumns = Math.Max(1, (int)Math.Floor((availableWidth + horizontalSpacing) / (minItemWidth + horizontalSpacing)));
		var columns = Math.Max(1, Math.Min(Math.Min(availableColumns, itemCount), maxColumns));
		var rawItemWidth = (availableWidth - (columns - 1) * horizontalSpacing) / columns;
		var itemWidth = Math.Min(Math.Max(1.0, rawItemWidth), GetMaxItemWidth());

		return new LayoutMetrics(columns, itemWidth, horizontalSpacing, verticalSpacing);
	}

	private double GetResolvedItemWidth()
	{
		if (!double.IsNaN(ItemWidth) && ItemWidth > 0.0)
		{
			return Math.Min(ItemWidth, GetMaxItemWidth());
		}

		return Math.Min(Math.Max(1.0, MinItemWidth), GetMaxItemWidth());
	}

	private double GetMaxItemWidth()
	{
		return MaxItemWidth > 0.0 ? MaxItemWidth : double.PositiveInfinity;
	}

	private double GetCachedItemHeight(int index, double estimatedHeight)
	{
		return index >= 0 && index < _itemHeights.Count && _itemHeights[index] > 0.0 ? _itemHeights[index] : estimatedHeight;
	}

	private double GetEstimatedItemHeight()
	{
		return Math.Max(1.0, EstimatedItemHeight);
	}

	private static int GetShortestColumn(IReadOnlyList<double> columnHeights)
	{
		var shortestColumn = 0;
		var shortestHeight = columnHeights[0];

		for (var column = 1; column < columnHeights.Count; column++)
		{
			if (columnHeights[column] < shortestHeight)
			{
				shortestColumn = column;
				shortestHeight = columnHeights[column];
			}
		}

		return shortestColumn;
	}

	private static bool AreRectsClose(Rect first, Rect second)
	{
		return Math.Abs(first.X - second.X) < 0.5 &&
			Math.Abs(first.Y - second.Y) < 0.5 &&
			Math.Abs(first.Width - second.Width) < 0.5 &&
			Math.Abs(first.Height - second.Height) < 0.5;
	}

	private readonly record struct LayoutMetrics(int Columns, double ItemWidth, double HorizontalSpacing, double VerticalSpacing);

	private readonly record struct ItemLayout(Rect Bounds);
}