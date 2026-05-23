using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;

namespace LuminaUI.Controls;

public class LuminaButtonGroup : StackPanel
{
	public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<LuminaButtonGroup, int>("SelectedIndex", 0, inherits: false, BindingMode.TwoWay);

	public static readonly StyledProperty<bool> IsSelectionRequiredProperty = AvaloniaProperty.Register<LuminaButtonGroup, bool>("IsSelectionRequired", defaultValue: true);

	public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty = AvaloniaProperty.Register<LuminaButtonGroup, ICommand?>("SelectionChangedCommand");

	public int SelectedIndex
	{
		get
		{
			return GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
		}
	}

	public bool IsSelectionRequired
	{
		get
		{
			return GetValue(IsSelectionRequiredProperty);
		}
		set
		{
			SetValue(IsSelectionRequiredProperty, value);
		}
	}

	public ICommand? SelectionChangedCommand
	{
		get
		{
			return GetValue(SelectionChangedCommandProperty);
		}
		set
		{
			SetValue(SelectionChangedCommandProperty, value);
		}
	}

	public LuminaButtonGroup()
	{
		base.Orientation = Orientation.Horizontal;
		base.Spacing = 0.0;
		base.Children.CollectionChanged += OnChildrenChanged;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		AttachChildren();
		SyncSelection();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		DetachChildren();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == SelectedIndexProperty)
		{
			SyncSelection();
			ExecuteSelectionCommand();
		}
		else if (change.Property == IsSelectionRequiredProperty)
		{
			SyncSelection();
		}
	}

	private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems != null)
		{
			foreach (Button item in e.OldItems.OfType<Button>())
			{
				item.Click -= OnChildClick;
			}
		}
		if (e.NewItems != null)
		{
			foreach (Button item2 in e.NewItems.OfType<Button>())
			{
				PrepareChild(item2);
			}
		}
		SyncSelection();
	}

	private void AttachChildren()
	{
		foreach (Button button in base.Children.OfType<Button>())
		{
			PrepareChild(button);
		}
	}

	private void DetachChildren()
	{
		foreach (Button button in base.Children.OfType<Button>())
		{
			button.Click -= OnChildClick;
		}
	}

	private void PrepareChild(Button button)
	{
		button.Classes.Add("ButtonGroupItem");
		button.Theme = ResolveButtonTheme((button is ToggleButton) ? "LuminaButtonGroupToggleButtonTheme" : "LuminaButtonGroupButtonTheme");
		button.Click -= OnChildClick;
		button.Click += OnChildClick;
	}

	private void OnChildClick(object? sender, RoutedEventArgs e)
	{
		if (!(sender is Button button))
		{
			return;
		}
		int index = base.Children.IndexOf(button);
		if (index >= 0)
		{
			if (!IsSelectionRequired && SelectedIndex == index)
			{
				SelectedIndex = -1;
			}
			else
			{
				SelectedIndex = index;
			}
		}
	}

	private void SyncSelection()
	{
		for (int index = 0; index < base.Children.Count; index++)
		{
			if (base.Children[index] is Button button)
			{
				SetClass(button, "First", index == 0);
				SetClass(button, "Last", index == base.Children.Count - 1);
				SetClass(button, "Selected", index == SelectedIndex);
				if (button is ToggleButton toggleButton)
				{
					toggleButton.IsChecked = index == SelectedIndex;
				}
			}
		}
	}

	private void ExecuteSelectionCommand()
	{
		ICommand? selectionChangedCommand = SelectionChangedCommand;
		if (selectionChangedCommand != null && selectionChangedCommand.CanExecute(SelectedIndex))
		{
			selectionChangedCommand.Execute(SelectedIndex);
		}
	}

	private static void SetClass(Control control, string className, bool isEnabled)
	{
		if (isEnabled)
		{
			if (!control.Classes.Contains(className))
			{
				control.Classes.Add(className);
			}
		}
		else
		{
			control.Classes.Remove(className);
		}
	}

	private static ControlTheme? ResolveButtonTheme(string key)
	{
		Application? current = Application.Current;
		object? resource;
		return (current != null && current.TryFindResource(key, out resource)) ? (resource as ControlTheme) : null;
	}
}
