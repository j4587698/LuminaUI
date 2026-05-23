using Avalonia;
using Avalonia.Controls.Primitives;

namespace LuminaUI.Controls;

public class LuminaListItem : TemplatedControl
{
	private bool _hasIcon;

	private bool _hasDescription;

	private bool _hasValue;

	public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<LuminaListItem, string?>("Header");

	public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaListItem, string?>("Description");

	public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<LuminaListItem, string?>("Value");

	public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<LuminaListItem, string?>("Icon");

	public static readonly DirectProperty<LuminaListItem, bool> HasIconProperty = AvaloniaProperty.RegisterDirect<LuminaListItem, bool>("HasIcon", (LuminaListItem item) => item.HasIcon, null, unsetValue: false);

	public static readonly DirectProperty<LuminaListItem, bool> HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaListItem, bool>("HasDescription", (LuminaListItem item) => item.HasDescription, null, unsetValue: false);

	public static readonly DirectProperty<LuminaListItem, bool> HasValueProperty = AvaloniaProperty.RegisterDirect<LuminaListItem, bool>("HasValue", (LuminaListItem item) => item.HasValue, null, unsetValue: false);

	public string? Header
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

	public string? Value
	{
		get
		{
			return GetValue(ValueProperty);
		}
		set
		{
			SetValue(ValueProperty, value);
		}
	}

	public string? Icon
	{
		get
		{
			return GetValue(IconProperty);
		}
		set
		{
			SetValue(IconProperty, value);
		}
	}

	public bool HasIcon
	{
		get
		{
			return _hasIcon;
		}
		private set
		{
			SetAndRaise(HasIconProperty, ref _hasIcon, value);
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

	public bool HasValue
	{
		get
		{
			return _hasValue;
		}
		private set
		{
			SetAndRaise(HasValueProperty, ref _hasValue, value);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IconProperty)
		{
			HasIcon = !string.IsNullOrWhiteSpace(Icon);
		}
		else if (change.Property == DescriptionProperty)
		{
			HasDescription = !string.IsNullOrWhiteSpace(Description);
		}
		else if (change.Property == ValueProperty)
		{
			HasValue = !string.IsNullOrWhiteSpace(Value);
		}
	}
}
