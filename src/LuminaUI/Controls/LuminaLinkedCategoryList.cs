using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_CategoryPanel", typeof(Panel))]
public class LuminaLinkedCategoryList : ItemsControl
{
	private readonly List<Button> _categoryButtons = new List<Button>();

	private ScrollViewer? _scrollViewer;

	private Panel? _categoryPanel;

	private bool _isUpdatingFromClick;

	public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<LuminaLinkedCategoryList, int>("SelectedIndex", 0, inherits: false, BindingMode.TwoWay);

	public static readonly StyledProperty<double> CategoryWidthProperty = AvaloniaProperty.Register<LuminaLinkedCategoryList, double>("CategoryWidth", 112.0);

	public int SelectedIndex
	{
		get
		{
			return GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
		}
	}

	public double CategoryWidth
	{
		get
		{
			return GetValue(CategoryWidthProperty);
		}
		set
		{
			SetValue(CategoryWidthProperty, value);
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_scrollViewer != null)
		{
			_scrollViewer.ScrollChanged -= OnScrollChanged;
		}
		base.OnApplyTemplate(e);
		_scrollViewer = e.NameScope.FindRequired<ScrollViewer>("PART_ScrollViewer");
		_categoryPanel = e.NameScope.FindRequired<Panel>("PART_CategoryPanel");
		if (_scrollViewer != null)
		{
			_scrollViewer.ScrollChanged += OnScrollChanged;
		}
		QueueRebuild();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == SelectedIndexProperty)
		{
			UpdateCategoryButtonState();
		}
		else if (change.Property == ItemsControl.ItemsSourceProperty)
		{
			QueueRebuild();
		}
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnAttachedToLogicalTree(e);
		QueueRebuild();
	}

	private void QueueRebuild()
	{
		Avalonia.Threading.Dispatcher.UIThread.Post(RebuildCategories, DispatcherPriority.Loaded);
	}

	private void RebuildCategories()
	{
		if (_categoryPanel != null)
		{
			_categoryButtons.Clear();
			_categoryPanel.Children.Clear();
			List<LuminaLinkedCategorySection> sections = GetSections();
			for (int index = 0; index < sections.Count; index++)
			{
				LuminaLinkedCategorySection section = sections[index];
				Button button = new Button
				{
					Content = (section.Header ?? section.GetNavigationKey()),
					Tag = index,
					Theme = ResolveButtonTheme("LuminaCategoryButtonTheme")
				};
				button.Classes.Add("LuminaCategoryButton");
				button.Click += OnCategoryButtonClick;
				_categoryButtons.Add(button);
				_categoryPanel.Children.Add(button);
			}
			if (sections.Count == 0)
			{
				SelectedIndex = 0;
				return;
			}
			SelectedIndex = Math.Clamp(SelectedIndex, 0, sections.Count - 1);
			UpdateCategoryButtonState();
		}
	}

	private void OnCategoryButtonClick(object? sender, RoutedEventArgs e)
	{
		if (!(sender is Button button))
		{
			return;
		}
		object? tag = button.Tag;
		int index = default(int);
		int num;
		if (tag is int)
		{
			index = (int)tag;
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num == 0)
		{
			return;
		}
		List<LuminaLinkedCategorySection> sections = GetSections();
		if (index >= 0 && index < sections.Count)
		{
			_isUpdatingFromClick = true;
			SelectedIndex = index;
			ScrollToSection(sections[index]);
			Avalonia.Threading.Dispatcher.UIThread.Post(delegate
			{
				_isUpdatingFromClick = false;
			}, DispatcherPriority.Background);
		}
	}

	private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		if (!_isUpdatingFromClick)
		{
			UpdateSelectedIndexFromScroll();
		}
	}

	private void UpdateSelectedIndexFromScroll()
	{
		if (_scrollViewer == null)
		{
			return;
		}
		List<LuminaLinkedCategorySection> sections = GetSections();
		if (sections.Count == 0)
		{
			return;
		}
		int selected = 0;
		for (int index = 0; index < sections.Count; index++)
		{
			Point? position = sections[index].TranslatePoint(new Point(0.0, 0.0), _scrollViewer);
			if (position.HasValue && position.Value.Y <= 16.0)
			{
				selected = index;
			}
		}
		SelectedIndex = selected;
	}

	private void ScrollToSection(LuminaLinkedCategorySection section)
	{
		if (_scrollViewer == null)
		{
			section.BringIntoView();
			return;
		}
		Point? position = section.TranslatePoint(new Point(0.0, 0.0), _scrollViewer);
		if (!position.HasValue)
		{
			section.BringIntoView();
			return;
		}
		double targetY = Math.Max(0.0, _scrollViewer.Offset.Y + position.Value.Y);
		_scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetY);
	}

	private void UpdateCategoryButtonState()
	{
		for (int index = 0; index < _categoryButtons.Count; index++)
		{
			SetClass(_categoryButtons[index], "Selected", index == SelectedIndex);
		}
	}

	private List<LuminaLinkedCategorySection> GetSections()
	{
		List<LuminaLinkedCategorySection> result = base.Items.OfType<LuminaLinkedCategorySection>().ToList();
		if (result.Count > 0)
		{
			return result;
		}
		if (base.ItemsSource != null)
		{
			result = base.ItemsSource.OfType<LuminaLinkedCategorySection>().ToList();
			if (result.Count > 0)
			{
				return result;
			}
		}
		return this.GetVisualDescendants().OfType<LuminaLinkedCategorySection>().ToList();
	}

	private static void SetClass(Control control, string className, bool isEnabled)
	{
		if (isEnabled)
		{
			if (!control.Classes.Contains(className))
			{
				control.Classes.Add(className);
			}
		}
		else
		{
			control.Classes.Remove(className);
		}
	}

	private static ControlTheme? ResolveButtonTheme(string key)
	{
		Application? current = Application.Current;
		object? resource;
		return (current != null && current.TryFindResource(key, out resource)) ? (resource as ControlTheme) : null;
	}
}
