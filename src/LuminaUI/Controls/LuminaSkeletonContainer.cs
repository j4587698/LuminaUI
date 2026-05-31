using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Threading;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaSkeletonContainer : ContentControl
{
    private ContentPresenter? _contentPresenter;

    private ContentPresenter? _skeletonPresenter;

    private object? _activeSkeletonContent;

    private IDataTemplate? _activeSkeletonContentTemplate;

    private bool _hasActiveSkeletonContent;

    private bool _refreshQueued;

    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<LuminaSkeletonContainer, bool>(nameof(IsLoading), defaultValue: false);

    public static readonly StyledProperty<LuminaSkeletonContainerMode> ModeProperty = AvaloniaProperty.Register<LuminaSkeletonContainer, LuminaSkeletonContainerMode>(nameof(Mode), LuminaSkeletonContainerMode.Auto);

    public static readonly StyledProperty<int> PlaceholderCountProperty = AvaloniaProperty.Register<LuminaSkeletonContainer, int>(nameof(PlaceholderCount), 4);

    public static readonly StyledProperty<object?> SkeletonContentProperty = AvaloniaProperty.Register<LuminaSkeletonContainer, object?>(nameof(SkeletonContent));

    public static readonly StyledProperty<IDataTemplate?> SkeletonContentTemplateProperty = AvaloniaProperty.Register<LuminaSkeletonContainer, IDataTemplate?>(nameof(SkeletonContentTemplate));

    public static readonly DirectProperty<LuminaSkeletonContainer, object?> ActiveSkeletonContentProperty = AvaloniaProperty.RegisterDirect("ActiveSkeletonContent", (LuminaSkeletonContainer container) => container.ActiveSkeletonContent);

    public static readonly DirectProperty<LuminaSkeletonContainer, IDataTemplate?> ActiveSkeletonContentTemplateProperty = AvaloniaProperty.RegisterDirect("ActiveSkeletonContentTemplate", (LuminaSkeletonContainer container) => container.ActiveSkeletonContentTemplate);

    public static readonly DirectProperty<LuminaSkeletonContainer, bool> HasActiveSkeletonContentProperty = AvaloniaProperty.RegisterDirect<LuminaSkeletonContainer, bool>(nameof(HasActiveSkeletonContent), (LuminaSkeletonContainer container) => container.HasActiveSkeletonContent, null, unsetValue: false);

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public LuminaSkeletonContainerMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public int PlaceholderCount
    {
        get => GetValue(PlaceholderCountProperty);
        set => SetValue(PlaceholderCountProperty, value);
    }

    public object? SkeletonContent
    {
        get => GetValue(SkeletonContentProperty);
        set => SetValue(SkeletonContentProperty, value);
    }

    public IDataTemplate? SkeletonContentTemplate
    {
        get => GetValue(SkeletonContentTemplateProperty);
        set => SetValue(SkeletonContentTemplateProperty, value);
    }

    public object? ActiveSkeletonContent
    {
        get
        {
            return _activeSkeletonContent;
        }
        private set
        {
            SetAndRaise(ActiveSkeletonContentProperty, ref _activeSkeletonContent, value);
        }
    }

    public IDataTemplate? ActiveSkeletonContentTemplate
    {
        get
        {
            return _activeSkeletonContentTemplate;
        }
        private set
        {
            SetAndRaise(ActiveSkeletonContentTemplateProperty, ref _activeSkeletonContentTemplate, value);
        }
    }

    public bool HasActiveSkeletonContent
    {
        get
        {
            return _hasActiveSkeletonContent;
        }
        private set
        {
            SetAndRaise(HasActiveSkeletonContentProperty, ref _hasActiveSkeletonContent, value);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _contentPresenter = e.NameScope.FindRequired<ContentPresenter>("PART_ContentPresenter");
        _skeletonPresenter = e.NameScope.FindRequired<ContentPresenter>("PART_SkeletonPresenter");
        UpdateSkeletonContent();
        UpdateLoadingVisualState();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateSkeletonContent();
        QueueAutoRefresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsLoadingProperty)
        {
            PseudoClasses.Set(":loading", IsLoading);
            UpdateSkeletonContent();
            QueueAutoRefresh();
        }
        else if (change.Property == ContentControl.ContentProperty || change.Property == ModeProperty || change.Property == PlaceholderCountProperty || change.Property == SkeletonContentProperty || change.Property == SkeletonContentTemplateProperty || change.Property == StyledElement.DataContextProperty)
        {
            UpdateSkeletonContent();
            QueueAutoRefresh();
        }
    }

    private void QueueAutoRefresh()
    {
        if (!IsLoading || Mode != LuminaSkeletonContainerMode.Auto || SkeletonContent != null || _refreshQueued)
        {
            return;
        }
        _refreshQueued = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            _refreshQueued = false;
            if (VisualRoot != null)
            {
                UpdateSkeletonContent();
            }
        }, DispatcherPriority.Loaded);
    }

    private void UpdateSkeletonContent()
    {
        object? content = null;
        IDataTemplate? template = null;
        if (IsLoading)
        {
            if (SkeletonContent != null)
            {
                content = SkeletonContent;
                template = SkeletonContentTemplate;
            }
            else if (Mode == LuminaSkeletonContainerMode.Auto)
            {
                content = BuildAutoSkeleton(Content);
            }
        }
        ActiveSkeletonContent = content;
        ActiveSkeletonContentTemplate = template;
        HasActiveSkeletonContent = content != null;
        UpdateLoadingVisualState();
    }

    private void UpdateLoadingVisualState()
    {
        bool shouldShowSkeleton = IsLoading && HasActiveSkeletonContent;
        if (_contentPresenter != null)
        {
            _contentPresenter.Opacity = shouldShowSkeleton ? 0 : 1;
            _contentPresenter.IsHitTestVisible = !shouldShowSkeleton;
        }
        if (_skeletonPresenter != null)
        {
            _skeletonPresenter.IsVisible = shouldShowSkeleton;
            _skeletonPresenter.IsHitTestVisible = shouldShowSkeleton;
        }
    }

    private Control BuildAutoSkeleton(object? content)
    {
        return content switch
        {
            Control control => BuildSkeletonForControl(control) ?? CreateSkeleton(null, LuminaSkeletonShape.Block, 240.0, 96.0),
            string text => CreateTextSkeleton(null, text, LuminaSkeletonShape.Text),
            _ => CreateSkeleton(null, LuminaSkeletonShape.Block, 240.0, 96.0)
        };
    }

    private Control? BuildSkeletonForControl(Control source)
    {
        if (Skeleton.GetIgnore(source))
        {
            return null;
        }
        LuminaSkeletonShape shape = Skeleton.GetShape(source);
        IDataTemplate? itemTemplate = Skeleton.GetItemTemplate(source);
        if (shape == LuminaSkeletonShape.Custom || itemTemplate != null)
        {
            return BuildCustomSkeleton(source, itemTemplate);
        }
        if (shape != LuminaSkeletonShape.Auto)
        {
            return shape == LuminaSkeletonShape.List ? BuildListSkeleton(source) : CreateSkeleton(source, shape, 160.0, 36.0);
        }
        return source switch
        {
            TextBlock textBlock => CreateTextSkeleton(textBlock, textBlock.Text, GuessTextShape(textBlock)),
            Image => CreateSkeleton(source, GuessImageShape(source), 96.0, 96.0),
            LuminaImage => CreateSkeleton(source, GuessImageShape(source), 96.0, 96.0),
            LuminaAvatar => CreateSkeleton(source, LuminaSkeletonShape.Circle, 44.0, 44.0),
            Button => CreateSkeleton(source, LuminaSkeletonShape.Rectangle, 96.0, 36.0),
            TextBox => CreateSkeleton(source, LuminaSkeletonShape.Rectangle, 220.0, 36.0),
            ComboBox => CreateSkeleton(source, LuminaSkeletonShape.Rectangle, 220.0, 36.0),
            ItemsControl => BuildListSkeleton(source),
            Border border => BuildBorderSkeleton(border),
            Grid grid => BuildGridSkeleton(grid),
            StackPanel stackPanel => BuildStackPanelSkeleton(stackPanel),
            Panel panel => BuildPanelSkeleton(panel),
            LuminaCard card => BuildCardSkeleton(card),
            ContentControl contentControl => BuildContentControlSkeleton(contentControl),
            _ => CreateSkeleton(source, LuminaSkeletonShape.Rectangle, 160.0, 32.0)
        };
    }

    private Control BuildCustomSkeleton(Control source, IDataTemplate? itemTemplate)
    {
        if (itemTemplate == null)
        {
            return CreateSkeleton(source, LuminaSkeletonShape.Rectangle, 160.0, 32.0);
        }
        if (source is ItemsControl)
        {
            StackPanel panel = new StackPanel
            {
                Spacing = 10.0
            };
            CopyLayout(source, panel);
            int count = GetPlaceholderCount(source);
            for (int i = 0; i < count; i++)
            {
                Control? item = BuildTemplateContent(itemTemplate, i);
                if (item != null)
                {
                    panel.Children.Add(item);
                }
            }
            return panel;
        }
        Control? content = BuildTemplateContent(itemTemplate, source.DataContext ?? source);
        return (content != null) ? CopyLayoutAndReturn(source, content) : CreateSkeleton(source, LuminaSkeletonShape.Rectangle, 160.0, 32.0);
    }

    private static Control? BuildTemplateContent(IDataTemplate itemTemplate, object? data)
    {
        return itemTemplate.Build(data);
    }

    private Control BuildListSkeleton(Control source)
    {
        StackPanel panel = new StackPanel
        {
            Spacing = 10.0
        };
        CopyLayout(source, panel);
        int count = GetPlaceholderCount(source);
        LuminaSkeletonShape itemShape = Skeleton.GetItemShape(source);
        for (int i = 0; i < count; i++)
        {
            panel.Children.Add(CreateListItemSkeleton(itemShape));
        }
        return panel;
    }

    private Control CreateListItemSkeleton(LuminaSkeletonShape itemShape)
    {
        Control result = itemShape switch
        {
            LuminaSkeletonShape.Text => CreateSkeleton(null, LuminaSkeletonShape.Text, 220.0, 14.0), 
            LuminaSkeletonShape.Title => CreateSkeleton(null, LuminaSkeletonShape.Title, 180.0, 22.0), 
            LuminaSkeletonShape.Block => CreateSkeleton(null, LuminaSkeletonShape.Block, 320.0, 72.0), 
            LuminaSkeletonShape.Card => new LuminaCard
            {
                Content = CreateAvatarTextSkeleton(),
                Margin = new Thickness(0.0, 0.0, 0.0, 2.0)
            }, 
            _ => CreateAvatarTextSkeleton(), 
        };
        return result;
    }

    private static Control CreateAvatarTextSkeleton()
    {
        Grid root = new Grid
        {
            ColumnDefinitions = 
            {
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                },
                new ColumnDefinition
                {
                    Width = new GridLength(1.0, GridUnitType.Star)
                }
            },
            ColumnSpacing = 12.0,
            Margin = new Thickness(0.0, 2.0)
        };
        LuminaSkeleton avatar = new LuminaSkeleton
        {
            Classes = { "Circle" }
        };
        StackPanel text = new StackPanel
        {
            Spacing = 8.0,
            VerticalAlignment = VerticalAlignment.Center
        };
        text.Children.Add(new LuminaSkeleton
        {
            Classes = { "Title" },
            Width = 180.0
        });
        text.Children.Add(new LuminaSkeleton
        {
            Classes = { "Text" },
            Width = 260.0
        });
        Grid.SetColumn(text, 1);
        root.Children.Add(avatar);
        root.Children.Add(text);
        return root;
    }

    private Control BuildBorderSkeleton(Border source)
    {
        Border border = new Border
        {
            Padding = source.Padding,
            CornerRadius = source.CornerRadius,
            Background = source.Background,
            BorderBrush = source.BorderBrush,
            BorderThickness = source.BorderThickness
        };
        CopyLayout(source, border);
        CopyClasses(source, border);
        Control? child = source.Child;
        border.Child = child != null ? BuildSkeletonForControl(child) : CreateSkeleton(null, LuminaSkeletonShape.Block, 240.0, 96.0);
        return border;
    }

    private Control BuildGridSkeleton(Grid source)
    {
        Grid grid = new Grid
        {
            RowSpacing = source.RowSpacing,
            ColumnSpacing = source.ColumnSpacing
        };
        CopyLayout(source, grid);
        foreach (RowDefinition row in source.RowDefinitions)
        {
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = row.Height,
                MinHeight = row.MinHeight,
                MaxHeight = row.MaxHeight
            });
        }
        foreach (ColumnDefinition column in source.ColumnDefinitions)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = column.Width,
                MinWidth = column.MinWidth,
                MaxWidth = column.MaxWidth
            });
        }
        foreach (Control child in source.Children)
        {
            Control? skeleton = BuildSkeletonForControl(child);
            if (skeleton != null)
            {
                Grid.SetRow(skeleton, Grid.GetRow(child));
                Grid.SetColumn(skeleton, Grid.GetColumn(child));
                Grid.SetRowSpan(skeleton, Grid.GetRowSpan(child));
                Grid.SetColumnSpan(skeleton, Grid.GetColumnSpan(child));
                grid.Children.Add(skeleton);
            }
        }
        return grid.Children.Count > 0 ? grid : CreateSkeleton(source, LuminaSkeletonShape.Block, 240.0, 96.0);
    }

    private Control BuildStackPanelSkeleton(StackPanel source)
    {
        StackPanel panel = new StackPanel
        {
            Orientation = source.Orientation,
            Spacing = source.Spacing
        };
        CopyLayout(source, panel);
        foreach (Control child in source.Children)
        {
            Control? skeleton = BuildSkeletonForControl(child);
            if (skeleton != null)
            {
                panel.Children.Add(skeleton);
            }
        }
        return panel.Children.Count > 0 ? panel : CreateSkeleton(source, LuminaSkeletonShape.Block, 240.0, 96.0);
    }

    private Control BuildPanelSkeleton(Panel source)
    {
        StackPanel panel = new StackPanel
        {
            Spacing = 8.0
        };
        CopyLayout(source, panel);
        foreach (Control child in source.Children)
        {
            Control? skeleton = BuildSkeletonForControl(child);
            if (skeleton != null)
            {
                panel.Children.Add(skeleton);
            }
        }
        return panel.Children.Count > 0 ? panel : CreateSkeleton(source, LuminaSkeletonShape.Block, 240.0, 96.0);
    }

    private Control BuildCardSkeleton(LuminaCard source)
    {
        LuminaCard card = new LuminaCard
        {
            IsElevated = source.IsElevated,
            Content = source.Content is Control child ? BuildSkeletonForControl(child) : CreateSkeleton(null, LuminaSkeletonShape.Block, 240.0, 96.0)
        };
        CopyLayout(source, card);
        CopyClasses(source, card);
        return card;
    }

    private Control BuildContentControlSkeleton(ContentControl source)
    {
        if (source.Content is Control child)
        {
            Control? childSkeleton = BuildSkeletonForControl(child);
            if (childSkeleton != null)
            {
                return CopyLayoutAndReturn(source, childSkeleton);
            }
        }
        return CreateSkeleton(source, LuminaSkeletonShape.Rectangle, 160.0, 32.0);
    }

    private static LuminaSkeletonShape GuessTextShape(TextBlock source)
    {
        return source.FontSize >= 16.0 || source.FontWeight.ToString().Contains("Bold", StringComparison.Ordinal) ? LuminaSkeletonShape.Title : LuminaSkeletonShape.Text;
    }

    private static LuminaSkeletonShape GuessImageShape(Control source)
    {
        double width = ResolveWidth(source, 96.0);
        double height = ResolveHeight(source, 96.0);
        return (Math.Abs(width - height) < 0.5) ? LuminaSkeletonShape.Circle : LuminaSkeletonShape.Rectangle;
    }

    private static Control CopyLayoutAndReturn(Control source, Control target)
    {
        CopyLayout(source, target);
        return target;
    }

    private static void CopyLayout(Control source, Control target)
    {
        target.Width = source.Width;
        target.Height = source.Height;
        target.MinWidth = source.MinWidth;
        target.MinHeight = source.MinHeight;
        target.MaxWidth = source.MaxWidth;
        target.MaxHeight = source.MaxHeight;
        target.Margin = source.Margin;
        target.HorizontalAlignment = source.HorizontalAlignment;
        target.VerticalAlignment = source.VerticalAlignment;
    }

    private static void CopyClasses(Control source, Control target)
    {
        foreach (string @class in source.Classes)
        {
            target.Classes.Add(@class);
        }
    }

    private static LuminaSkeleton CreateTextSkeleton(TextBlock? source, string? text, LuminaSkeletonShape shape)
    {
        double fontSize = source?.FontSize ?? 14.0;
        double estimatedWidth = string.IsNullOrWhiteSpace(text) ? 180.0 : Math.Clamp((double)text.Length * fontSize * 0.52, 64.0, 360.0);
        double height = Math.Clamp(fontSize * 1.35, 14.0, 26.0);
        return CreateSkeleton(source, shape, estimatedWidth, height);
    }

    private static LuminaSkeleton CreateSkeleton(Control? source, LuminaSkeletonShape shape, double defaultWidth, double defaultHeight)
    {
        LuminaSkeleton skeleton = new LuminaSkeleton();
        if (source != null)
        {
            CopyLayout(source, skeleton);
        }
        double width = ResolveWidth(source, defaultWidth);
        double height = ResolveHeight(source, defaultHeight);
        CornerRadius? cornerRadius = source == null ? null : Skeleton.GetCornerRadius(source);
        switch (shape)
        {
        case LuminaSkeletonShape.Title:
            skeleton.Classes.Add("Title");
            break;
        case LuminaSkeletonShape.Text:
            skeleton.Classes.Add("Text");
            break;
        case LuminaSkeletonShape.Circle:
        {
            skeleton.Classes.Add("Circle");
            double size = (skeleton.Width = Math.Min(width, height));
            skeleton.Height = size;
            return skeleton;
        }
        case LuminaSkeletonShape.Block:
        case LuminaSkeletonShape.Card:
            skeleton.Classes.Add("Block");
            break;
        }
        if (!double.IsNaN(width) && width > 0.0)
        {
            skeleton.Width = width;
        }
        if (!double.IsNaN(height) && height > 0.0)
        {
            skeleton.Height = height;
        }
        if (shape == LuminaSkeletonShape.Block || shape == LuminaSkeletonShape.Card)
        {
            skeleton.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        if (cornerRadius.HasValue)
        {
            skeleton.CornerRadius = cornerRadius.Value;
        }
        return skeleton;
    }

    private static double ResolveWidth(Control? source, double fallback)
    {
        if (source == null)
        {
            return fallback;
        }
        double attached = Skeleton.GetWidth(source);
        if (!double.IsNaN(attached))
        {
            return attached;
        }
        if (!double.IsNaN(source.Width) && source.Width > 0.0)
        {
            return source.Width;
        }
        return (source.Bounds.Width > 0.0) ? source.Bounds.Width : fallback;
    }

    private static double ResolveHeight(Control? source, double fallback)
    {
        if (source == null)
        {
            return fallback;
        }
        double attached = Skeleton.GetHeight(source);
        if (!double.IsNaN(attached))
        {
            return attached;
        }
        if (!double.IsNaN(source.Height) && source.Height > 0.0)
        {
            return source.Height;
        }
        return (source.Bounds.Height > 0.0) ? source.Bounds.Height : fallback;
    }

    private int GetPlaceholderCount(Control source)
    {
        int attached = Skeleton.GetPlaceholderCount(source);
        int count = (attached > 0) ? attached : PlaceholderCount;
        return Math.Clamp(count, 1, 50);
    }
}
