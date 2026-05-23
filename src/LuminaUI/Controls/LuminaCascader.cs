using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Controls;

public class LuminaCascader : TemplatedControl
{
	private Button? _surfaceButton;

	private Button? _clearButton;

	private Popup? _popup;

	private ItemsControl? _panelsHost;

	private ItemsControl? _sheetPanelsHost;

	private TopLevel? _wheelBlockerTopLevel;

	private readonly List<(ScrollViewer Viewer, Vector Offset)> _lockedScrollViewers = new List<(ScrollViewer, Vector)>();

	private bool _isRestoringScrollOffset;

	private bool _isUpdatingSelectedValue;

	private bool _isSheetOpen;

	private CancellationTokenSource? _hoverExpandCancellation;

	private ContentControl? _sheetHeaderHost;

	private readonly List<LuminaCascaderNode> _activePath = new List<LuminaCascaderNode>();

	private readonly List<LuminaCascaderNode> _selectedPath = new List<LuminaCascaderNode>();

	private readonly AvaloniaList<LuminaCascaderNode> _roots = new AvaloniaList<LuminaCascaderNode>();

	private AvaloniaList<LuminaCascaderPanel> _panels = new AvaloniaList<LuminaCascaderPanel>();

	public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty;

	public static readonly StyledProperty<object?> SelectedValueProperty;

	public static readonly StyledProperty<string?> WatermarkProperty;

	public static readonly StyledProperty<string> SeparatorProperty;

	public static readonly StyledProperty<bool> IsDropDownOpenProperty;

	public static readonly StyledProperty<double> MaxDropDownHeightProperty;

	public static readonly StyledProperty<double> PanelWidthProperty;

	public static readonly StyledProperty<bool> CanClearProperty;

	public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty;

	public static readonly StyledProperty<bool> ExpandOnHoverProperty;

	public static readonly StyledProperty<bool> CanSelectIntermediateNodesProperty;

	public static readonly StyledProperty<Func<LuminaCascaderNode, Task<IEnumerable<LuminaCascaderNode>>>?> LoadChildrenAsyncProperty;

	public static readonly DirectProperty<LuminaCascader, bool> HasSelectionProperty;

	private bool _hasSelection;

	public static readonly DirectProperty<LuminaCascader, string> DisplayTextProperty;

	private string _displayText = string.Empty;

	public static readonly DirectProperty<LuminaCascader, AvaloniaList<LuminaCascaderPanel>> PanelsProperty;

	public static readonly DirectProperty<LuminaCascader, bool> ShowClearButtonProperty;

	private bool _showClearButton;

	public IEnumerable? ItemsSource
	{
		get
		{
			return GetValue(ItemsSourceProperty);
		}
		set
		{
			SetValue(ItemsSourceProperty, value);
		}
	}

	public object? SelectedValue
	{
		get
		{
			return GetValue(SelectedValueProperty);
		}
		set
		{
			SetValue(SelectedValueProperty, value);
		}
	}

	public string? Watermark
	{
		get
		{
			return GetValue(WatermarkProperty);
		}
		set
		{
			SetValue(WatermarkProperty, value);
		}
	}

	public string Separator
	{
		get
		{
			return GetValue(SeparatorProperty);
		}
		set
		{
			SetValue(SeparatorProperty, value);
		}
	}

	public bool IsDropDownOpen
	{
		get
		{
			return GetValue(IsDropDownOpenProperty);
		}
		set
		{
			SetValue(IsDropDownOpenProperty, value);
		}
	}

	public double MaxDropDownHeight
	{
		get
		{
			return GetValue(MaxDropDownHeightProperty);
		}
		set
		{
			SetValue(MaxDropDownHeightProperty, value);
		}
	}

	public double PanelWidth
	{
		get
		{
			return GetValue(PanelWidthProperty);
		}
		set
		{
			SetValue(PanelWidthProperty, value);
		}
	}

	public bool CanClear
	{
		get
		{
			return GetValue(CanClearProperty);
		}
		set
		{
			SetValue(CanClearProperty, value);
		}
	}

	public ICommand? SelectionChangedCommand
	{
		get
		{
			return GetValue(SelectionChangedCommandProperty);
		}
		set
		{
			SetValue(SelectionChangedCommandProperty, value);
		}
	}

	public bool ExpandOnHover
	{
		get
		{
			return GetValue(ExpandOnHoverProperty);
		}
		set
		{
			SetValue(ExpandOnHoverProperty, value);
		}
	}

