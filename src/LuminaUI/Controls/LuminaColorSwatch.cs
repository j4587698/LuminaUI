using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaColorSwatch : TemplatedControl
{
	private bool _hasDescription;

	public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<LuminaColorSwatch, string?>("Title");

	public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<LuminaColorSwatch, string?>("Description");

	public static readonly StyledProperty<IBrush?> SwatchBrushProperty = AvaloniaProperty.Register<LuminaColorSwatch, IBrush?>("SwatchBrush");

	public static readonly DirectProperty<LuminaColorSwatch, bool> HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaColorSwatch, bool>("HasDescription", (LuminaColorSwatch swatch) => swatch.HasDescription, null, unsetValue: false);

	public string? Title
	{
		get
		{
			return GetValue(TitleProperty);
		}
		set
		{
			SetValue(TitleProperty, value);
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

	public IBrush? SwatchBrush
	{
		get
		{
			return GetValue(SwatchBrushProperty);
		}
		set
		{
			SetValue(SwatchBrushProperty, value);
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

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == DescriptionProperty)
		{
			HasDescription = !string.IsNullOrWhiteSpace(Description);
		}
	}
}
