using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace LuminaUI.Controls;

public class LuminaSteps : ItemsControl
{
    public static readonly StyledProperty<int> CurrentProperty;

    public static readonly StyledProperty<LuminaStepsDirection> DirectionProperty;

    public static readonly StyledProperty<LuminaStepsSize> SizeProperty;

    public static readonly StyledProperty<bool> IsProgressDotProperty;

    public static readonly StyledProperty<BindingBase?> TitleMemberBindingProperty;

    public static readonly StyledProperty<BindingBase?> DescriptionMemberBindingProperty;

    public static readonly StyledProperty<BindingBase?> StatusMemberBindingProperty;

    public static readonly StyledProperty<BindingBase?> IconMemberBindingProperty;

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

    public int Current
    {
        get => GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public LuminaStepsDirection Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public LuminaStepsSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool IsProgressDot
    {
        get => GetValue(IsProgressDotProperty);
        set => SetValue(IsProgressDotProperty, value);
    }

    [AssignBinding]
    [InheritDataTypeFromItems("ItemsSource")]
    public BindingBase? TitleMemberBinding
    {
        get => GetValue(TitleMemberBindingProperty);
        set => SetValue(TitleMemberBindingProperty, value);
    }

    [AssignBinding]
    [InheritDataTypeFromItems("ItemsSource")]
    public BindingBase? DescriptionMemberBinding
    {
        get => GetValue(DescriptionMemberBindingProperty);
        set => SetValue(DescriptionMemberBindingProperty, value);
    }

    [AssignBinding]
    [InheritDataTypeFromItems("ItemsSource")]
    public BindingBase? StatusMemberBinding
    {
        get => GetValue(StatusMemberBindingProperty);
        set => SetValue(StatusMemberBindingProperty, value);
    }

    [AssignBinding]
    [InheritDataTypeFromItems("ItemsSource")]
    public BindingBase? IconMemberBinding
    {
        get => GetValue(IconMemberBindingProperty);
        set => SetValue(IconMemberBindingProperty, value);
    }

    [InheritDataTypeFromItems("ItemsSource")]
    public IDataTemplate? IconTemplate
    {
        get => GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    static LuminaSteps()
    {
        CurrentProperty = AvaloniaProperty.Register<LuminaSteps, int>(nameof(Current), 0);
        DirectionProperty = AvaloniaProperty.Register<LuminaSteps, LuminaStepsDirection>(nameof(Direction), LuminaStepsDirection.Horizontal);
        SizeProperty = AvaloniaProperty.Register<LuminaSteps, LuminaStepsSize>(nameof(Size), LuminaStepsSize.Default);
        IsProgressDotProperty = AvaloniaProperty.Register<LuminaSteps, bool>(nameof(IsProgressDot), defaultValue: false);
        TitleMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>(nameof(TitleMemberBinding));
        DescriptionMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>(nameof(DescriptionMemberBinding));
        StatusMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>(nameof(StatusMemberBinding));
        IconMemberBindingProperty = AvaloniaProperty.Register<LuminaSteps, BindingBase?>(nameof(IconMemberBinding));
        IconTemplateProperty = AvaloniaProperty.Register<LuminaSteps, IDataTemplate?>(nameof(IconTemplate));
        ItemsControl.ItemsPanelProperty.OverrideDefaultValue<LuminaSteps>(new FuncTemplate<Panel?>(() => new StackPanel
        {
            Orientation = Orientation.Horizontal
        }));
        CurrentProperty.Changed.AddClassHandler((LuminaSteps steps, AvaloniaPropertyChangedEventArgs _) =>
        {
            steps.RefreshStepContainers();
        });
        DirectionProperty.Changed.AddClassHandler((LuminaSteps steps, AvaloniaPropertyChangedEventArgs<LuminaStepsDirection> _) =>
        {
            steps.ItemsPanelProperty_Changed();
            steps.RefreshStepContainers();
        });
        IsProgressDotProperty.Changed.AddClassHandler((LuminaSteps steps, AvaloniaPropertyChangedEventArgs _) =>
        {
            steps.RefreshStepContainers();
        });
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not LuminaStepItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return (item as LuminaStepItem) ?? new LuminaStepItem();
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is LuminaStepItem stepItem)
        {
            stepItem.StepNumber = index + 1;
            if (TitleMemberBinding != null)
            {
                stepItem.Bind(HeaderedContentControl.HeaderProperty, TitleMemberBinding);
            }
            if (DescriptionMemberBinding != null)
            {
                stepItem.Bind(ContentControl.ContentProperty, DescriptionMemberBinding);
            }
            if (StatusMemberBinding != null)
            {
                stepItem.Bind(LuminaStepItem.StatusProperty, StatusMemberBinding);
            }
            if (IconMemberBinding != null)
            {
                stepItem.Bind(LuminaStepItem.IconProperty, IconMemberBinding);
            }
            stepItem.SetIfUnset(LuminaStepItem.IconTemplateProperty, IconTemplate);
            stepItem.SetIfUnset(HeaderedContentControl.HeaderTemplateProperty, ItemTemplate);
            if (item is not LuminaStepItem && DescriptionMemberBinding == null && stepItem.Content == null)
            {
                stepItem.Content = item;
            }
            ApplyItemState(stepItem, index);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        RefreshStepContainers();
        return base.ArrangeOverride(finalSize);
    }

    private void RefreshStepContainers()
    {
        if (ItemsPanelRoot != null)
        {
            LuminaStepItem[] items = ItemsPanelRoot.Children.OfType<LuminaStepItem>().ToArray();
            for (int i = 0; i < items.Length; i++)
            {
                items[i].SetEdgeState(i == 0, i == items.Length - 1);
                items[i].SetDirectionState(Direction);
                ApplyItemState(items[i], i);
            }
        }
    }

    private void ApplyItemState(LuminaStepItem item, int index)
    {
        if (StatusMemberBinding == null)
        {
            LuminaStepStatus status = index.CompareTo(Current) switch
            {
                < 0 => LuminaStepStatus.Finish,
                0 => LuminaStepStatus.Process,
                _ => LuminaStepStatus.Wait
            };
            if (item.Status != LuminaStepStatus.Error || index != Current)
            {
                item.SetCurrentValue(LuminaStepItem.StatusProperty, status);
            }
        }
        item.SetCurrentState(index == Current);
    }

    private void ItemsPanelProperty_Changed()
    {
        FuncTemplate<Panel?> panel = Direction == LuminaStepsDirection.Horizontal ? new FuncTemplate<Panel?>(() => new UniformGrid
        {
            Rows = 1
        }) : new FuncTemplate<Panel?>(() => new StackPanel
        {
            Orientation = Orientation.Vertical
        });
        SetCurrentValue(ItemsControl.ItemsPanelProperty, panel);
    }
}
