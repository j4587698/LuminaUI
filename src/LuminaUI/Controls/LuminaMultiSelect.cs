using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using LuminaUI.Extensions;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Controls;

public class LuminaMultiSelect : TemplatedControl
{
	private sealed class SheetSelectionEntry(object? item, string text, bool isSelected)
	{
		public object? Item { get; } = item;

		public string Text { get; } = text;

		public bool IsSelected { get; } = isSelected;

		public CheckBox? CheckBox { get; set; }
	}

	private Button? _surfaceButton;

	private Button? _clearButton;

	private Popup? _popup;

	private bool _isSyncingSelection;

	private INotifyCollectionChanged? _selectedItemsNotifier;

	private INotifyCollectionChanged? _itemsSourceNotifier;

	private readonly AvaloniaList<LuminaMultiSelectOption> _options = new AvaloniaList<LuminaMultiSelectOption>();

	public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty;

	public static readonly DirectProperty<LuminaMultiSelect, AvaloniaList<LuminaMultiSelectOption>> OptionsProperty;

	public static readonly StyledProperty<IList?> SelectedItemsProperty;

	public static readonly StyledProperty<string?> WatermarkProperty;

	public static readonly StyledProperty<bool> IsDropDownOpenProperty;

	public static readonly StyledProperty<double> MaxDropDownHeightProperty;

	public static readonly StyledProperty<bool> CanClearSelectionProperty;

	public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty;

	public static readonly StyledProperty<IDataTemplate?> SelectedItemTemplateProperty;

	public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty;

	public static readonly DirectProperty<LuminaMultiSelect, bool> HasSelectionProperty;

	private bool _hasSelection;

	public static readonly DirectProperty<LuminaMultiSelect, string> SelectionSummaryProperty;

	private string _selectionSummary = string.Empty;

	public static readonly DirectProperty<LuminaMultiSelect, bool> ShowClearButtonProperty;

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

	public AvaloniaList<LuminaMultiSelectOption> Options => _options;

