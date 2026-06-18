using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Controls;

internal sealed class LuminaWheelColumn : Grid
{
    private const double DefaultItemHeight = 44.0;

    private const int VisibleItemCount = 5;

    private const int CenterRowIndex = 2;

    private readonly double _itemHeight;

    private readonly Grid _itemsGrid;

    private readonly List<Border> _itemRows = new List<Border>();

    private readonly List<TextBlock> _itemTexts = new List<TextBlock>();

    private List<LuminaWheelOption> _options = new List<LuminaWheelOption>();

    private int _selectedIndex;

    public int SelectedValue { get; private set; }

    public event Action<int>? ValueChanged;

    public LuminaWheelColumn(string label)
    {
        _itemHeight = LuminaPickerResources.Double("LuminaWheelColumnItemHeight", DefaultItemHeight);
        RowDefinitions = new RowDefinitions("Auto,Auto");
        HorizontalAlignment = HorizontalAlignment.Stretch;
        LuminaPickerResources.BindResource(this, Grid.RowSpacingProperty, "LuminaWheelColumnRowSpacing");
        TextBlock labelBlock = new TextBlock
        {
            Text = label,
            FontWeight = FontWeight.DemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        LuminaPickerResources.BindResource(labelBlock, TextBlock.FontSizeProperty, "LuminaWheelColumnLabelFontSize");
        labelBlock.Foreground = LuminaPickerResources.Brush("LuminaTextMutedBrush", Brushes.Gray);
        labelBlock.Classes.Add("LuminaWheelColumnLabel");
        Children.Add(labelBlock);
        _itemsGrid = new Grid
        {
            Height = _itemHeight * VisibleItemCount,
            RowDefinitions = CreateItemRows(_itemHeight),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ZIndex = 2
        };
        for (int rowIndex = 0; rowIndex < VisibleItemCount; rowIndex++)
        {
            TextBlock textBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            textBlock.Classes.Add("LuminaWheelItemText");
            Border row = new Border
            {
                Height = _itemHeight,
                Background = Brushes.Transparent,
                Child = textBlock
            };
            row.Classes.Add("LuminaWheelItem");
            Grid.SetRow(row, rowIndex);
            int capturedRowIndex = rowIndex;
            row.PointerPressed += (_, e) =>
            {
                SelectIndex(_selectedIndex + capturedRowIndex - 2, notify: true);
                e.Handled = true;
            };
            _itemRows.Add(row);
            _itemTexts.Add(textBlock);
            _itemsGrid.Children.Add(row);
        }
        Border selectionBand = new Border
        {
            Height = _itemHeight,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            ZIndex = 1,
            Opacity = LuminaPickerResources.Double("LuminaWheelColumnSelectionBandOpacity", 0.72)
        };
        LuminaPickerResources.BindResource(selectionBand, Border.BorderThicknessProperty, "LuminaWheelColumnSelectionBandBorderThickness");
        LuminaPickerResources.BindResource(selectionBand, Border.CornerRadiusProperty, "LuminaWheelColumnSelectionBandCornerRadius");
        selectionBand.Background = LuminaPickerResources.Brush("LuminaPrimaryBgBrush", Brushes.Transparent);
        selectionBand.BorderBrush = LuminaPickerResources.Brush("LuminaPrimaryBrush", Brushes.DodgerBlue);
        selectionBand.Classes.Add("LuminaWheelSelectionBand");
        Border frame = new Border
        {
            ClipToBounds = true,
            Child = new Grid
            {
                Children =
                {
                    selectionBand,
                    _itemsGrid
                }
            }
        };
        LuminaPickerResources.BindResource(frame, Border.BorderThicknessProperty, "LuminaWheelColumnFrameBorderThickness");
        LuminaPickerResources.BindResource(frame, Border.CornerRadiusProperty, "LuminaWheelColumnFrameCornerRadius");
        frame.Background = LuminaPickerResources.Brush("LuminaSurfaceElevatedBrush", Brushes.Transparent);
        frame.BorderBrush = LuminaPickerResources.Brush("LuminaBorderDefaultBrush", Brushes.Gray);
        frame.Classes.Add("LuminaWheelFrame");
        frame.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        Grid.SetRow(frame, 1);
        Children.Add(frame);
    }

    public void SetOptions(IReadOnlyList<LuminaWheelOption> options, int preferredValue)
    {
        if (options.Count == 0)
        {
            _options = new List<LuminaWheelOption>();
            _selectedIndex = 0;
            SelectedValue = preferredValue;
            RefreshVisibleItems();
        }
        else
        {
            _options = options.ToList();
            _selectedIndex = FindNearestIndex(_options, preferredValue);
            SelectedValue = _options[_selectedIndex].Value;
            RefreshVisibleItems();
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_options.Count != 0 && e.Delta.Y != 0.0)
        {
            int direction = e.Delta.Y < 0.0 ? 1 : -1;
            int steps = Math.Clamp((int)Math.Round(Math.Abs(e.Delta.Y)), 1, 3);
            SelectIndex(_selectedIndex + direction * steps, notify: true);
            e.Handled = true;
        }
    }

    private void SelectIndex(int index, bool notify)
    {
        if (_options.Count == 0)
        {
            return;
        }
        int nextIndex = Math.Clamp(index, 0, _options.Count - 1);
        if (_selectedIndex != nextIndex)
        {
            _selectedIndex = nextIndex;
            SelectedValue = _options[_selectedIndex].Value;
            RefreshVisibleItems();
            if (notify)
            {
                ValueChanged?.Invoke(SelectedValue);
            }
        }
    }

    private void RefreshVisibleItems()
    {
        double nearOpacity = LuminaPickerResources.Double("LuminaWheelColumnNearOpacity", 0.72);
        double farOpacity = LuminaPickerResources.Double("LuminaWheelColumnFarOpacity", 0.42);
        double selectedFontSize = LuminaPickerResources.Double("LuminaWheelColumnSelectedFontSize", 20.0);
        double nearFontSize = LuminaPickerResources.Double("LuminaWheelColumnNearFontSize", 16.0);
        double farFontSize = LuminaPickerResources.Double("LuminaWheelColumnFarFontSize", 14.0);
        for (int rowIndex = 0; rowIndex < VisibleItemCount; rowIndex++)
        {
            int optionIndex = _selectedIndex + rowIndex - 2;
            Border row = _itemRows[rowIndex];
            TextBlock textBlock = _itemTexts[rowIndex];
            if (optionIndex < 0 || optionIndex >= _options.Count)
            {
                row.Opacity = 0.0;
                row.IsHitTestVisible = false;
                textBlock.Text = string.Empty;
                continue;
            }
            Border border = row;
            double opacity;
            if (rowIndex == 2)
            {
                opacity = 1.0;
            }
            else
            {
                opacity = rowIndex is 1 or 3 ? nearOpacity : farOpacity;
            }
            border.Opacity = opacity;
            row.IsHitTestVisible = true;
            textBlock.Text = _options[optionIndex].Label;
            if (rowIndex == 2)
            {
                textBlock.FontSize = selectedFontSize;
                textBlock.FontWeight = FontWeight.DemiBold;
                textBlock.Foreground = LuminaPickerResources.Brush("LuminaPrimaryBrush", Brushes.DodgerBlue);
            }
            else
            {
                textBlock.FontSize = rowIndex is 1 or 3 ? nearFontSize : farFontSize;
                textBlock.FontWeight = FontWeight.Medium;
                textBlock.Foreground = LuminaPickerResources.Brush("LuminaTextTertiaryBrush", Brushes.Gray);
            }
        }
    }

    private static int FindNearestIndex(IReadOnlyList<LuminaWheelOption> options, int preferredValue)
    {
        int selectedIndex = 0;
        int selectedDistance = Math.Abs(options[0].Value - preferredValue);
        for (int index = 1; index < options.Count; index++)
        {
            int distance = Math.Abs(options[index].Value - preferredValue);
            if (distance < selectedDistance)
            {
                selectedIndex = index;
                selectedDistance = distance;
            }
        }
        return selectedIndex;
    }

    private static RowDefinitions CreateItemRows(double itemHeight)
    {
        string rowHeight = itemHeight.ToString(CultureInfo.InvariantCulture);
        return new RowDefinitions(string.Join(",", Enumerable.Repeat(rowHeight, VisibleItemCount)));
    }
}
