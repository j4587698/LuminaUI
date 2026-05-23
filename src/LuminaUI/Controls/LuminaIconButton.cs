using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

public enum LuminaIconPlacement
{
    Left,
    Right,
    Top,
    Bottom
}

public class LuminaIconButton : Button
{
    private Grid? _layoutRoot;
    private Control? _iconPresenter;
    private Control? _contentPresenter;
    private bool _hasIcon;
    private bool _hasContent;

    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<LuminaIconButton, object?>(nameof(Icon));

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty =
        AvaloniaProperty.Register<LuminaIconButton, IDataTemplate?>(nameof(IconTemplate));

    public IDataTemplate? IconTemplate
    {
        get => GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    public static readonly StyledProperty<LuminaIconPlacement> IconPlacementProperty =
        AvaloniaProperty.Register<LuminaIconButton, LuminaIconPlacement>(nameof(IconPlacement), LuminaIconPlacement.Left);

    public LuminaIconPlacement IconPlacement
    {
        get => GetValue(IconPlacementProperty);
        set => SetValue(IconPlacementProperty, value);
    }

    public static readonly StyledProperty<double> IconSpacingProperty =
        AvaloniaProperty.Register<LuminaIconButton, double>(nameof(IconSpacing), 8);

    public double IconSpacing
    {
        get => GetValue(IconSpacingProperty);
        set => SetValue(IconSpacingProperty, value);
    }

    public static readonly DirectProperty<LuminaIconButton, bool> HasIconProperty =
        AvaloniaProperty.RegisterDirect<LuminaIconButton, bool>(nameof(HasIcon), button => button.HasIcon);

    public bool HasIcon
    {
        get => _hasIcon;
        private set => SetAndRaise(HasIconProperty, ref _hasIcon, value);
    }

    public static readonly DirectProperty<LuminaIconButton, bool> HasContentProperty =
        AvaloniaProperty.RegisterDirect<LuminaIconButton, bool>(nameof(HasContent), button => button.HasContent);

    public bool HasContent
    {
        get => _hasContent;
        private set => SetAndRaise(HasContentProperty, ref _hasContent, value);
    }

    public LuminaIconButton()
    {
        UpdateState();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _layoutRoot = e.NameScope.FindRequired<Grid>("PART_LayoutRoot");
        _iconPresenter = e.NameScope.FindRequired<Control>("PART_IconPresenter");
        _contentPresenter = e.NameScope.FindRequired<Control>("PART_ContentPresenter");
        UpdateState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty ||
            change.Property == ContentProperty ||
            change.Property == IconPlacementProperty ||
            change.Property == IconSpacingProperty)
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        HasIcon = Icon != null;
        HasContent = Content is not null && (Content is not string text || !string.IsNullOrWhiteSpace(text));
        UpdateIconLayout();
    }

    private void UpdateIconLayout()
    {
        if (_layoutRoot == null || _iconPresenter == null || _contentPresenter == null)
        {
            return;
        }

        var isHorizontal = IconPlacement is LuminaIconPlacement.Left or LuminaIconPlacement.Right;
        var isIconFirst = IconPlacement is LuminaIconPlacement.Left or LuminaIconPlacement.Top;
        var spacing = HasIcon && HasContent ? Math.Max(0, IconSpacing) : 0;

        _layoutRoot.ColumnDefinitions.Clear();
        _layoutRoot.RowDefinitions.Clear();

        if (isHorizontal)
        {
            _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(_iconPresenter, 0);
            Grid.SetRow(_contentPresenter, 0);
            Grid.SetColumn(_iconPresenter, isIconFirst ? 0 : 1);
            Grid.SetColumn(_contentPresenter, isIconFirst ? 1 : 0);
            _iconPresenter.Margin = isIconFirst
                ? new Thickness(0, 0, spacing, 0)
                : new Thickness(spacing, 0, 0, 0);
        }
        else
        {
            _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(_iconPresenter, 0);
            Grid.SetColumn(_contentPresenter, 0);
            Grid.SetRow(_iconPresenter, isIconFirst ? 0 : 1);
            Grid.SetRow(_contentPresenter, isIconFirst ? 1 : 0);
            _iconPresenter.Margin = isIconFirst
                ? new Thickness(0, 0, 0, spacing)
                : new Thickness(0, spacing, 0, 0);
        }
    }
}
