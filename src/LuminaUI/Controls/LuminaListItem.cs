using Avalonia;
using Avalonia.Controls.Primitives;

namespace LuminaUI.Controls;

public class LuminaListItem : TemplatedControl
{
    private bool _hasIcon;

    private bool _hasDescription;

    private bool _hasValue;

    public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<LuminaListItem, string?>(nameof(Header));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaListItem, string?>(nameof(Description));

    public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<LuminaListItem, string?>(nameof(Value));

    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<LuminaListItem, string?>(nameof(Icon));

    public static readonly DirectProperty<LuminaListItem, bool> HasIconProperty = AvaloniaProperty.RegisterDirect<LuminaListItem, bool>(nameof(HasIcon), (LuminaListItem item) => item.HasIcon, null, unsetValue: false);

    public static readonly DirectProperty<LuminaListItem, bool> HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaListItem, bool>(nameof(HasDescription), (LuminaListItem item) => item.HasDescription, null, unsetValue: false);

    public static readonly DirectProperty<LuminaListItem, bool> HasValueProperty = AvaloniaProperty.RegisterDirect<LuminaListItem, bool>(nameof(HasValue), (LuminaListItem item) => item.HasValue, null, unsetValue: false);

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
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

    public bool HasDescription
    {
        get
        {
            return _hasDescription;
        }
        private set
        {
            SetAndRaise(HasDescriptionProperty, ref _hasDescription, value);
        }
    }

    public bool HasValue
    {
        get
        {
            return _hasValue;
        }
        private set
        {
            SetAndRaise(HasValueProperty, ref _hasValue, value);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty)
        {
            HasIcon = !string.IsNullOrWhiteSpace(Icon);
        }
        else if (change.Property == DescriptionProperty)
        {
            HasDescription = !string.IsNullOrWhiteSpace(Description);
        }
        else if (change.Property == ValueProperty)
        {
            HasValue = !string.IsNullOrWhiteSpace(Value);
        }
    }
}
