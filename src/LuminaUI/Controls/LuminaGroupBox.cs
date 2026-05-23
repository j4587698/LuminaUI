using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaGroupBox : ContentControl
{
	public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<LuminaGroupBox, object?>("Header");

	public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaGroupBox, string?>("Description");

	public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<LuminaGroupBox, object?>("Actions");

	public static readonly StyledProperty<IBrush?> HeaderBackgroundProperty = AvaloniaProperty.Register<LuminaGroupBox, IBrush?>("HeaderBackground");

	public static readonly StyledProperty<HorizontalAlignment> HeaderHorizontalAlignmentProperty = AvaloniaProperty.Register<LuminaGroupBox, HorizontalAlignment>("HeaderHorizontalAlignment", HorizontalAlignment.Left);

	public static readonly DirectProperty<LuminaGroupBox, bool> HasHeaderProperty = AvaloniaProperty.RegisterDirect<LuminaGroupBox, bool>("HasHeader", (LuminaGroupBox groupBox) => groupBox.HasHeader, null, unsetValue: false);

	private bool _hasHeader;

	public static readonly DirectProperty<LuminaGroupBox, bool> HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaGroupBox, bool>("HasDescription", (LuminaGroupBox groupBox) => groupBox.HasDescription, null, unsetValue: false);

	private bool _hasDescription;

	public static readonly DirectProperty<LuminaGroupBox, bool> HasActionsProperty = AvaloniaProperty.RegisterDirect<LuminaGroupBox, bool>("HasActions", (LuminaGroupBox groupBox) => groupBox.HasActions, null, unsetValue: false);

	private bool _hasActions;

	public object? Header
	{
		get
		{
			return GetValue(HeaderProperty);
		}
		set
		{
			SetValue(HeaderProperty, value);
		}
	}

	public string? Description
	{
		get
		{
			return GetValue(DescriptionProperty);
		}
		set
		{
			SetValue(DescriptionProperty, value);
		}
	}

	public object? Actions
	{
		get
		{
			return GetValue(ActionsProperty);
		}
		set
		{
			SetValue(ActionsProperty, value);
		}
	}

	public IBrush? HeaderBackground
	{
		get
		{
			return GetValue(HeaderBackgroundProperty);
		}
		set
		{
			SetValue(HeaderBackgroundProperty, value);
		}
	}

	public HorizontalAlignment HeaderHorizontalAlignment
	{
		get
		{
			return GetValue(HeaderHorizontalAlignmentProperty);
		}
		set
		{
			SetValue(HeaderHorizontalAlignmentProperty, value);
		}
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

	public bool HasActions
	{
		get
		{
			return _hasActions;
		}
		private set
		{
			SetAndRaise(HasActionsProperty, ref _hasActions, value);
		}
	}

	public LuminaGroupBox()
	{
		UpdateHeaderState();
		UpdateDescriptionState();
		UpdateActionsState();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == HeaderProperty)
		{
			UpdateHeaderState();
		}
		else if (change.Property == DescriptionProperty)
		{
			UpdateDescriptionState();
		}
		else if (change.Property == ActionsProperty)
		{
			UpdateActionsState();
		}
	}

	private void UpdateHeaderState()
	{
		object? header = Header;
		if (1 == 0)
		{
		}
		bool hasHeader = header != null && (!(header is string text) || !string.IsNullOrWhiteSpace(text));
		if (1 == 0)
		{
		}
		HasHeader = hasHeader;
	}

	private void UpdateDescriptionState()
	{
		HasDescription = !string.IsNullOrWhiteSpace(Description);
	}

	private void UpdateActionsState()
	{
		HasActions = Actions != null;
	}
}
