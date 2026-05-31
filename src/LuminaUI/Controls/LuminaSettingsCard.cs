using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaSettingsCard : ItemsControl
{
    private bool _hasHeader;

    private bool _hasDescription;

    public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<LuminaSettingsCard, string?>(nameof(Header));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaSettingsCard, string?>(nameof(Description));

    public static readonly DirectProperty<LuminaSettingsCard, bool> HasHeaderProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsCard, bool>(nameof(HasHeader), (LuminaSettingsCard card) => card.HasHeader, null, unsetValue: false);

    public static readonly DirectProperty<LuminaSettingsCard, bool> HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaSettingsCard, bool>(nameof(HasDescription), (LuminaSettingsCard card) => card.HasDescription, null, unsetValue: false);

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

    public bool HasHeader
    {
        get
        {
            return _hasHeader;
        }
        private set
        {
            SetAndRaise(HasHeaderProperty, ref _hasHeader, value);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HeaderProperty)
        {
            HasHeader = !string.IsNullOrWhiteSpace(Header);
        }
        else if (change.Property == DescriptionProperty)
        {
            HasDescription = !string.IsNullOrWhiteSpace(Description);
        }
    }
}
