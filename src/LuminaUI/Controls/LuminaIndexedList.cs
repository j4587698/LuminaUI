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
[TemplatePart("PART_IndexPanel", typeof(Panel))]
public class LuminaIndexedList : ItemsControl
{
    private readonly List<Button> _indexButtons = new List<Button>();

    private ScrollViewer? _scrollViewer;

    private Panel? _indexPanel;

    private bool _isUpdatingFromClick;

    public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<LuminaIndexedList, int>(nameof(SelectedIndex), 0, inherits: false, BindingMode.TwoWay);

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        }
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.FindRequired<ScrollViewer>("PART_ScrollViewer");
        _indexPanel = e.NameScope.FindRequired<Panel>("PART_IndexPanel");
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
            UpdateIndexButtonState();
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
        Avalonia.Threading.Dispatcher.UIThread.Post(RebuildIndex, DispatcherPriority.Loaded);
    }

    private void RebuildIndex()
    {
        if (_indexPanel != null)
        {
            _indexButtons.Clear();
            _indexPanel.Children.Clear();
            List<LuminaIndexedSection> sections = GetSections();
            for (int index = 0; index < sections.Count; index++)
            {
                LuminaIndexedSection section = sections[index];
                Button button = new Button
                {
                    Content = section.GetNavigationKey(),
                    Tag = index,
                    Theme = ResolveButtonTheme("LuminaIndexButtonTheme")
                };
                button.Classes.Add("LuminaIndexButton");
                button.Click += OnIndexButtonClick;
                _indexButtons.Add(button);
                _indexPanel.Children.Add(button);
            }
            if (sections.Count == 0)
            {
                SelectedIndex = 0;
                return;
            }
            SelectedIndex = Math.Clamp(SelectedIndex, 0, sections.Count - 1);
            UpdateIndexButtonState();
        }
    }

    private void OnIndexButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }
        if (button.Tag is not int index)
        {
            return;
        }
        List<LuminaIndexedSection> sections = GetSections();
        if (index >= 0 && index < sections.Count)
        {
            _isUpdatingFromClick = true;
            SelectedIndex = index;
            ScrollToSection(sections[index]);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
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
        List<LuminaIndexedSection> sections = GetSections();
        if (sections.Count == 0)
        {
            return;
        }
        int selected = 0;
        for (int index = 0; index < sections.Count; index++)
        {
            Point? position = sections[index].TranslatePoint(new Point(0.0, 0.0), _scrollViewer);
            if (position.HasValue && position.Value.Y <= 12.0)
            {
                selected = index;
            }
        }
        SelectedIndex = selected;
    }

    private void ScrollToSection(LuminaIndexedSection section)
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

    private void UpdateIndexButtonState()
    {
        for (int index = 0; index < _indexButtons.Count; index++)
        {
            SetClass(_indexButtons[index], "Selected", index == SelectedIndex);
        }
    }

    public void LoadFlatData<T>(IEnumerable<T> items, Func<T, string> keySelector, Func<T, string> headerSelector, Func<T, Control> itemVisualFactory)
    {
        ItemsSource = null;
        Items.Clear();
        IOrderedEnumerable<IGrouping<string, T>> groups = items.GroupBy(keySelector).OrderBy(g => g.Key);
        List<LuminaIndexedSection> newSections = new List<LuminaIndexedSection>();
        foreach (IGrouping<string, T> group in groups)
        {
            LuminaIndexedSection section = new LuminaIndexedSection
            {
                Key = group.Key,
                Header = headerSelector(group.First())
            };
            foreach (T item in group)
            {
                section.Items.Add(itemVisualFactory(item));
            }
            newSections.Add(section);
        }
        ItemsSource = newSections;
        QueueRebuild();
    }

    private List<LuminaIndexedSection> GetSections()
    {
        List<LuminaIndexedSection> result = Items.OfType<LuminaIndexedSection>().ToList();
        if (result.Count > 0)
        {
            return result;
        }
        if (ItemsSource != null)
        {
            result = ItemsSource.OfType<LuminaIndexedSection>().ToList();
            if (result.Count > 0)
            {
                return result;
            }
        }
        return this.GetVisualDescendants().OfType<LuminaIndexedSection>().ToList();
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
