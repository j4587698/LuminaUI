using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace LuminaUI.Controls;

public class LuminaFormGroup : HeaderedItemsControl
{
    public static readonly StyledProperty<string?> DescriptionProperty;

    public static readonly StyledProperty<LuminaFormLabelPosition> LabelPositionProperty;

    public static readonly StyledProperty<GridLength> LabelWidthProperty;

    public static readonly StyledProperty<double> ItemSpacingProperty;

    public static readonly DirectProperty<LuminaFormGroup, bool> HasHeaderProperty;

    private bool _hasHeader;

    public static readonly DirectProperty<LuminaFormGroup, bool> HasDescriptionProperty;

    private bool _hasDescription;

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
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

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
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

    static LuminaFormGroup()
    {
        DescriptionProperty = AvaloniaProperty.Register<LuminaFormGroup, string?>(nameof(Description));
        LabelPositionProperty = AvaloniaProperty.Register<LuminaFormGroup, LuminaFormLabelPosition>(nameof(LabelPosition), LuminaFormLabelPosition.Top);
        LabelWidthProperty = AvaloniaProperty.Register<LuminaFormGroup, GridLength>(nameof(LabelWidth), GridLength.Auto);
        ItemSpacingProperty = AvaloniaProperty.Register<LuminaFormGroup, double>(nameof(ItemSpacing), 14.0);
        HasHeaderProperty = AvaloniaProperty.RegisterDirect<LuminaFormGroup, bool>(nameof(HasHeader), (LuminaFormGroup group) => group.HasHeader, null, unsetValue: false);
        HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaFormGroup, bool>(nameof(HasDescription), (LuminaFormGroup group) => group.HasDescription, null, unsetValue: false);
        HeaderedItemsControl.HeaderProperty.Changed.AddClassHandler((LuminaFormGroup group, AvaloniaPropertyChangedEventArgs _) =>
        {
            group.UpdateState();
        });
        DescriptionProperty.Changed.AddClassHandler((LuminaFormGroup group, AvaloniaPropertyChangedEventArgs _) =>
        {
            group.UpdateState();
        });
    }

    public LuminaFormGroup()
    {
        UpdateState();
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not LuminaFormItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        LuminaFormItem formItem = new LuminaFormItem();
        if (item is Control control)
        {
            formItem.Content = control;
            formItem.Label = LuminaForm.GetLabel(control);
            formItem.Description = LuminaForm.GetDescription(control);
            formItem.IsRequired = LuminaForm.GetIsRequired(control);
            formItem.NoLabel = LuminaForm.GetNoLabel(control);
        }
        else
        {
            formItem.Content = item;
        }
        return formItem;
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is LuminaFormItem formItem)
        {
            formItem.SetCurrentValue(LuminaFormItem.LabelPositionProperty, LabelPosition);
            formItem.SetCurrentValue(LuminaFormItem.LabelWidthProperty, LabelWidth);
        }
    }

    private void UpdateState()
    {
        HasHeader = Header != null;
        HasDescription = !string.IsNullOrWhiteSpace(Description);
    }
}
