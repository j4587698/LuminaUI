using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaInputGroup : Grid
{
	private const string ItemClass = "LuminaInputGroupItem";

	private const string FirstClass = "LuminaInputGroupFirst";

	private const string MiddleClass = "LuminaInputGroupMiddle";

	private const string LastClass = "LuminaInputGroupLast";

	private const string SingleClass = "LuminaInputGroupSingle";

	public static readonly StyledProperty<int> FillIndexProperty;

	public int FillIndex
	{
		get
		{
			return GetValue(FillIndexProperty);
		}
		set
		{
			SetValue(FillIndexProperty, value);
		}
	}

	static LuminaInputGroup()
	{
		FillIndexProperty = AvaloniaProperty.Register<LuminaInputGroup, int>("FillIndex", -1);
		FillIndexProperty.Changed.AddClassHandler(delegate(LuminaInputGroup group, AvaloniaPropertyChangedEventArgs _)
		{
			group.UpdateChildClasses();
		});
	}

	public LuminaInputGroup()
	{
		base.Children.CollectionChanged += OnChildrenChanged;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		UpdateChildClasses();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		ClearChildClasses();
		base.OnDetachedFromVisualTree(e);
	}

	private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems != null)
		{
			foreach (Control control in e.OldItems.OfType<Control>())
			{
				RemoveGroupClasses(control);
			}
		}
		UpdateChildClasses();
	}

	private void UpdateChildClasses()
	{
		Control[] controls = base.Children.OfType<Control>().ToArray();
		int fillIndex = ResolveFillIndex(controls);
		base.ColumnDefinitions.Clear();
		for (int index = 0; index < controls.Length; index++)
		{
			Control control = controls[index];
			RemoveGroupClasses(control);
			base.ColumnDefinitions.Add(new ColumnDefinition((index == fillIndex) ? GridLength.Star : GridLength.Auto));
			Grid.SetColumn(control, index);
			control.Classes.Add("LuminaInputGroupItem");
			control.Classes.Add((controls.Length == 1) ? "LuminaInputGroupSingle" : ((index == 0) ? "LuminaInputGroupFirst" : ((index == controls.Length - 1) ? "LuminaInputGroupLast" : "LuminaInputGroupMiddle")));
		}
	}

	private int ResolveFillIndex(IReadOnlyList<Control> controls)
	{
		if (controls.Count == 0)
		{
			return -1;
		}
		if (FillIndex >= 0 && FillIndex < controls.Count)
		{
			return FillIndex;
		}
		for (int index = 0; index < controls.Count; index++)
		{
			if (controls[index] is TextBox && HasAutoWidth(controls[index]))
			{
				return index;
			}
		}
		for (int i = 0; i < controls.Count; i++)
		{
			if (IsInputControl(controls[i]) && HasAutoWidth(controls[i]))
			{
				return i;
			}
		}
		for (int j = 0; j < controls.Count; j++)
		{
			if (IsInputControl(controls[j]))
			{
				return j;
			}
		}
		return controls.Count - 1;
	}

	private static bool IsInputControl(Control control)
	{
		if (control is TextBox || control is ComboBox || control is NumericUpDown || control is DatePicker || control is TimePicker || control is CalendarDatePicker)
		{
			return true;
		}
		return false;
	}

	private static bool HasAutoWidth(Control control)
	{
		return double.IsNaN(control.Width);
	}

	private void ClearChildClasses()
	{
		foreach (Control control in base.Children.OfType<Control>())
		{
			RemoveGroupClasses(control);
		}
	}

	private static void RemoveGroupClasses(Control control)
	{
		control.Classes.Remove("LuminaInputGroupItem");
		control.Classes.Remove("LuminaInputGroupFirst");
		control.Classes.Remove("LuminaInputGroupMiddle");
		control.Classes.Remove("LuminaInputGroupLast");
		control.Classes.Remove("LuminaInputGroupSingle");
	}
}
