using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace LuminaUI.Controls;

public class LuminaDrawer : ContentControl
{
    private Thickness _effectiveContentPadding = new Thickness(24);

    public static readonly StyledProperty<DrawerPlacement> PlacementProperty = AvaloniaProperty.Register<LuminaDrawer, DrawerPlacement>(nameof(Placement), DrawerPlacement.Right);

    public static readonly StyledProperty<double> DrawerLengthProperty = AvaloniaProperty.Register<LuminaDrawer, double>(nameof(DrawerLength), 360.0);

    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<LuminaDrawer, Thickness>(nameof(ContentPadding), new Thickness(24));

    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty = AvaloniaProperty.Register<LuminaDrawer, Thickness>(nameof(SafeAreaPadding));

    public static readonly DirectProperty<LuminaDrawer, Thickness> EffectiveContentPaddingProperty = AvaloniaProperty.RegisterDirect<LuminaDrawer, Thickness>(nameof(EffectiveContentPadding), drawer => drawer.EffectiveContentPadding);

    public DrawerPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public double DrawerLength
    {
        get => GetValue(DrawerLengthProperty);
        set => SetValue(DrawerLengthProperty, value);
    }

    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public Thickness SafeAreaPadding
    {
        get => GetValue(SafeAreaPaddingProperty);
        set => SetValue(SafeAreaPaddingProperty, value);
    }

    public Thickness EffectiveContentPadding
    {
        get
        {
            return _effectiveContentPadding;
        }
        private set
        {
            SetAndRaise(EffectiveContentPaddingProperty, ref _effectiveContentPadding, value);
        }
    }

    public LuminaDrawer()
    {
        UpdatePlacementLayout();
        UpdateEffectiveContentPadding();
    }

    internal static LuminaDrawer? EnsureDrawer(object? content, DrawerPlacement placement = DrawerPlacement.Right)
    {
        if (content == null)
        {
            return null;
        }

        if (content is LuminaDrawer drawer)
        {
            return drawer;
        }

        return new LuminaDrawer
        {
            Placement = placement,
            Content = content
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PlacementProperty || change.Property == DrawerLengthProperty)
        {
            UpdatePlacementLayout();
        }
        else if (change.Property == ContentPaddingProperty || change.Property == SafeAreaPaddingProperty)
        {
            UpdateEffectiveContentPadding();
        }
    }

    private void UpdateEffectiveContentPadding()
    {
        Thickness padding = ContentPadding;
        Thickness safeAreaPadding = SafeAreaPadding;
        EffectiveContentPadding = new Thickness(
            padding.Left + safeAreaPadding.Left,
            padding.Top + safeAreaPadding.Top,
            padding.Right + safeAreaPadding.Right,
            padding.Bottom + safeAreaPadding.Bottom);
    }

    private void UpdatePlacementLayout()
    {
        DrawerPlacement placement = Placement;
        double length = DrawerLength;

        PseudoClasses.Set(":left", placement == DrawerPlacement.Left);
        PseudoClasses.Set(":right", placement == DrawerPlacement.Right);
        PseudoClasses.Set(":top", placement == DrawerPlacement.Top);
        PseudoClasses.Set(":bottom", placement == DrawerPlacement.Bottom);

        switch (placement)
        {
            case DrawerPlacement.Left:
                Width = length;
                Height = double.NaN;
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                break;
            case DrawerPlacement.Right:
                Width = length;
                Height = double.NaN;
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                break;
            case DrawerPlacement.Top:
                Width = double.NaN;
                Height = length;
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                break;
            case DrawerPlacement.Bottom:
                Width = double.NaN;
                Height = length;
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
                break;
        }
    }
}
