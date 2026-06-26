using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaInputGroup : Grid
{
    public static readonly StyledProperty<int> FillIndexProperty;

    public int FillIndex
    {
        get => GetValue(FillIndexProperty);
        set => SetValue(FillIndexProperty, value);
    }

    static LuminaInputGroup()
    {
        FillIndexProperty = AvaloniaProperty.Register<LuminaInputGroup, int>(nameof(FillIndex), -1);
        FillIndexProperty.Changed.AddClassHandler((LuminaInputGroup group, AvaloniaPropertyChangedEventArgs _) =>
        {
            group.UpdateChildClasses();
        });
    }

    public LuminaInputGroup()
    {
        Children.CollectionChanged += OnChildrenChanged;
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
        Control[] controls = Children.OfType<Control>().ToArray();
        int fillIndex = ResolveFillIndex(controls);
        ColumnDefinitions.Clear();
        for (int index = 0; index < controls.Length; index++)
        {
            Control control = controls[index];
            RemoveGroupClasses(control);
            ColumnDefinitions.Add(new ColumnDefinition((index == fillIndex) ? GridLength.Star : GridLength.Auto));
            Grid.SetColumn(control, index);
            control.Classes.Add("LuminaInputGroupItem");
            string positionClass = controls.Length switch
            {
                1 => "LuminaInputGroupSingle",
                _ when index == 0 => "LuminaInputGroupFirst",
                _ when index == controls.Length - 1 => "LuminaInputGroupLast",
                _ => "LuminaInputGroupMiddle"
            };
            control.Classes.Add(positionClass);
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
        foreach (Control control in Children.OfType<Control>())
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