	public bool CanSelectIntermediateNodes
	{
		get
		{
			return GetValue(CanSelectIntermediateNodesProperty);
		}
		set
		{
			SetValue(CanSelectIntermediateNodesProperty, value);
		}
	}

	public Func<LuminaCascaderNode, Task<IEnumerable<LuminaCascaderNode>>>? LoadChildrenAsync
	{
		get
		{
			return GetValue(LoadChildrenAsyncProperty);
		}
		set
		{
			SetValue(LoadChildrenAsyncProperty, value);
		}
	}

	public bool HasSelection
	{
		get
		{
			return _hasSelection;
		}
		private set
		{
			SetAndRaise(HasSelectionProperty, ref _hasSelection, value);
		}
	}

	public string DisplayText
	{
		get
		{
			return _displayText;
		}
		private set
		{
			SetAndRaise(DisplayTextProperty, ref _displayText, value);
		}
	}

	public AvaloniaList<LuminaCascaderPanel> Panels
	{
		get
		{
			return _panels;
		}
		private set
		{
			SetAndRaise(PanelsProperty, ref _panels, value);
		}
	}

	public bool ShowClearButton
	{
		get
		{
			return _showClearButton;
		}
		private set
		{
			SetAndRaise(ShowClearButtonProperty, ref _showClearButton, value);
		}
	}

