using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

[PseudoClasses(":first", ":last", ":empty-icon", ":left", ":right", ":separate")]
public class LuminaTimelineItem : HeaderedContentControl
{
    public const string PC_First = ":first";

    public const string PC_Last = ":last";

    public const string PC_EmptyIcon = ":empty-icon";

    public const string PC_Left = ":left";

    public const string PC_Right = ":right";

    public const string PC_Separate = ":separate";

    public static readonly StyledProperty<object?> IconProperty;

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

    public static readonly StyledProperty<LuminaTimelineItemStatus> StatusProperty;

    public static readonly StyledProperty<LuminaTimelineItemPosition> PositionProperty;

    public static readonly StyledProperty<object?> TimeProperty;

    public static readonly StyledProperty<string?> TimeFormatProperty;

    public static readonly DirectProperty<LuminaTimelineItem, string> DisplayTimeProperty;

    private string _displayTime = string.Empty;

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

    public LuminaTimelineItemStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public LuminaTimelineItemPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public object? Time
    {
        get => GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public string? TimeFormat
    {
        get => GetValue(TimeFormatProperty);
        set => SetValue(TimeFormatProperty, value);
    }

    public string DisplayTime
    {
        get
        {
            return _displayTime;
        }
        private set
        {
            SetAndRaise(DisplayTimeProperty, ref _displayTime, value);
        }
    }

    static LuminaTimelineItem()
    {
        IconProperty = AvaloniaProperty.Register<LuminaTimelineItem, object?>(nameof(Icon));
        IconTemplateProperty = AvaloniaProperty.Register<LuminaTimelineItem, IDataTemplate?>(nameof(IconTemplate));
        StatusProperty = AvaloniaProperty.Register<LuminaTimelineItem, LuminaTimelineItemStatus>(nameof(Status), LuminaTimelineItemStatus.Default);
        PositionProperty = AvaloniaProperty.Register<LuminaTimelineItem, LuminaTimelineItemPosition>(nameof(Position), LuminaTimelineItemPosition.Right);
        TimeProperty = AvaloniaProperty.Register<LuminaTimelineItem, object?>(nameof(Time));
        TimeFormatProperty = AvaloniaProperty.Register<LuminaTimelineItem, string?>(nameof(TimeFormat), "yyyy-MM-dd HH:mm");
        DisplayTimeProperty = AvaloniaProperty.RegisterDirect<LuminaTimelineItem, string>(nameof(DisplayTime), (LuminaTimelineItem item) => item.DisplayTime);
        IconProperty.Changed.AddClassHandler((LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<object?> _) =>
        {
            item.UpdateIconState();
        });
        PositionProperty.Changed.AddClassHandler((LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<LuminaTimelineItemPosition> _) =>
        {
            item.UpdatePositionState();
        });
        TimeProperty.Changed.AddClassHandler((LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<object?> _) =>
        {
            item.UpdateDisplayTime();
        });
        TimeFormatProperty.Changed.AddClassHandler((LuminaTimelineItem item, AvaloniaPropertyChangedEventArgs<string?> _) =>
        {
            item.UpdateDisplayTime();
        });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateIconState();
        UpdatePositionState();
        UpdateDisplayTime();
    }

    internal void SetEdgeState(bool isFirst, bool isLast)
    {
        PseudoClasses.Set(":first", isFirst);
        PseudoClasses.Set(":last", isLast);
    }

    internal void SetIfUnset<T>(AvaloniaProperty<T> property, T value)
    {
        if (!IsSet(property))
        {
            SetCurrentValue(property, value);
        }
    }

    private void UpdateIconState()
    {
        PseudoClasses.Set(":empty-icon", Icon == null);
    }

    private void UpdatePositionState()
    {
        PseudoClasses.Set(":left", Position == LuminaTimelineItemPosition.Left);
        PseudoClasses.Set(":right", Position == LuminaTimelineItemPosition.Right);
        PseudoClasses.Set(":separate", Position == LuminaTimelineItemPosition.Separate);
    }

    private void UpdateDisplayTime()
    {
        object? time = Time;
        string displayTime;
        if (time == null)
        {
            displayTime = string.Empty;
        }
        else if (!string.IsNullOrWhiteSpace(TimeFormat))
        {
            displayTime = time switch
            {
                DateTime dateTime => dateTime.ToString(TimeFormat),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(TimeFormat),
                IFormattable formattable => formattable.ToString(TimeFormat, null),
                _ => time.ToString() ?? string.Empty
            };
        }
        else
        {
            displayTime = time.ToString() ?? string.Empty;
        }
        DisplayTime = displayTime;
    }
}
