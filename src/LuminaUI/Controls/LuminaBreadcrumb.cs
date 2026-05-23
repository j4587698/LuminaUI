using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace LuminaUI.Controls;

public class LuminaBreadcrumb : ItemsControl
{
	private static readonly FuncTemplate<Panel?> DefaultPanel;

	public static readonly StyledProperty<BindingBase?> IconBindingProperty;

	public static readonly StyledProperty<BindingBase?> CommandBindingProperty;

	public static readonly StyledProperty<BindingBase?> CommandParameterBindingProperty;

	public static readonly StyledProperty<object?> SeparatorProperty;

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? IconBinding
	{
		get
		{
			return GetValue(IconBindingProperty);
		}
		set
		{
			SetValue(IconBindingProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? CommandBinding
	{
		get
		{
			return GetValue(CommandBindingProperty);
		}
		set
		{
			SetValue(CommandBindingProperty, value);
		}
	}

	[AssignBinding]
	[InheritDataTypeFromItems("ItemsSource")]
	public BindingBase? CommandParameterBinding
	{
		get
		{
			return GetValue(CommandParameterBindingProperty);
		}
		set
		{
			SetValue(CommandParameterBindingProperty, value);
		}
	}

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

	[InheritDataTypeFromItems("ItemsSource")]
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

	static LuminaBreadcrumb()
	{
		DefaultPanel = new FuncTemplate<Panel?>(() => new StackPanel
		{
			Orientation = Orientation.Horizontal
		});
		IconBindingProperty = AvaloniaProperty.Register<LuminaBreadcrumb, BindingBase?>("IconBinding");
		CommandBindingProperty = AvaloniaProperty.Register<LuminaBreadcrumb, BindingBase?>("CommandBinding");
		CommandParameterBindingProperty = AvaloniaProperty.Register<LuminaBreadcrumb, BindingBase?>("CommandParameterBinding");
		SeparatorProperty = AvaloniaProperty.Register<LuminaBreadcrumb, object?>("Separator", "/");
		IconTemplateProperty = AvaloniaProperty.Register<LuminaBreadcrumb, IDataTemplate?>("IconTemplate");
		ItemsControl.ItemsPanelProperty.OverrideDefaultValue<LuminaBreadcrumb>(DefaultPanel);
		SeparatorProperty.Changed.AddClassHandler(delegate(LuminaBreadcrumb breadcrumb, AvaloniaPropertyChangedEventArgs<object?> _)
		{
			breadcrumb.RefreshBreadcrumbContainers();
		});
		IconTemplateProperty.Changed.AddClassHandler(delegate(LuminaBreadcrumb breadcrumb, AvaloniaPropertyChangedEventArgs<IDataTemplate?> _)
		{
			breadcrumb.RefreshBreadcrumbContainers();
		});
	}

	protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
	{
		recycleKey = null;
		return !(item is LuminaBreadcrumbItem);
	}

	protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
	{
		return (item as LuminaBreadcrumbItem) ?? new LuminaBreadcrumbItem();
	}

	protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
	{
		base.PrepareContainerForItemOverride(container, item, index);
		if (!(container is LuminaBreadcrumbItem breadcrumbItem))
		{
			return;
		}
		breadcrumbItem.SetIfUnset(LuminaBreadcrumbItem.SeparatorProperty, Separator);
		breadcrumbItem.SetIfUnset(LuminaBreadcrumbItem.IconTemplateProperty, IconTemplate);
		if (!(item is LuminaBreadcrumbItem))
		{
			if (!breadcrumbItem.IsSet(ContentControl.ContentTemplateProperty) && base.ItemTemplate != null)
			{
				breadcrumbItem.SetCurrentValue(ContentControl.ContentTemplateProperty, base.ItemTemplate);
			}
			if (base.DisplayMemberBinding != null && !breadcrumbItem.IsSet(ContentControl.ContentProperty))
			{
				breadcrumbItem.Bind(ContentControl.ContentProperty, base.DisplayMemberBinding);
			}
			else if (!breadcrumbItem.IsSet(ContentControl.ContentProperty))
			{
				breadcrumbItem.SetCurrentValue(ContentControl.ContentProperty, item);
			}
		}
		if (IconBinding != null && !breadcrumbItem.IsSet(LuminaBreadcrumbItem.IconProperty))
		{
			breadcrumbItem.Bind(LuminaBreadcrumbItem.IconProperty, IconBinding);
		}
		if (CommandBinding != null && !breadcrumbItem.IsSet(LuminaBreadcrumbItem.CommandProperty))
		{
			breadcrumbItem.Bind(LuminaBreadcrumbItem.CommandProperty, CommandBinding);
		}
		if (CommandParameterBinding != null && !breadcrumbItem.IsSet(LuminaBreadcrumbItem.CommandParameterProperty))
		{
			breadcrumbItem.Bind(LuminaBreadcrumbItem.CommandParameterProperty, CommandParameterBinding);
		}
		RefreshContainerStates();
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		RefreshContainerStates();
		return base.ArrangeOverride(finalSize);
	}

	private void RefreshBreadcrumbContainers()
	{
		if (base.ItemsPanelRoot == null)
		{
			return;
		}
		foreach (LuminaBreadcrumbItem item in base.ItemsPanelRoot.Children.OfType<LuminaBreadcrumbItem>())
		{
			if (!item.IsSet(LuminaBreadcrumbItem.SeparatorProperty))
			{
				item.SetCurrentValue(LuminaBreadcrumbItem.SeparatorProperty, Separator);
			}
			if (!item.IsSet(LuminaBreadcrumbItem.IconTemplateProperty))
			{
				item.SetCurrentValue(LuminaBreadcrumbItem.IconTemplateProperty, IconTemplate);
			}
		}
		RefreshContainerStates();
	}

	private void RefreshContainerStates()
	{
		if (base.ItemsPanelRoot != null)
		{
			LuminaBreadcrumbItem[] items = base.ItemsPanelRoot.Children.OfType<LuminaBreadcrumbItem>().ToArray();
			for (int i = 0; i < items.Length; i++)
			{
				items[i].SetIsLast(i == items.Length - 1);
			}
		}
	}
}
