using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaFormItem : ContentControl
{
    public static readonly StyledProperty<object?> LabelProperty;

    public static readonly StyledProperty<string?> DescriptionProperty;

    public static readonly StyledProperty<bool> IsRequiredProperty;

    public static readonly StyledProperty<bool> NoLabelProperty;

    public static readonly StyledProperty<LuminaFormLabelPosition> LabelPositionProperty;

    public static readonly StyledProperty<GridLength> LabelWidthProperty;

    public static readonly DirectProperty<LuminaFormItem, bool> HasLabelProperty;

    private bool _hasLabel;

    public static readonly DirectProperty<LuminaFormItem, bool> HasDescriptionProperty;

    private bool _hasDescription;

    public static readonly DirectProperty<LuminaFormItem, double> EffectiveLabelWidthProperty;

    private double _effectiveLabelWidth = double.NaN;

    public object? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool IsRequired
    {
        get => GetValue(IsRequiredProperty);
        set => SetValue(IsRequiredProperty, value);
    }

    public bool NoLabel
    {
        get => GetValue(NoLabelProperty);
        set => SetValue(NoLabelProperty, value);
    }

    public LuminaFormLabelPosition LabelPosition
    {
        get => GetValue(LabelPositionProperty);
        set => SetValue(LabelPositionProperty, value);
    }

    public GridLength LabelWidth
    {
        get => GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public bool HasLabel
    {
        get
        {
            return _hasLabel;
        }
        private set
        {
            SetAndRaise(HasLabelProperty, ref _hasLabel, value);
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

    public double EffectiveLabelWidth
    {
        get
        {
            return _effectiveLabelWidth;
        }
        private set
        {
            SetAndRaise(EffectiveLabelWidthProperty, ref _effectiveLabelWidth, value);
        }
    }

    static LuminaFormItem()
    {
        LabelProperty = AvaloniaProperty.Register<LuminaFormItem, object?>(nameof(Label));
        DescriptionProperty = AvaloniaProperty.Register<LuminaFormItem, string?>(nameof(Description));
        IsRequiredProperty = AvaloniaProperty.Register<LuminaFormItem, bool>(nameof(IsRequired), defaultValue: false);
        NoLabelProperty = AvaloniaProperty.Register<LuminaFormItem, bool>(nameof(NoLabel), defaultValue: false);
        LabelPositionProperty = AvaloniaProperty.Register<LuminaFormItem, LuminaFormLabelPosition>(nameof(LabelPosition), LuminaFormLabelPosition.Top);
        LabelWidthProperty = AvaloniaProperty.Register<LuminaFormItem, GridLength>(nameof(LabelWidth), GridLength.Auto);
        HasLabelProperty = AvaloniaProperty.RegisterDirect<LuminaFormItem, bool>(nameof(HasLabel), (LuminaFormItem item) => item.HasLabel, null, unsetValue: false);
        HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaFormItem, bool>(nameof(HasDescription), (LuminaFormItem item) => item.HasDescription, null, unsetValue: false);
        EffectiveLabelWidthProperty = AvaloniaProperty.RegisterDirect<LuminaFormItem, double>(nameof(EffectiveLabelWidth), (LuminaFormItem item) => item.EffectiveLabelWidth, null, 0.0);
        LabelProperty.Changed.AddClassHandler((LuminaFormItem item, AvaloniaPropertyChangedEventArgs _) =>
        {
            item.UpdateState();
        });
        DescriptionProperty.Changed.AddClassHandler((LuminaFormItem item, AvaloniaPropertyChangedEventArgs _) =>
        {
            item.UpdateState();
        });
        NoLabelProperty.Changed.AddClassHandler((LuminaFormItem item, AvaloniaPropertyChangedEventArgs _) =>
        {
            item.UpdateState();
        });
        LabelPositionProperty.Changed.AddClassHandler((LuminaFormItem item, AvaloniaPropertyChangedEventArgs _) =>
        {
            item.UpdateState();
        });
        LabelWidthProperty.Changed.AddClassHandler((LuminaFormItem item, AvaloniaPropertyChangedEventArgs _) =>
        {
            item.UpdateState();
        });
    }

    public LuminaFormItem()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        HasLabel = !NoLabel && Label != null;
        HasDescription = !string.IsNullOrWhiteSpace(Description);
        EffectiveLabelWidth = (LabelPosition == LuminaFormLabelPosition.Left && LabelWidth.IsAbsolute) ? LabelWidth.Value : double.NaN;
    }
}