	static LuminaCascader()
	{
		ItemsSourceProperty = AvaloniaProperty.Register<LuminaCascader, IEnumerable?>("ItemsSource");
		SelectedValueProperty = AvaloniaProperty.Register<LuminaCascader, object?>("SelectedValue", null, inherits: false, BindingMode.TwoWay);
		WatermarkProperty = AvaloniaProperty.Register<LuminaCascader, string?>("Watermark");
		SeparatorProperty = AvaloniaProperty.Register<LuminaCascader, string>("Separator", " / ");
		IsDropDownOpenProperty = AvaloniaProperty.Register<LuminaCascader, bool>("IsDropDownOpen", defaultValue: false, inherits: false, BindingMode.TwoWay);
		MaxDropDownHeightProperty = AvaloniaProperty.Register<LuminaCascader, double>("MaxDropDownHeight", 260.0);
		PanelWidthProperty = AvaloniaProperty.Register<LuminaCascader, double>("PanelWidth", 140.0);
		CanClearProperty = AvaloniaProperty.Register<LuminaCascader, bool>("CanClear", defaultValue: true);
		SelectionChangedCommandProperty = AvaloniaProperty.Register<LuminaCascader, ICommand?>("SelectionChangedCommand");
		ExpandOnHoverProperty = AvaloniaProperty.Register<LuminaCascader, bool>("ExpandOnHover", defaultValue: true);
		CanSelectIntermediateNodesProperty = AvaloniaProperty.Register<LuminaCascader, bool>("CanSelectIntermediateNodes", defaultValue: false);
		LoadChildrenAsyncProperty = AvaloniaProperty.Register<LuminaCascader, Func<LuminaCascaderNode, Task<IEnumerable<LuminaCascaderNode>>>?>("LoadChildrenAsync");
		HasSelectionProperty = AvaloniaProperty.RegisterDirect<LuminaCascader, bool>("HasSelection", (LuminaCascader c) => c.HasSelection, null, unsetValue: false);
		DisplayTextProperty = AvaloniaProperty.RegisterDirect<LuminaCascader, string>("DisplayText", (LuminaCascader c) => c.DisplayText);
		PanelsProperty = AvaloniaProperty.RegisterDirect<LuminaCascader, AvaloniaList<LuminaCascaderPanel>>("Panels", (LuminaCascader c) => c.Panels);
		ShowClearButtonProperty = AvaloniaProperty.RegisterDirect<LuminaCascader, bool>("ShowClearButton", (LuminaCascader c) => c.ShowClearButton, null, unsetValue: false);
		ItemsSourceProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.SyncPathFromSelectedValue();
		});
		IsDropDownOpenProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs<bool> args)
		{
			c.OnDropDownOpenChanged(args.NewValue.Value);
		});
		SelectedValueProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.OnSelectedValueChanged();
		});
		WatermarkProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.Rebuild(refreshRoots: false);
		});
		SeparatorProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.Rebuild(refreshRoots: false);
		});
		CanClearProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.Rebuild(refreshRoots: false);
		});
		MaxDropDownHeightProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.Rebuild(refreshRoots: false);
		});
		PanelWidthProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.Rebuild(refreshRoots: false);
		});
		CanSelectIntermediateNodesProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.Rebuild(refreshRoots: false);
		});
		LoadChildrenAsyncProperty.Changed.AddClassHandler(delegate(LuminaCascader c, AvaloniaPropertyChangedEventArgs _)
		{
			c.Rebuild(refreshRoots: false);
		});
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		if (_surfaceButton != null)
		{
			_surfaceButton.Click -= OnSurfaceClick;
		}
		if (_clearButton != null)
		{
			_clearButton.Click -= OnClearClick;
		}
		if (_popup != null)
		{
			_popup.Closed -= OnPopupClosed;
		}
		_surfaceButton = e.NameScope.Find<Button>("PART_SurfaceButton");
		_clearButton = e.NameScope.Find<Button>("PART_ClearButton");
		_popup = e.NameScope.Find<Popup>("PART_Popup");
		if (_surfaceButton != null)
		{
			_surfaceButton.Click += OnSurfaceClick;
		}
		if (_clearButton != null)
		{
			_clearButton.Click += OnClearClick;
		}
		if (_popup != null)
		{
			_popup.Closed += OnPopupClosed;
		}
		if (_popup != null)
		{
			_popup.Child = CreatePopupContent();
		}
		SyncPathFromSelectedValue();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (_surfaceButton != null)
		{
			_surfaceButton.Click -= OnSurfaceClick;
		}
		if (_clearButton != null)
		{
			_clearButton.Click -= OnClearClick;
		}
		if (_popup != null)
		{
			_popup.Closed -= OnPopupClosed;
		}
		DetachWheelBlocker();
		UnlockPageScroll();
		CancelHoverExpand();
		base.OnDetachedFromVisualTree(e);
	}

	public void ClearSelection()
	{
		_activePath.Clear();
		_selectedPath.Clear();
		SetSelectedValue(null);
		Rebuild();
		FireCommand();
	}

	private void OnSelectedValueChanged()
	{
		if (!_isUpdatingSelectedValue)
		{
			SyncPathFromSelectedValue();
		}
	}

	private void SyncPathFromSelectedValue()
	{
		RefreshRoots();
		_activePath.Clear();
		_selectedPath.Clear();
		if (SelectedValue != null)
		{
			List<LuminaCascaderNode>? path = FindPath(_roots, SelectedValue);
			if (path != null)
			{
				_activePath.AddRange(path);
				_selectedPath.AddRange(path);
			}
		}
		Rebuild(refreshRoots: false);
	}

	private void OnSurfaceClick(object? sender, RoutedEventArgs e)
	{
		if (!IsDropDownOpen && TryShowSheet())
		{
			e.Handled = true;
			return;
		}
		if (!IsDropDownOpen)
		{
			Rebuild();
		}
		IsDropDownOpen = !IsDropDownOpen;
	}

	private void OnClearClick(object? sender, RoutedEventArgs e)
	{
		e.Handled = true;
		if (CanClear)
		{
			ClearSelection();
		}
	}

	private void OnPopupClosed(object? sender, EventArgs e)
	{
		IsDropDownOpen = false;
	}

	private void OnDropDownOpenChanged(bool isOpen)
	{
		if (isOpen && ShouldUseSheet())
		{
			SetCurrentValue(IsDropDownOpenProperty, value: false);
			TryShowSheet();
		}
		else if (isOpen)
		{
			AttachWheelBlocker();
			LockPageScroll();
		}
		else
		{
			DetachWheelBlocker();
			UnlockPageScroll();
		}
	}

	private void LockPageScroll()
	{
		UnlockPageScroll();
		foreach (ScrollViewer scrollViewer in this.GetVisualAncestors().OfType<ScrollViewer>())
		{
			_lockedScrollViewers.Add((scrollViewer, scrollViewer.Offset));
			scrollViewer.ScrollChanged -= OnLockedScrollViewerScrollChanged;
			scrollViewer.ScrollChanged += OnLockedScrollViewerScrollChanged;
		}
	}

	private void UnlockPageScroll()
	{
		foreach (var (viewer, offset) in _lockedScrollViewers)
		{
			viewer.ScrollChanged -= OnLockedScrollViewerScrollChanged;
			RestoreScrollOffset(viewer, offset);
		}
		_lockedScrollViewers.Clear();
	}

	private void OnLockedScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		if (_isRestoringScrollOffset)
		{
			return;
		}
		ScrollViewer? scrollViewer = sender as ScrollViewer;
		if (scrollViewer != null && IsDropDownOpen)
		{
			int entryIndex = _lockedScrollViewers.FindIndex(((ScrollViewer Viewer, Vector Offset) entry) => entry.Viewer == scrollViewer);
			if (entryIndex >= 0)
			{
				Vector offset = _lockedScrollViewers[entryIndex].Offset;
				RestoreScrollOffset(scrollViewer, offset);
			}
		}
	}

	private void RestoreScrollOffset(ScrollViewer scrollViewer, Vector offset)
	{
		double maxX = Math.Max(0.0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
		double maxY = Math.Max(0.0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
		_isRestoringScrollOffset = true;
		scrollViewer.Offset = new Vector(Math.Min(offset.X, maxX), Math.Min(offset.Y, maxY));
		_isRestoringScrollOffset = false;
	}

	private void OnPopupPointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		if (IsDropDownOpen)
		{
			e.Handled = true;
		}
	}

	private void AttachWheelBlocker()
	{
		TopLevel? topLevel = TopLevel.GetTopLevel(this);
		if (topLevel != null && _wheelBlockerTopLevel != topLevel)
		{
			DetachWheelBlocker();
			_wheelBlockerTopLevel = topLevel;
			_wheelBlockerTopLevel.AddHandler(InputElement.PointerWheelChangedEvent, OnTopLevelPointerWheelChanged, RoutingStrategies.Tunnel, handledEventsToo: true);
		}
	}

	private void DetachWheelBlocker()
	{
		if (_wheelBlockerTopLevel != null)
		{
			_wheelBlockerTopLevel.RemoveHandler(InputElement.PointerWheelChangedEvent, OnTopLevelPointerWheelChanged);
			_wheelBlockerTopLevel = null;
		}
	}

	private void OnTopLevelPointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		if (IsDropDownOpen && !IsSourceInsidePopup(e.Source))
		{
			e.Handled = true;
		}
	}

	private bool IsSourceInsidePopup(object? source)
	{
		Visual? popupChild = _popup?.Child;
		if (popupChild == null || !(source is Visual sourceVisual))
		{
			return false;
		}
		return sourceVisual == popupChild || sourceVisual.GetVisualAncestors().Contains(popupChild);
	}

	internal async void OnNodeClick(LuminaCascaderNode node)
	{
		CancelHoverExpand();
		if (CanSelectNode(node))
		{
			SelectNode(node);
		}
		else if (IsExpandable(node))
		{
			await ActivateNodeAsync(node, loadChildren: true);
		}
	}

	private async void OnNodePointerEntered(LuminaCascaderNode node)
	{
		if (!ExpandOnHover || !IsDropDownOpen || !IsExpandable(node))
		{
			return;
		}
		CancelHoverExpand();
		CancellationTokenSource cancellation = (_hoverExpandCancellation = new CancellationTokenSource());
		try
		{
			await Task.Delay(180, cancellation.Token);
			if (!cancellation.IsCancellationRequested && IsDropDownOpen && IsExpandable(node))
			{
				await ActivateNodeAsync(node, loadChildren: true);
			}
		}
		catch (TaskCanceledException)
		{
		}
		finally
		{
			if (_hoverExpandCancellation == cancellation)
			{
				_hoverExpandCancellation = null;
			}
			cancellation.Dispose();
		}
	}

	private void CancelHoverExpand()
	{
		_hoverExpandCancellation?.Cancel();
		_hoverExpandCancellation?.Dispose();
		_hoverExpandCancellation = null;
	}

	private async Task ActivateNodeAsync(LuminaCascaderNode node, bool loadChildren)
	{
		SetActiveNode(node);
		if (loadChildren)
		{
			await EnsureChildrenLoadedAsync(node);
		}
		Rebuild();
	}

	private void SetActiveNode(LuminaCascaderNode node)
	{
		int depth = FindDepth(node);
		if (depth >= 0)
		{
			_activePath.RemoveRange(depth, _activePath.Count - depth);
		}
		_activePath.Add(node);
	}

	private void SelectNode(LuminaCascaderNode node)
	{
		SetActiveNode(node);
		_selectedPath.Clear();
		_selectedPath.AddRange(_activePath);
		SetSelectedValue(node.Value);
		IsDropDownOpen = false;
		CloseSheet();
		FireCommand();
		Rebuild();
	}

	private bool CanSelectNode(LuminaCascaderNode node)
	{
		return node.IsSelectable && (node.IsLeaf || CanSelectIntermediateNodes);
	}

	private bool IsExpandable(LuminaCascaderNode node)
	{
		return node.Children.Count > 0 || node.HasUnloadedChildren || node.LoadChildrenAsync != null;
	}

	private async Task EnsureChildrenLoadedAsync(LuminaCascaderNode node)
	{
		if (node.Children.Count > 0 || node.IsLoading || (node.IsLoaded && !node.HasUnloadedChildren))
		{
			return;
		}
		Func<LuminaCascaderNode, Task<IEnumerable<LuminaCascaderNode>>>? loader = node.LoadChildrenAsync ?? LoadChildrenAsync;
		if (loader == null)
		{
			return;
		}
		node.IsLoading = true;
		Rebuild(refreshRoots: false);
		try
		{
			IEnumerable<LuminaCascaderNode> children = await loader(node);
			node.Children.Clear();
			foreach (LuminaCascaderNode child in children)
			{
				node.Children.Add(child);
			}
			node.HasUnloadedChildren = false;
			node.IsLoaded = true;
		}
		catch
		{
		}
		finally
		{
			node.IsLoading = false;
		}
	}

	private void SetSelectedValue(object? value)
	{
		_isUpdatingSelectedValue = true;
		try
		{
			SelectedValue = value;
		}
		finally
		{
			_isUpdatingSelectedValue = false;
		}
	}

	private void Rebuild(bool refreshRoots = true)
	{
		if (refreshRoots)
		{
			RefreshRoots();
		}
		MarkActive();
		HasSelection = _selectedPath.Count > 0;
		ShowClearButton = CanClear && HasSelection;
		DisplayText = (HasSelection ? string.Join(Separator, _selectedPath.Select((LuminaCascaderNode n) => n.Label)) : (Watermark ?? string.Empty));
		Panels = BuildPanels();
		UpdatePopupPanels();
		UpdateSheetPanels();
	}

	public bool TryShowSheet()
	{
		if (!ShouldUseSheet())
		{
			return false;
		}
		CancelHoverExpand();
		IsDropDownOpen = false;
		_activePath.Clear();
		Rebuild();
		Control content = CreateSheetContent();
		bool shown = (_isSheetOpen = LuminaBottomSheetService.Instance.TryShow(this, content));
		if (!shown)
		{
			_sheetPanelsHost = null;
		}
		return shown;
	}

	private bool ShouldUseSheet()
	{
		return LuminaSheetPlacement.ShouldUseSheet(LuminaOptions.GetPopupType(this));
	}

	private Control CreateSheetContent()
	{
		_sheetHeaderHost = new ContentControl
		{
			Content = CreateSheetHeader()
		};
		_sheetPanelsHost = new ItemsControl();
		ScrollViewer body = new ScrollViewer
		{
			MaxHeight = Math.Max(280.0, Math.Min(480.0, MaxDropDownHeight + 120.0)),
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = _sheetPanelsHost
		};
		Button closeButton = new Button
		{
			Content = LuminaLocalization.Get("Lumina.Common.Cancel")
		};
		closeButton.Classes.Add("Ghost");
		closeButton.Click += delegate
		{
			CloseSheet();
		};
		StackPanel layout = new StackPanel
		{
			Spacing = 16.0
		};
		layout.Children.Add(_sheetHeaderHost);
		layout.Children.Add(body);
		layout.Children.Add(new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,Auto"),
			Children = { (Control)closeButton }
		});
		Grid.SetColumn(closeButton, 1);
		UpdateSheetPanels();
		return layout;
	}

	private Control CreateSheetHeader()
	{
		TextBlock textBlock = new TextBlock();
		object text;
		if (_activePath.Count != 0)
		{
			List<LuminaCascaderNode> activePath = _activePath;
			text = activePath[activePath.Count - 1].Label;
		}
		else
		{
			text = Watermark ?? LuminaLocalization.Get("Lumina.Picker.SelectOption");
		}
		textBlock.Text = (string?)text;
		textBlock.FontSize = 18.0;
		textBlock.FontWeight = FontWeight.DemiBold;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		TextBlock title = textBlock;
		TextBlock subtitle = new TextBlock
		{
			Text = ((_selectedPath.Count == 0) ? LuminaLocalization.Get(LuminaLocalizationKeys.CascaderChooseOption) : LuminaLocalization.Format(LuminaLocalizationKeys.CascaderCurrentFormat, string.Join(Separator, _selectedPath.Select((LuminaCascaderNode n) => n.Label)))),
			FontSize = 12.0,
			Foreground = Brush("LuminaTextMutedBrush", Brushes.Gray),
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		StackPanel titleBlock = new StackPanel
		{
			Spacing = 2.0,
			Children = 
			{
				(Control)title,
				(Control)subtitle
			}
		};
		Grid.SetColumn(titleBlock, 1);
		if (_activePath.Count == 0)
		{
			return titleBlock;
		}
		Button backButton = new Button
		{
			Content = new PathIcon
			{
				Width = 12.0,
				Height = 12.0,
				Data = Geometry.Parse("M15,6 L9,12 L15,18"),
				Foreground = Brush("LuminaTextMutedBrush", Brushes.Gray),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			},
			MinWidth = 40.0,
			MinHeight = 40.0,
			Padding = new Thickness(0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			Theme = TryFindControlTheme("LuminaCascaderNodeButtonTheme")
		};
		backButton.Click += delegate
		{
			if (_activePath.Count > 0)
			{
				_activePath.RemoveAt(_activePath.Count - 1);
			}
			RefreshSheetContent();
		};
		return new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("Auto,*"),
			ColumnSpacing = 10.0,
			Children = 
			{
				(Control)backButton,
				(Control)titleBlock
			}
		};
	}

	private void CloseSheet()
	{
		if (_isSheetOpen)
		{
			_isSheetOpen = false;
			_sheetPanelsHost = null;
			_sheetHeaderHost = null;
			LuminaBottomSheetService.Instance.Close(this);
		}
	}

	private void RefreshSheetContent()
	{
		if (_isSheetOpen && _sheetPanelsHost != null)
		{
			if (_sheetHeaderHost != null)
			{
				_sheetHeaderHost.Content = CreateSheetHeader();
			}
			UpdateSheetPanels();
		}
	}

	private Control CreatePopupContent()
	{
		_panelsHost = new ItemsControl
		{
			ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6.0
			})
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = _panelsHost
		};
		Border popupFrame = new Border
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0),
			Padding = new Thickness(6.0),
			Background = Brush("LuminaPopoverBrush", Brushes.White),
			BorderBrush = Brush("LuminaBorderDefaultBrush", Brushes.LightGray),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(10.0),
			BoxShadow = Shadow("LuminaShadowPopup"),
			Child = scrollViewer
		};
		popupFrame.AddHandler(InputElement.PointerWheelChangedEvent, OnPopupPointerWheelChanged, RoutingStrategies.Bubble, handledEventsToo: true);
		return popupFrame;
	}

	private void UpdatePopupPanels()
	{
		if (_panelsHost == null)
		{
			return;
		}
		AvaloniaList<Control> panelControls = new AvaloniaList<Control>();
		foreach (LuminaCascaderPanel panel in Panels)
		{
			panelControls.Add(CreatePanel(panel));
		}
		_panelsHost.ItemsSource = panelControls;
	}

	private void UpdateSheetPanels()
	{
		if (_sheetPanelsHost != null)
		{
			AvaloniaList<Control> panelControls = new AvaloniaList<Control>();
			panelControls.Add(CreateSheetList());
			_sheetPanelsHost.ItemsSource = panelControls;
		}
	}

	private Control CreateSheetList()
	{
		AvaloniaList<LuminaCascaderNode> avaloniaList;
		if (_activePath.Count != 0)
		{
			List<LuminaCascaderNode> activePath = _activePath;
			avaloniaList = activePath[activePath.Count - 1].Children;
		}
		else
		{
			avaloniaList = _roots;
		}
		AvaloniaList<LuminaCascaderNode> current = avaloniaList;
		StackPanel list = new StackPanel
		{
			Spacing = 8.0
		};
		if (_activePath.Count > 0)
		{
			List<LuminaCascaderNode> activePath2 = _activePath;
			if (CanSelectNode(activePath2[activePath2.Count - 1]))
			{
				List<LuminaCascaderNode> activePath3 = _activePath;
				LuminaCascaderNode currentNode = activePath3[activePath3.Count - 1];
				Button selectCurrent = CreateSheetRow(LuminaLocalization.Format(LuminaLocalizationKeys.CascaderSelectCurrentFormat, currentNode.Label), object.Equals(SelectedValue, currentNode.Value), showChevron: false, isPrimary: true);
				selectCurrent.Click += delegate(object? _, RoutedEventArgs e)
				{
					SelectNode(currentNode);
					e.Handled = true;
				};
				list.Children.Add(selectCurrent);
			}
		}
		foreach (LuminaCascaderNode node in current)
		{
			list.Children.Add(CreateSheetNodeButton(node));
		}
		if (list.Children.Count == 0)
		{
			list.Children.Add(CreateSheetEmptyText());
		}
		return list;
	}

	private static TextBlock CreateSheetEmptyText()
	{
		return new TextBlock
		{
			Text = LuminaLocalization.Get(LuminaLocalizationKeys.PageEmpty),
			HorizontalAlignment = HorizontalAlignment.Center,
			Foreground = Brush("LuminaTextMutedBrush", Brushes.Gray),
			TextWrapping = TextWrapping.Wrap
		};
	}

	private Button CreateSheetNodeButton(LuminaCascaderNode node)
	{
		Button button = CreateSheetRow(node.Label, object.Equals(SelectedValue, node.Value), IsExpandable(node));
		button.IsEnabled = CanSelectNode(node) || IsExpandable(node);
		button.Click += async delegate(object? _, RoutedEventArgs e)
		{
			if (IsExpandable(node))
			{
				SetActiveNode(node);
				await EnsureChildrenLoadedAsync(node);
				RefreshSheetContent();
			}
			else if (CanSelectNode(node))
			{
				SelectNode(node);
			}
			e.Handled = true;
		};
		return button;
	}

	private Button CreateSheetRow(string text, bool isActive, bool showChevron, bool isPrimary = false)
	{
		TextBlock label = new TextBlock
		{
			Text = text,
			FontSize = 15.0,
			FontWeight = (isPrimary ? FontWeight.DemiBold : FontWeight.Normal),
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		Grid content = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,Auto"),
			ColumnSpacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Children = { (Control)label }
		};
		if (isActive)
		{
			content.Children.Add(new PathIcon
			{
				Width = 18.0,
				Height = 18.0,
				Data = Geometry.Parse("M9,16.2 L4.8,12 L3.4,13.4 L9,19 L21,7 L19.6,5.6 Z"),
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = Brush("LuminaPrimaryBrush", Brushes.DodgerBlue),
				[Grid.ColumnProperty] = 1
			});
		}
		else if (showChevron)
		{
			content.Children.Add(new PathIcon
			{
				Width = 12.0,
				Height = 12.0,
				VerticalAlignment = VerticalAlignment.Center,
				Data = Geometry.Parse("M9,6 L15,12 L9,18 Z"),
				Foreground = Brush("LuminaTextMutedBrush", Brushes.Gray),
				[Grid.ColumnProperty] = 1
			});
		}
		Button button = new Button
		{
			Content = content,
			MinHeight = 52.0,
			Padding = new Thickness(14.0, 10.0),
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Theme = TryFindControlTheme("LuminaCascaderNodeButtonTheme")
		};
		button.Classes.Set("active", isActive || isPrimary);
		button.Background = (isPrimary ? Brush("LuminaPrimaryBgBrush", Brushes.Transparent) : Brush("LuminaSurfaceElevatedBrush", Brushes.Transparent));
		button.BorderBrush = (isActive ? Brush("LuminaPrimaryBrush", Brushes.DodgerBlue) : Brush("LuminaBorderDefaultBrush", Brushes.LightGray));
		button.BorderThickness = new Thickness(1.0);
		button.CornerRadius = new CornerRadius(14.0);
		label[!TextBlock.ForegroundProperty] = button[!TemplatedControl.ForegroundProperty];
		return button;
	}

	private Control CreatePanel(LuminaCascaderPanel panel)
	{
		StackPanel list = new StackPanel
		{
			Spacing = 2.0
		};
		foreach (LuminaCascaderNode node in panel.Nodes)
		{
			list.Children.Add(CreateNodeButton(node));
		}
		return new Border
		{
			Width = panel.Width,
			MaxHeight = panel.MaxHeight,
			Padding = new Thickness(4.0),
			Background = Brush("LuminaSurfaceBrush", Brushes.White),
			BorderBrush = Brush("LuminaBorderBrush", Brushes.LightGray),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Child = new ScrollViewer
			{
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Content = list
			}
		};
	}

	private Button CreateNodeButton(LuminaCascaderNode node)
	{
		TextBlock label = new TextBlock
		{
			Text = node.Label,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		Grid content = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,Auto"),
			ColumnSpacing = 8.0,
			Children = { (Control)label }
		};
		if (node.IsLoading)
		{
			content.Children.Add(new TextBlock
			{
				Text = "...",
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = Brush("LuminaTextMutedBrush", Brushes.Gray),
				[Grid.ColumnProperty] = 1
			});
		}
		else if (IsExpandable(node))
		{
			content.Children.Add(new PathIcon
			{
				Width = 12.0,
				Height = 12.0,
				VerticalAlignment = VerticalAlignment.Center,
				Data = Geometry.Parse("M9,6 L15,12 L9,18 Z"),
				Foreground = Brush("LuminaTextMutedBrush", Brushes.Gray),
				[Grid.ColumnProperty] = 1
			});
		}
		Button button = new Button
		{
			Name = "PART_NodeButton",
			DataContext = node,
			Content = content,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			IsEnabled = (CanSelectNode(node) || IsExpandable(node)),
			Theme = TryFindControlTheme("LuminaCascaderNodeButtonTheme")
		};
		button.Classes.Set("active", node.IsActive);
		label[!TextBlock.ForegroundProperty] = button[!TemplatedControl.ForegroundProperty];
		button.PointerEntered += delegate
		{
			OnNodePointerEntered(node);
		};
		button.AddHandler(InputElement.PointerPressedEvent, delegate(object? _, PointerPressedEventArgs e)
		{
			if (CanSelectNode(node) && IsExpandable(node) && e.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
			{
				OnNodeClick(node);
				e.Handled = true;
			}
		}, RoutingStrategies.Tunnel);
		button.Click += delegate(object? _, RoutedEventArgs e)
		{
			OnNodeClick(node);
			e.Handled = true;
		};
		return button;
	}

	private static ControlTheme? TryFindControlTheme(string key)
	{
		Application? current = Application.Current;
		object? resource;
		return (current != null && current.TryFindResource(key, out resource) && resource is ControlTheme theme) ? theme : null;
	}

	private static IBrush Brush(string key, IBrush fallback)
	{
		Application? current = Application.Current;
		object? resource;
		return (current != null && current.TryFindResource(key, out resource) && resource is IBrush brush) ? brush : fallback;
	}

	private static BoxShadows Shadow(string key)
	{
		Application? current = Application.Current;
		object? resource;
		return (current != null && current.TryFindResource(key, out resource) && resource is BoxShadows shadow) ? shadow : default(BoxShadows);
	}

	private void RefreshRoots()
	{
		_roots.Clear();
		if (ItemsSource == null)
		{
			return;
		}
		foreach (object item in ItemsSource)
		{
			if (item is LuminaCascaderNode node)
			{
				_roots.Add(node);
			}
		}
	}

	private AvaloniaList<LuminaCascaderPanel> BuildPanels()
	{
		AvaloniaList<LuminaCascaderPanel> panels = new AvaloniaList<LuminaCascaderPanel>
		{
			new LuminaCascaderPanel(_roots, PanelWidth, MaxDropDownHeight)
		};
		foreach (LuminaCascaderNode node in _activePath)
		{
			if (node.Children.Count > 0)
			{
				panels.Add(new LuminaCascaderPanel(node.Children, PanelWidth, MaxDropDownHeight));
			}
		}
		return panels;
	}

	private void MarkActive()
	{
		ClearActive(_roots);
		for (int i = 0; i < _activePath.Count; i++)
		{
			LuminaCascaderNode target = _activePath[i];
			AvaloniaList<LuminaCascaderNode> source = ((i == 0) ? _roots : _activePath[i - 1].Children);
			foreach (LuminaCascaderNode n in source)
			{
				if (object.Equals(n.Value, target.Value))
				{
					n.IsActive = true;
					break;
				}
			}
		}
	}

	private static void ClearActive(IEnumerable<LuminaCascaderNode> nodes)
	{
		foreach (LuminaCascaderNode node in nodes)
		{
			node.IsActive = false;
			ClearActive(node.Children);
		}
	}

	private int FindDepth(LuminaCascaderNode node)
	{
		if (_roots.Contains(node))
		{
			return 0;
		}
		for (int i = 0; i < _activePath.Count; i++)
		{
			if (_activePath[i].Children.Contains(node))
			{
				return i + 1;
			}
		}
		return -1;
	}

	private static List<LuminaCascaderNode>? FindPath(IEnumerable<LuminaCascaderNode> nodes, object? value)
	{
		foreach (LuminaCascaderNode node in nodes)
		{
			if (object.Equals(node.Value, value))
			{
				int num = 1;
				List<LuminaCascaderNode> list = new List<LuminaCascaderNode>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<LuminaCascaderNode> span = CollectionsMarshal.AsSpan(list);
				int index = 0;
				span[index] = node;
				return list;
			}
			List<LuminaCascaderNode>? childPath = FindPath(node.Children, value);
			if (childPath != null)
			{
				childPath.Insert(0, node);
				return childPath;
			}
		}
		return null;
	}

	private void FireCommand()
	{
		ICommand? cmd = SelectionChangedCommand;
		if (cmd != null && cmd.CanExecute(SelectedValue))
		{
			cmd.Execute(SelectedValue);
		}
	}
}
