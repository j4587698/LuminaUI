using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaLoadingContainer : ContentControl
{
	public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<LuminaLoadingContainer, bool>("IsLoading", defaultValue: false);

	public static readonly StyledProperty<object?> IndicatorProperty = AvaloniaProperty.Register<LuminaLoadingContainer, object?>("Indicator");

	public static readonly StyledProperty<object?> LoadingMessageProperty = AvaloniaProperty.Register<LuminaLoadingContainer, object?>("LoadingMessage");

	public static readonly StyledProperty<LuminaLoadingKind> LoadingKindProperty = AvaloniaProperty.Register<LuminaLoadingContainer, LuminaLoadingKind>("LoadingKind", LuminaLoadingKind.Bar);

	public static readonly StyledProperty<IDataTemplate?> LoadingMessageTemplateProperty = AvaloniaProperty.Register<LuminaLoadingContainer, IDataTemplate?>("LoadingMessageTemplate");

	public static readonly StyledProperty<IBrush?> OverlayBackgroundProperty = AvaloniaProperty.Register<LuminaLoadingContainer, IBrush?>("OverlayBackground");

	public static readonly StyledProperty<IBrush?> MessageForegroundProperty = AvaloniaProperty.Register<LuminaLoadingContainer, IBrush?>("MessageForeground");

	public bool IsLoading
	{
		get
		{
			return GetValue(IsLoadingProperty);
		}
		set
		{
			SetValue(IsLoadingProperty, value);
		}
	}

	public object? Indicator
	{
		get
		{
			return GetValue(IndicatorProperty);
		}
		set
		{
			SetValue(IndicatorProperty, value);
		}
	}

	public object? LoadingMessage
	{
		get
		{
			return GetValue(LoadingMessageProperty);
		}
		set
		{
			SetValue(LoadingMessageProperty, value);
		}
	}

	public LuminaLoadingKind LoadingKind
	{
		get
		{
			return GetValue(LoadingKindProperty);
		}
		set
		{
			SetValue(LoadingKindProperty, value);
		}
	}

	public IDataTemplate? LoadingMessageTemplate
	{
		get
		{
			return GetValue(LoadingMessageTemplateProperty);
		}
		set
		{
			SetValue(LoadingMessageTemplateProperty, value);
		}
	}

	public IBrush? OverlayBackground
	{
		get
		{
			return GetValue(OverlayBackgroundProperty);
		}
		set
		{
			SetValue(OverlayBackgroundProperty, value);
		}
	}

	public IBrush? MessageForeground
	{
		get
		{
			return GetValue(MessageForegroundProperty);
		}
		set
		{
			SetValue(MessageForegroundProperty, value);
		}
	}
}
