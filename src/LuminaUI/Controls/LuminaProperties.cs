using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;

namespace LuminaUI.Controls;

public class LuminaProperties : ItemsControl
{
    public static readonly StyledProperty<BindingBase?> PropertyNameMemberBindingProperty = AvaloniaProperty.Register<LuminaProperties, BindingBase?>(nameof(PropertyNameMemberBinding));

    public static readonly StyledProperty<IDataTemplate?> PropertyNameTemplateProperty = AvaloniaProperty.Register<LuminaProperties, IDataTemplate?>(nameof(PropertyNameTemplate));

    public static readonly StyledProperty<double> LabelWidthProperty = AvaloniaProperty.Register<LuminaProperties, double>(nameof(LabelWidth), 150.0);

    public static readonly StyledProperty<bool> ShowBorderLinesProperty = AvaloniaProperty.Register<LuminaProperties, bool>(nameof(ShowBorderLines), defaultValue: true);

    [AssignBinding]
    [InheritDataTypeFromItems("ItemsSource")]
    public BindingBase? PropertyNameMemberBinding
    {
        get => GetValue(PropertyNameMemberBindingProperty);
        set => SetValue(PropertyNameMemberBindingProperty, value);
    }

    [InheritDataTypeFromItems("ItemsSource")]
    public IDataTemplate? PropertyNameTemplate
    {
        get => GetValue(PropertyNameTemplateProperty);
        set => SetValue(PropertyNameTemplateProperty, value);
    }

    public double LabelWidth
    {
        get => GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public bool ShowBorderLines
    {
        get => GetValue(ShowBorderLinesProperty);
        set => SetValue(ShowBorderLinesProperty, value);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not LuminaPropertyItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return (item as LuminaPropertyItem) ?? new LuminaPropertyItem();
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is not LuminaPropertyItem propertyItem)
        {
            return;
        }
        propertyItem.SetIfUnset(LuminaPropertyItem.LabelWidthProperty, LabelWidth);
        propertyItem.SetIfUnset(LuminaPropertyItem.PropertyNameTemplateProperty, PropertyNameTemplate);
        propertyItem.SetIfUnset(LuminaPropertyItem.ShowBorderLinesProperty, ShowBorderLines);
        if (item is not LuminaPropertyItem)
        {
            if (!propertyItem.IsSet(ContentControl.ContentTemplateProperty) && ItemTemplate != null)
            {
                propertyItem.SetCurrentValue(ContentControl.ContentTemplateProperty, ItemTemplate);
            }
            if (PropertyNameMemberBinding != null && !propertyItem.IsSet(LuminaPropertyItem.PropertyNameProperty))
            {
                propertyItem.Bind(LuminaPropertyItem.PropertyNameProperty, PropertyNameMemberBinding);
            }
            else if (!propertyItem.IsSet(LuminaPropertyItem.PropertyNameProperty))
            {
                propertyItem.SetCurrentValue(LuminaPropertyItem.PropertyNameProperty, item);
            }
            if (DisplayMemberBinding != null && !propertyItem.IsSet(ContentControl.ContentProperty))
            {
                propertyItem.Bind(ContentControl.ContentProperty, DisplayMemberBinding);
            }
            else if (!propertyItem.IsSet(ContentControl.ContentProperty))
            {
                propertyItem.SetCurrentValue(ContentControl.ContentProperty, item);
            }
        }
    }
}