	public IList? SelectedItems
	{
		get
		{
			return GetValue(SelectedItemsProperty);
		}
		set
		{
			SetValue(SelectedItemsProperty, value);
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

	public bool CanClearSelection
	{
		get
		{
			return GetValue(CanClearSelectionProperty);
		}
		set
		{
			SetValue(CanClearSelectionProperty, value);
		}
	}

	public IDataTemplate? ItemTemplate
	{
		get
		{
			return GetValue(ItemTemplateProperty);
		}
		set
		{
			SetValue(ItemTemplateProperty, value);
		}
	}

	public IDataTemplate? SelectedItemTemplate
	{
		get
		{
			return GetValue(SelectedItemTemplateProperty);
		}
		set
		{
			SetValue(SelectedItemTemplateProperty, value);
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

	public string SelectionSummary
	{
		get
		{
			return _selectionSummary;
		}
		private set
		{
			SetAndRaise(SelectionSummaryProperty, ref _selectionSummary, value);
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

	static LuminaMultiSelect()
	{
		ItemsSourceProperty = AvaloniaProperty.Register<LuminaMultiSelect, IEnumerable?>("ItemsSource");
		OptionsProperty = AvaloniaProperty.RegisterDirect<LuminaMultiSelect, AvaloniaList<LuminaMultiSelectOption>>("Options", (LuminaMultiSelect control) => control.Options);
		SelectedItemsProperty = AvaloniaProperty.Register<LuminaMultiSelect, IList?>("SelectedItems", null, inherits: false, BindingMode.TwoWay);
		WatermarkProperty = AvaloniaProperty.Register<LuminaMultiSelect, string?>("Watermark");
		IsDropDownOpenProperty = AvaloniaProperty.Register<LuminaMultiSelect, bool>("IsDropDownOpen", defaultValue: false, inherits: false, BindingMode.TwoWay);
		MaxDropDownHeightProperty = AvaloniaProperty.Register<LuminaMultiSelect, double>("MaxDropDownHeight", 260.0);
		CanClearSelectionProperty = AvaloniaProperty.Register<LuminaMultiSelect, bool>("CanClearSelection", defaultValue: true);
		ItemTemplateProperty = AvaloniaProperty.Register<LuminaMultiSelect, IDataTemplate?>("ItemTemplate");
		SelectedItemTemplateProperty = AvaloniaProperty.Register<LuminaMultiSelect, IDataTemplate?>("SelectedItemTemplate");
		SelectionChangedCommandProperty = AvaloniaProperty.Register<LuminaMultiSelect, ICommand?>("SelectionChangedCommand");
		HasSelectionProperty = AvaloniaProperty.RegisterDirect<LuminaMultiSelect, bool>("HasSelection", (LuminaMultiSelect control) => control.HasSelection, null, unsetValue: false);
		SelectionSummaryProperty = AvaloniaProperty.RegisterDirect<LuminaMultiSelect, string>("SelectionSummary", (LuminaMultiSelect control) => control.SelectionSummary);
		ShowClearButtonProperty = AvaloniaProperty.RegisterDirect<LuminaMultiSelect, bool>("ShowClearButton", (LuminaMultiSelect control) => control.ShowClearButton, null, unsetValue: false);
		SelectedItemsProperty.Changed.AddClassHandler(delegate(LuminaMultiSelect control, AvaloniaPropertyChangedEventArgs<IList?> args)
		{
			control.OnSelectedItemsChanged(args.OldValue.Value, args.NewValue.Value);
		});
		ItemsSourceProperty.Changed.AddClassHandler(delegate(LuminaMultiSelect control, AvaloniaPropertyChangedEventArgs<IEnumerable?> args)
		{
			control.OnItemsSourceChanged(args.OldValue.Value, args.NewValue.Value);
		});
		CanClearSelectionProperty.Changed.AddClassHandler(delegate(LuminaMultiSelect control, AvaloniaPropertyChangedEventArgs _)
		{
			control.UpdateSelectionState();
		});
		WatermarkProperty.Changed.AddClassHandler(delegate(LuminaMultiSelect control, AvaloniaPropertyChangedEventArgs _)
		{
			control.UpdateSelectionState();
		});
	}

	public LuminaMultiSelect()
	{
		SelectedItems = new AvaloniaList<object>();
		UpdateSelectionState();
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
		RemoveHandler(Button.ClickEvent, OnOptionButtonClick);
		_surfaceButton = e.NameScope.FindRequired<Button>("PART_SurfaceButton");
		_clearButton = e.NameScope.FindRequired<Button>("PART_ClearButton");
		_popup = e.NameScope.FindRequired<Popup>("PART_Popup");
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
		AddHandler(Button.ClickEvent, OnOptionButtonClick, RoutingStrategies.Bubble);
		RebuildOptions();
		UpdateSelectionState();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		SubscribeToSelectedItems(null);
		SubscribeToItemsSource(null);
		RemoveHandler(Button.ClickEvent, OnOptionButtonClick);
		base.OnDetachedFromVisualTree(e);
	}

	public void ClearSelection()
	{
		SelectedItems?.Clear();
		SyncOptionSelection();
		UpdateSelectionState();
		ExecuteSelectionChangedCommand();
	}

	private void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
	{
		SubscribeToItemsSource(newValue);
		RebuildOptions();
	}

	private void SubscribeToItemsSource(IEnumerable? itemsSource)
	{
		if (_itemsSourceNotifier != null)
		{
			_itemsSourceNotifier.CollectionChanged -= OnItemsSourceCollectionChanged;
		}
		_itemsSourceNotifier = itemsSource as INotifyCollectionChanged;
		if (_itemsSourceNotifier != null)
		{
			_itemsSourceNotifier.CollectionChanged += OnItemsSourceCollectionChanged;
		}
	}

	private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		RebuildOptions();
	}

	private void OnSelectedItemsChanged(IList? oldValue, IList? newValue)
	{
		SubscribeToSelectedItems(newValue);
		SyncOptionSelection();
		UpdateSelectionState();
	}

	private void SubscribeToSelectedItems(IList? items)
	{
		if (_selectedItemsNotifier != null)
		{
			_selectedItemsNotifier.CollectionChanged -= OnSelectedItemsCollectionChanged;
		}
		_selectedItemsNotifier = items as INotifyCollectionChanged;
		if (_selectedItemsNotifier != null)
		{
			_selectedItemsNotifier.CollectionChanged += OnSelectedItemsCollectionChanged;
		}
	}

	private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (!_isSyncingSelection)
		{
			SyncOptionSelection();
			UpdateSelectionState();
		}
	}

	private void OnSurfaceClick(object? sender, RoutedEventArgs e)
	{
		if (ShouldUseSheet() && TryShowSheet())
		{
			e.Handled = true;
		}
		else
		{
			IsDropDownOpen = !IsDropDownOpen;
		}
	}

	private void OnClearClick(object? sender, RoutedEventArgs e)
	{
		e.Handled = true;
		if (CanClearSelection)
		{
			ClearSelection();
		}
	}

	private void OnPopupClosed(object? sender, EventArgs e)
	{
		IsDropDownOpen = false;
	}

	private void OnOptionButtonClick(object? sender, RoutedEventArgs e)
	{
		if (e.Source is Button { Name: "PART_OptionButton", DataContext: LuminaMultiSelectOption option })
		{
			ToggleOption(option);
			e.Handled = true;
		}
	}

	private IList EnsureSelectedItems()
	{
		if (SelectedItems != null)
		{
			return SelectedItems;
		}
		return SelectedItems = new AvaloniaList<object>();
	}

	public bool TryShowSheet()
	{
		IsDropDownOpen = false;
		if (_options.Count == 0)
		{
			RebuildOptions();
		}
		List<SheetSelectionEntry> entries = new List<SheetSelectionEntry>();
		StackPanel list = new StackPanel
		{
			Spacing = 4.0
		};
		if (_options.Count == 0)
		{
			list.Children.Add(CreateSheetEmptyText());
		}
		else
		{
			foreach (LuminaMultiSelectOption option in _options)
			{
				SheetSelectionEntry entry = CreateSheetSelectionEntry(option);
				entries.Add(entry);
				list.Children.Add(CreateSheetSelectionButton(entry));
			}
		}
		ScrollViewer body = new ScrollViewer
		{
			MaxHeight = Math.Max(240.0, Math.Min(420.0, MaxDropDownHeight)),
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = list
		};
		StackPanel content = CreateSheetLayout(Watermark ?? LuminaLocalization.Get("Lumina.Picker.SelectOption"), body, CreateSheetActions(entries));
		return LuminaBottomSheetService.Instance.TryShow(this, content);
	}

	private static TextBlock CreateSheetEmptyText()
	{
		return new TextBlock
		{
			Text = LuminaLocalization.Get(LuminaLocalizationKeys.PageEmpty),
			HorizontalAlignment = HorizontalAlignment.Center,
			Foreground = LuminaPickerResources.Brush("LuminaTextMutedBrush", Brushes.Gray),
			TextWrapping = TextWrapping.Wrap
		};
	}

	private bool ShouldUseSheet()
	{
		return LuminaSheetPlacement.ShouldUseSheet(LuminaOptions.GetPopupType(this));
	}

	private SheetSelectionEntry CreateSheetSelectionEntry(LuminaMultiSelectOption option)
	{
		return new SheetSelectionEntry(option.Item, option.DisplayText, option.IsSelected);
	}

	private Button CreateSheetSelectionButton(SheetSelectionEntry entry)
	{
		CheckBox checkBox = new CheckBox
		{
			IsChecked = entry.IsSelected,
			IsHitTestVisible = false,
			VerticalAlignment = VerticalAlignment.Center
		};
		entry.CheckBox = checkBox;
		TextBlock label = new TextBlock
		{
			Text = entry.Text,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Foreground = LuminaPickerResources.Brush("LuminaTextPrimaryBrush", Brushes.White)
		};
		Grid grid = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("Auto,*"),
			ColumnSpacing = 12.0,
			Children = 
			{
				(Control)checkBox,
				(Control)label
			}
		};
		Grid.SetColumn(label, 1);
		Button button = new Button
		{
			Content = grid,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch
		};
		button.Classes.Add("ActionSheetItem");
		button.Click += delegate
		{
			checkBox.IsChecked = checkBox.IsChecked != true;
		};
		return button;
	}

	private Control CreateSheetActions(IReadOnlyList<SheetSelectionEntry> entries)
	{
		Grid actions = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions(CanClearSelection ? "*,Auto,Auto" : "*,Auto"),
			ColumnSpacing = 8.0
		};
		Button cancelButton = new Button
		{
			Content = LuminaLocalization.Get("Lumina.Common.Cancel")
		};
		cancelButton.Classes.Add("Outline");
		cancelButton.Click += delegate
		{
			LuminaBottomSheetService.Instance.Close(this);
		};
		Button doneButton = new Button
		{
			Content = LuminaLocalization.Get("Lumina.Common.Done")
		};
		doneButton.Classes.Add("Primary");
		doneButton.Click += delegate
		{
			ApplySheetSelection(entries);
			LuminaBottomSheetService.Instance.Close(this);
		};
		if (CanClearSelection)
		{
			Button clearButton = new Button
			{
				Content = LuminaLocalization.Get("Lumina.Common.Clear")
			};
			clearButton.Classes.Add("Ghost");
			clearButton.Click += delegate
			{
				ClearSelection();
				LuminaBottomSheetService.Instance.Close(this);
			};
			Grid.SetColumn(clearButton, 0);
			Grid.SetColumn(cancelButton, 1);
			Grid.SetColumn(doneButton, 2);
			actions.Children.Add(clearButton);
		}
		else
		{
			Grid.SetColumn(cancelButton, 0);
			Grid.SetColumn(doneButton, 1);
		}
		actions.Children.Add(cancelButton);
		actions.Children.Add(doneButton);
		return actions;
	}

	private void ApplySheetSelection(IReadOnlyList<SheetSelectionEntry> entries)
	{
		IList selectedItems = EnsureSelectedItems();
		_isSyncingSelection = true;
		selectedItems.Clear();
		foreach (SheetSelectionEntry entry in entries)
		{
			CheckBox? checkBox = entry.CheckBox;
			if (checkBox != null && checkBox.IsChecked == true)
			{
				selectedItems.Add(entry.Item);
			}
		}
		_isSyncingSelection = false;
		SyncOptionSelection();
		UpdateSelectionState();
		ExecuteSelectionChangedCommand();
	}

	private static StackPanel CreateSheetLayout(string title, Control body, Control footer)
	{
		return new StackPanel
		{
			Spacing = 16.0,
			Children = 
			{
				(Control)new TextBlock
				{
					Text = title,
					FontSize = 18.0,
					FontWeight = FontWeight.DemiBold
				},
				body,
				footer
			}
		};
	}

	private void RebuildOptions()
	{
		_options.Clear();
		if (ItemsSource != null)
		{
			foreach (object item in ItemsSource)
			{
				_options.Add(new LuminaMultiSelectOption(item, IsSelected(item)));
			}
		}
		UpdateSelectionState();
	}

	private void SyncOptionSelection()
	{
		foreach (LuminaMultiSelectOption option in _options)
		{
			option.IsSelected = IsSelected(option.Item);
		}
	}

	private void ToggleOption(LuminaMultiSelectOption option)
	{
		IList selectedItems = EnsureSelectedItems();
		_isSyncingSelection = true;
		if (selectedItems.Contains(option.Item))
		{
			selectedItems.Remove(option.Item);
			option.IsSelected = false;
		}
		else
		{
			selectedItems.Add(option.Item);
			option.IsSelected = true;
		}
		_isSyncingSelection = false;
		UpdateSelectionState();
		ExecuteSelectionChangedCommand();
	}

	private bool IsSelected(object? item)
	{
		if (SelectedItems == null)
		{
			return false;
		}
		foreach (object selectedItem in SelectedItems)
		{
			if (object.Equals(selectedItem, item))
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateSelectionState()
	{
		int count = SelectedItems?.Count ?? 0;
		HasSelection = count > 0;
		ShowClearButton = CanClearSelection && count > 0;
		SelectionSummary = count switch
		{
			1 => SelectedItems?[0]?.ToString() ?? string.Empty, 
			0 => Watermark ?? string.Empty, 
			_ => $"{count} selected", 
		};
		SyncOptionSelection();
	}

	private void ExecuteSelectionChangedCommand()
	{
		ICommand? command = SelectionChangedCommand;
		if (command != null && command.CanExecute(SelectedItems))
		{
			command.Execute(SelectedItems);
		}
	}
}
