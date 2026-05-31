using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaButtonPresenter : ContentControl
{
    private Grid? _layoutRoot;

    private Control? _iconPresenter;

    private Control? _loadingPresenter;

    private Control? _contentPresenter;

    private bool _hasIcon;

    private bool _hasContent;

    private bool _showsIcon;

    private bool _showsLoading;

    private IBrush? _effectiveIconForeground;

    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<LuminaButtonPresenter, object?>(nameof(Icon));

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<LuminaButtonPresenter, IDataTemplate?>(nameof(IconTemplate));

    public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<LuminaButtonPresenter, IBrush?>(nameof(IconForeground));

    public static readonly StyledProperty<bool> SyncIconForegroundProperty = AvaloniaProperty.Register<LuminaButtonPresenter, bool>(nameof(SyncIconForeground), defaultValue: true);

    public static readonly StyledProperty<LuminaIconPlacement> IconPlacementProperty = AvaloniaProperty.Register<LuminaButtonPresenter, LuminaIconPlacement>(nameof(IconPlacement), LuminaIconPlacement.Left);

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<LuminaButtonPresenter, double>(nameof(IconSize), 16.0);

    public static readonly StyledProperty<double> IconSpacingProperty = AvaloniaProperty.Register<LuminaButtonPresenter, double>(nameof(IconSpacing), 8.0);

    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<LuminaButtonPresenter, bool>(nameof(IsLoading), defaultValue: false);

    public static readonly StyledProperty<LuminaLoadingKind> LoadingKindProperty = AvaloniaProperty.Register<LuminaButtonPresenter, LuminaLoadingKind>(nameof(LoadingKind), LuminaLoadingKind.Ring);

    public static readonly DirectProperty<LuminaButtonPresenter, bool> HasIconProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>(nameof(HasIcon), (LuminaButtonPresenter presenter) => presenter.HasIcon, null, unsetValue: false);

    public static readonly DirectProperty<LuminaButtonPresenter, bool> HasContentProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>(nameof(HasContent), (LuminaButtonPresenter presenter) => presenter.HasContent, null, unsetValue: false);

    public static readonly DirectProperty<LuminaButtonPresenter, bool> ShowsIconProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>(nameof(ShowsIcon), (LuminaButtonPresenter presenter) => presenter.ShowsIcon, null, unsetValue: false);

    public static readonly DirectProperty<LuminaButtonPresenter, bool> ShowsLoadingProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>(nameof(ShowsLoading), (LuminaButtonPresenter presenter) => presenter.ShowsLoading, null, unsetValue: false);

    public static readonly DirectProperty<LuminaButtonPresenter, IBrush?> EffectiveIconForegroundProperty = AvaloniaProperty.RegisterDirect("EffectiveIconForeground", (LuminaButtonPresenter presenter) => presenter.EffectiveIconForeground);

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IDataTemplate? IconTemplate
    {
        get => GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    public IBrush? IconForeground
    {
        get => GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public bool SyncIconForeground
    {
        get => GetValue(SyncIconForegroundProperty);
        set => SetValue(SyncIconForegroundProperty, value);
    }

    public LuminaIconPlacement IconPlacement
    {
        get => GetValue(IconPlacementProperty);
        set => SetValue(IconPlacementProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public double IconSpacing
    {
        get => GetValue(IconSpacingProperty);
        set => SetValue(IconSpacingProperty, value);
    }

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public LuminaLoadingKind LoadingKind
    {
        get => GetValue(LoadingKindProperty);
        set => SetValue(LoadingKindProperty, value);
    }

    public bool HasIcon
    {
        get
        {
            return _hasIcon;
        }
        private set
        {
            SetAndRaise(HasIconProperty, ref _hasIcon, value);
        }
    }

    public bool HasContent
    {
        get
        {
            return _hasContent;
        }
        private set
        {
            SetAndRaise(HasContentProperty, ref _hasContent, value);
        }
    }

    public bool ShowsIcon
    {
        get
        {
            return _showsIcon;
        }
        private set
        {
            SetAndRaise(ShowsIconProperty, ref _showsIcon, value);
        }
    }

    public bool ShowsLoading
    {
        get
        {
            return _showsLoading;
        }
        private set
        {
            SetAndRaise(ShowsLoadingProperty, ref _showsLoading, value);
        }
    }

    public IBrush? EffectiveIconForeground
    {
        get
        {
            return _effectiveIconForeground;
        }
        private set
        {
            SetAndRaise(EffectiveIconForegroundProperty, ref _effectiveIconForeground, value);
        }
    }

    protected override Type StyleKeyOverride => typeof(LuminaButtonPresenter);

    public LuminaButtonPresenter()
    {
        UpdateState();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _layoutRoot = e.NameScope.Find<Grid>("PART_LayoutRoot");
        _iconPresenter = e.NameScope.Find<Control>("PART_IconPresenter");
        _loadingPresenter = e.NameScope.Find<Control>("PART_LoadingPresenter");
        _contentPresenter = e.NameScope.Find<Control>("PART_ContentPresenter");
        UpdateState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty || change.Property == ContentControl.ContentProperty || change.Property == IconForegroundProperty || change.Property == SyncIconForegroundProperty || change.Property == IconPlacementProperty || change.Property == IconSizeProperty || change.Property == IconSpacingProperty || change.Property == IsLoadingProperty || change.Property == TemplatedControl.ForegroundProperty || change.Property == ContentControl.HorizontalContentAlignmentProperty || change.Property == ContentControl.VerticalContentAlignmentProperty)
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        HasIcon = Icon != null;
        HasContent = Content != null && (Content is not string text || !string.IsNullOrWhiteSpace(text));
        ShowsLoading = IsLoading;
        ShowsIcon = HasIcon && !ShowsLoading;
        EffectiveIconForeground = IconForeground ?? Foreground;
        ApplyIconForeground();
        UpdateButtonLayout();
    }

    private void ApplyIconForeground()
    {
        if (SyncIconForeground && Icon != null)
        {
            ApplyForegroundToIcon(Icon, EffectiveIconForeground);
        }
    }

    private void ApplyForegroundToIcon(object icon, IBrush? foreground)
    {
        if (icon is TemplatedControl templatedControl)
        {
            SetIconValue(templatedControl, TemplatedControl.ForegroundProperty, foreground);
            if (templatedControl is ContentControl contentControl)
            {
                object? content = contentControl.Content;
                if (content != null && content != contentControl)
                {
                    ApplyForegroundToIcon(content, foreground);
                }
            }
        }
        else if (icon is TextBlock textBlock)
        {
            SetIconValue(textBlock, TextBlock.ForegroundProperty, foreground);
        }
        else if (icon is ContentPresenter contentPresenter)
        {
            SetIconValue(contentPresenter, ContentPresenter.ForegroundProperty, foreground);
        }
        else if (icon is Shape shape)
        {
            SetIconValue(shape, Shape.FillProperty, foreground);
            SetIconValue(shape, Shape.StrokeProperty, foreground);
        }
        else if (icon is Panel panel)
        {
            foreach (Control child in panel.Children)
            {
                ApplyForegroundToIcon(child, foreground);
            }
        }
        else if (icon is Decorator decorator)
        {
            if (decorator.Child != null)
            {
                ApplyForegroundToIcon(decorator.Child, foreground);
            }
            else
            {
                SetIconValue(decorator, TextElement.ForegroundProperty, foreground);
            }
        }
        else if (icon is Control control)
        {
            SetIconValue(control, TextElement.ForegroundProperty, foreground);
        }
    }

    private static void SetIconValue<T>(AvaloniaObject target, StyledProperty<T> property, T value)
    {
        target.SetValue(property, value);
    }

    private void UpdateButtonLayout()
    {
        if (_layoutRoot == null || _iconPresenter == null || _loadingPresenter == null || _contentPresenter == null)
        {
            return;
        }
        bool hasBoth = (ShowsIcon || ShowsLoading) && HasContent;
        LuminaIconPlacement iconPlacement = IconPlacement;
        bool isHorizontal = iconPlacement is LuminaIconPlacement.Left or LuminaIconPlacement.Right;
        bool isIconFirst = iconPlacement is LuminaIconPlacement.Left or LuminaIconPlacement.Top;
        double spacing = hasBoth ? Math.Max(0.0, IconSpacing) : 0.0;
        double iconSize = Math.Max(0.0, IconSize);
        _iconPresenter.Width = iconSize;
        _iconPresenter.Height = iconSize;
        _loadingPresenter.Width = iconSize;
        _loadingPresenter.Height = iconSize;
        bool stretchContentH = HorizontalContentAlignment == HorizontalAlignment.Stretch;
        bool stretchContentV = VerticalContentAlignment == VerticalAlignment.Stretch;
        GridLength contentColumnWidth = stretchContentH ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto;
        GridLength contentRowHeight = stretchContentV ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto;
        _layoutRoot.ColumnDefinitions.Clear();
        _layoutRoot.RowDefinitions.Clear();
        if (isHorizontal)
        {
            _layoutRoot.RowDefinitions.Add(new RowDefinition
            {
                Height = contentRowHeight
            });
            int iconColumn = (hasBoth && !isIconFirst) ? 1 : 0;
            int contentColumn = (hasBoth && isIconFirst) ? 1 : 0;
            int columnCount = hasBoth ? 2 : 1;
            for (int i = 0; i < columnCount; i++)
            {
                _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = (i == contentColumn) ? contentColumnWidth : GridLength.Auto
                });
            }
            MoveElement(_iconPresenter, 0, iconColumn);
            MoveElement(_loadingPresenter, 0, iconColumn);
            MoveElement(_contentPresenter, 0, contentColumn);
            Thickness iconMargin = !hasBoth ? default : (isIconFirst ? new Thickness(0.0, 0.0, spacing, 0.0) : new Thickness(spacing, 0.0, 0.0, 0.0));
            _iconPresenter.Margin = iconMargin;
            _loadingPresenter.Margin = iconMargin;
            _contentPresenter.Margin = default;
        }
        else
        {
            _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = contentColumnWidth
            });
            int iconRow = (hasBoth && !isIconFirst) ? 1 : 0;
            int contentRow = (hasBoth && isIconFirst) ? 1 : 0;
            int rowCount = hasBoth ? 2 : 1;
            for (int i = 0; i < rowCount; i++)
            {
                _layoutRoot.RowDefinitions.Add(new RowDefinition
                {
                    Height = (i == contentRow) ? contentRowHeight : GridLength.Auto
                });
            }
            MoveElement(_iconPresenter, iconRow, 0);
            MoveElement(_loadingPresenter, iconRow, 0);
            MoveElement(_contentPresenter, contentRow, 0);
            Thickness iconMargin = !hasBoth ? default : (isIconFirst ? new Thickness(0.0, 0.0, 0.0, spacing) : new Thickness(0.0, spacing, 0.0, 0.0));
            _iconPresenter.Margin = iconMargin;
            _loadingPresenter.Margin = iconMargin;
            _contentPresenter.Margin = default;
        }
    }

    private static void MoveElement(Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
    }
}
