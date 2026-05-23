using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace LuminaUI.Controls;

[PseudoClasses(new string[] { ":last" })]
public class LuminaBreadcrumbItem : ContentControl
{
	public const string PC_Last = ":last";

	public static readonly StyledProperty<object?> SeparatorProperty = AvaloniaProperty.Register<LuminaBreadcrumbItem, object?>("Separator");

	public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<LuminaBreadcrumbItem, object?>("Icon");

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<LuminaBreadcrumbItem, IDataTemplate?>("IconTemplate");

	public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<LuminaBreadcrumbItem, ICommand?>("Command");

	public static readonly StyledProperty<object?> CommandParameterProperty = AvaloniaProperty.Register<LuminaBreadcrumbItem, object?>("CommandParameter");

	public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<LuminaBreadcrumbItem, bool>("IsReadOnly", defaultValue: false);

	public object? Separator
	{
		get
		{
			return GetValue(SeparatorProperty);
		}
		set
		{
			SetValue(SeparatorProperty, value);
		}
	}

	public object? Icon
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

	public IDataTemplate? IconTemplate
	{
		get
		{
			return GetValue(IconTemplateProperty);
		}
		set
		{
			SetValue(IconTemplateProperty, value);
		}
	}

	public ICommand? Command
	{
		get
		{
			return GetValue(CommandProperty);
		}
		set
		{
			SetValue(CommandProperty, value);
		}
	}

	public object? CommandParameter
	{
		get
		{
			return GetValue(CommandParameterProperty);
		}
		set
		{
			SetValue(CommandParameterProperty, value);
		}
	}

	public bool IsReadOnly
	{
		get
		{
			return GetValue(IsReadOnlyProperty);
		}
		set
		{
			SetValue(IsReadOnlyProperty, value);
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (!IsReadOnly && Command != null && Command.CanExecute(CommandParameter))
		{
			Command.Execute(CommandParameter);
			e.Handled = true;
		}
	}

	internal void SetIsLast(bool isLast)
	{
		base.PseudoClasses.Set(":last", isLast);
	}

	internal void SetIfUnset<T>(AvaloniaProperty<T> property, T value)
	{
		if (!IsSet(property))
		{
			SetCurrentValue(property, value);
		}
	}
}
