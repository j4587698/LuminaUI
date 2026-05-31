using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaLoadingContainer : ContentControl
{
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<LuminaLoadingContainer, bool>(nameof(IsLoading), defaultValue: false);

    public static readonly StyledProperty<object?> IndicatorProperty = AvaloniaProperty.Register<LuminaLoadingContainer, object?>(nameof(Indicator));

    public static readonly StyledProperty<object?> LoadingMessageProperty = AvaloniaProperty.Register<LuminaLoadingContainer, object?>(nameof(LoadingMessage));

    public static readonly StyledProperty<LuminaLoadingKind> LoadingKindProperty = AvaloniaProperty.Register<LuminaLoadingContainer, LuminaLoadingKind>(nameof(LoadingKind), LuminaLoadingKind.Bar);

    public static readonly StyledProperty<IDataTemplate?> LoadingMessageTemplateProperty = AvaloniaProperty.Register<LuminaLoadingContainer, IDataTemplate?>(nameof(LoadingMessageTemplate));

    public static readonly StyledProperty<IBrush?> OverlayBackgroundProperty = AvaloniaProperty.Register<LuminaLoadingContainer, IBrush?>(nameof(OverlayBackground));

    public static readonly StyledProperty<IBrush?> MessageForegroundProperty = AvaloniaProperty.Register<LuminaLoadingContainer, IBrush?>(nameof(MessageForeground));

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public object? Indicator
    {
        get => GetValue(IndicatorProperty);
        set => SetValue(IndicatorProperty, value);
    }

    public object? LoadingMessage
    {
        get => GetValue(LoadingMessageProperty);
        set => SetValue(LoadingMessageProperty, value);
    }

    public LuminaLoadingKind LoadingKind
    {
        get => GetValue(LoadingKindProperty);
        set => SetValue(LoadingKindProperty, value);
    }

    public IDataTemplate? LoadingMessageTemplate
    {
        get => GetValue(LoadingMessageTemplateProperty);
        set => SetValue(LoadingMessageTemplateProperty, value);
    }

    public IBrush? OverlayBackground
    {
        get => GetValue(OverlayBackgroundProperty);
        set => SetValue(OverlayBackgroundProperty, value);
    }

    public IBrush? MessageForeground
    {
        get => GetValue(MessageForegroundProperty);
        set => SetValue(MessageForegroundProperty, value);
    }
}
