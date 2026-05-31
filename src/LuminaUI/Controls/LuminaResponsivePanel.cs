using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaResponsivePanel : Panel
{
    private readonly record struct ResponsiveLayout(int Columns, double ItemWidth, double HorizontalSpacing, double VerticalSpacing)
    {
        public double RowWidth => (double)Columns * ItemWidth + (double)Math.Max(0, Columns - 1) * HorizontalSpacing;
    }

    public static readonly StyledProperty<double> MinItemWidthProperty = AvaloniaProperty.Register<LuminaResponsivePanel, double>(nameof(MinItemWidth), 220.0);

    public static readonly StyledProperty<double> MaxItemWidthProperty = AvaloniaProperty.Register<LuminaResponsivePanel, double>(nameof(MaxItemWidth), double.PositiveInfinity);

    public static readonly StyledProperty<double> HorizontalSpacingProperty = AvaloniaProperty.Register<LuminaResponsivePanel, double>(nameof(HorizontalSpacing), 12.0);

    public static readonly StyledProperty<double> VerticalSpacingProperty = AvaloniaProperty.Register<LuminaResponsivePanel, double>(nameof(VerticalSpacing), 12.0);

    public static readonly StyledProperty<int> MaxColumnsProperty = AvaloniaProperty.Register<LuminaResponsivePanel, int>(nameof(MaxColumns), int.MaxValue);

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

    protected override Size MeasureOverride(Size availableSize)
    {
        Control[] visibleChildren = Children.Where(control => control.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
        {
            return default;
        }
        ResponsiveLayout layout = CalculateLayout(availableSize.Width, visibleChildren.Length);
        int rowCount = (int)Math.Ceiling((double)visibleChildren.Length / (double)layout.Columns);
        double[] rowHeights = new double[rowCount];
        for (int index = 0; index < visibleChildren.Length; index++)
        {
            Control child = visibleChildren[index];
            child.Measure(new Size(layout.ItemWidth, availableSize.Height));
            int row = index / layout.Columns;
            rowHeights[row] = Math.Max(rowHeights[row], child.DesiredSize.Height);
        }
        double desiredHeight = rowHeights.Sum() + (double)Math.Max(0, rowHeights.Length - 1) * layout.VerticalSpacing;
        double desiredWidth = double.IsInfinity(availableSize.Width) ? layout.RowWidth : availableSize.Width;
        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Control[] visibleChildren = Children.Where(control => control.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
        {
            return finalSize;
        }
        ResponsiveLayout layout = CalculateLayout(finalSize.Width, visibleChildren.Length);
        int rowCount = (int)Math.Ceiling((double)visibleChildren.Length / (double)layout.Columns);
        double[] rowHeights = new double[rowCount];
        for (int index = 0; index < visibleChildren.Length; index++)
        {
            int row = index / layout.Columns;
            rowHeights[row] = Math.Max(rowHeights[row], visibleChildren[index].DesiredSize.Height);
        }
        double y = 0.0;
        for (int row = 0; row < rowCount; row++)
        {
            int childrenInRow = Math.Min(layout.Columns, visibleChildren.Length - row * layout.Columns);
            double x = 0.0;
            for (int column = 0; column < childrenInRow; column++)
            {
                Control child = visibleChildren[row * layout.Columns + column];
                child.Arrange(new Rect(x, y, layout.ItemWidth, rowHeights[row]));
                x += layout.ItemWidth + layout.HorizontalSpacing;
            }
            y += rowHeights[row] + layout.VerticalSpacing;
        }
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MinItemWidthProperty || change.Property == MaxItemWidthProperty || change.Property == HorizontalSpacingProperty || change.Property == VerticalSpacingProperty || change.Property == MaxColumnsProperty)
        {
            InvalidateMeasure();
        }
    }

    private ResponsiveLayout CalculateLayout(double availableWidth, int childCount)
    {
        double minItemWidth = Math.Max(1.0, MinItemWidth);
        double horizontalSpacing = Math.Max(0.0, HorizontalSpacing);
        double verticalSpacing = Math.Max(0.0, VerticalSpacing);
        int maxColumns = (MaxColumns <= 0) ? int.MaxValue : MaxColumns;
        if (double.IsInfinity(availableWidth) || availableWidth <= 0.0)
        {
            int columnsForInfiniteWidth = Math.Max(1, Math.Min(childCount, maxColumns));
            double itemWidthForInfiniteWidth = GetConstrainedItemWidth(minItemWidth);
            return new ResponsiveLayout(columnsForInfiniteWidth, itemWidthForInfiniteWidth, horizontalSpacing, verticalSpacing);
        }
        int availableColumns = (int)Math.Floor((availableWidth + horizontalSpacing) / (minItemWidth + horizontalSpacing));
        int columns = Math.Max(1, Math.Min(Math.Min(availableColumns, childCount), maxColumns));
        double rawItemWidth = (availableWidth - (double)(columns - 1) * horizontalSpacing) / (double)columns;
        double itemWidth = GetConstrainedItemWidth(rawItemWidth);
        return new ResponsiveLayout(columns, itemWidth, horizontalSpacing, verticalSpacing);
    }

    private double GetConstrainedItemWidth(double itemWidth)
    {
        double minItemWidth = Math.Max(1.0, MinItemWidth);
        double maxItemWidth = (MaxItemWidth <= 0.0) ? double.PositiveInfinity : MaxItemWidth;
        return Math.Max(1.0, Math.Min(Math.Max(minItemWidth, itemWidth), maxItemWidth));
    }
}
