using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

[PseudoClasses(":first", ":last", ":current", ":finished", ":wait", ":error", ":vertical")]
public class LuminaStepItem : HeaderedContentControl
{
    public const string PC_First = ":first";

    public const string PC_Last = ":last";

    public const string PC_Current = ":current";

    public const string PC_Finished = ":finished";

    public const string PC_Wait = ":wait";

    public const string PC_Error = ":error";

    public const string PC_Vertical = ":vertical";

    public static readonly StyledProperty<LuminaStepStatus> StatusProperty;

    public static readonly StyledProperty<object?> IconProperty;

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

    public static readonly StyledProperty<int> StepNumberProperty;

    public static readonly DirectProperty<LuminaStepItem, bool> IsCurrentProperty;

    private bool _isCurrent;

    public LuminaStepStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

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

    public int StepNumber
    {
        get => GetValue(StepNumberProperty);
        set => SetValue(StepNumberProperty, value);
    }

    public bool IsCurrent
    {
        get
        {
            return _isCurrent;
        }
        private set
        {
            SetAndRaise(IsCurrentProperty, ref _isCurrent, value);
        }
    }

    static LuminaStepItem()
    {
        StatusProperty = AvaloniaProperty.Register<LuminaStepItem, LuminaStepStatus>(nameof(Status), LuminaStepStatus.Wait);
        IconProperty = AvaloniaProperty.Register<LuminaStepItem, object?>(nameof(Icon));
        IconTemplateProperty = AvaloniaProperty.Register<LuminaStepItem, IDataTemplate?>(nameof(IconTemplate));
        StepNumberProperty = AvaloniaProperty.Register<LuminaStepItem, int>(nameof(StepNumber), 0);
        IsCurrentProperty = AvaloniaProperty.RegisterDirect<LuminaStepItem, bool>(nameof(IsCurrent), (LuminaStepItem item) => item.IsCurrent, null, unsetValue: false);
        StatusProperty.Changed.AddClassHandler((LuminaStepItem item, AvaloniaPropertyChangedEventArgs<LuminaStepStatus> _) =>
        {
            item.UpdateStatusPseudoClass();
        });
        IconProperty.Changed.AddClassHandler((LuminaStepItem item, AvaloniaPropertyChangedEventArgs<object?> _) =>
        {
            item.UpdateIconState();
        });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateStatusPseudoClass();
        UpdateIconState();
    }

    internal void SetEdgeState(bool isFirst, bool isLast)
    {
        PseudoClasses.Set(":first", isFirst);
        PseudoClasses.Set(":last", isLast);
    }

    internal void SetCurrentState(bool isCurrent)
    {
        IsCurrent = isCurrent;
        PseudoClasses.Set(":current", isCurrent);
    }

    internal void SetDirectionState(LuminaStepsDirection direction)
    {
        PseudoClasses.Set(":vertical", direction == LuminaStepsDirection.Vertical);
    }

    internal void SetIfUnset<T>(AvaloniaProperty<T> property, T value)
    {
        if (!IsSet(property))
        {
            SetCurrentValue(property, value);
        }
    }

    private void UpdateStatusPseudoClass()
    {
        PseudoClasses.Set(":finished", Status == LuminaStepStatus.Finish);
        PseudoClasses.Set(":wait", Status == LuminaStepStatus.Wait);
        PseudoClasses.Set(":error", Status == LuminaStepStatus.Error);
    }

    private void UpdateIconState()
    {
    }
}
