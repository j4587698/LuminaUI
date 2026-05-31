using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaColorSwatch : TemplatedControl
{
    private bool _hasDescription;

    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<LuminaColorSwatch, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaColorSwatch, string?>(nameof(Description));

    public static readonly StyledProperty<IBrush?> SwatchBrushProperty = AvaloniaProperty.Register<LuminaColorSwatch, IBrush?>(nameof(SwatchBrush));

    public static readonly DirectProperty<LuminaColorSwatch, bool> HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaColorSwatch, bool>(nameof(HasDescription), (LuminaColorSwatch swatch) => swatch.HasDescription, null, unsetValue: false);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public IBrush? SwatchBrush
    {
        get => GetValue(SwatchBrushProperty);
        set => SetValue(SwatchBrushProperty, value);
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
        if (change.Property == DescriptionProperty)
        {
            HasDescription = !string.IsNullOrWhiteSpace(Description);
        }
    }
}
