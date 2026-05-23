using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace LuminaUI.Controls;

public class LuminaRating : TemplatedControl
{
	public const string PART_ItemsHost = "PART_ItemsHost";

	private StackPanel? _itemsHost;

	public static readonly StyledProperty<double> ValueProperty;

	public static readonly StyledProperty<int> CountProperty;

	public static readonly StyledProperty<bool> AllowClearProperty;

	public static readonly StyledProperty<bool> AllowHalfProperty;

	public static readonly StyledProperty<bool> IsReadOnlyProperty;

	public static readonly StyledProperty<object?> CharacterProperty;

	public static readonly StyledProperty<double> SizeProperty;

	public static readonly DirectProperty<LuminaRating, AvaloniaList<LuminaRatingItem>> ItemsProperty;

	private AvaloniaList<LuminaRatingItem> _items = new AvaloniaList<LuminaRatingItem>();

	public double Value
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

	public int Count
	{
		get
		{
			return GetValue(CountProperty);
		}
		set
		{
			SetValue(CountProperty, value);
		}
	}

	public bool AllowClear
	{
		get
		{
			return GetValue(AllowClearProperty);
		}
		set
		{
			SetValue(AllowClearProperty, value);
		}
	}

	public bool AllowHalf
	{
		get
		{
			return GetValue(AllowHalfProperty);
		}
		set
		{
			SetValue(AllowHalfProperty, value);
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

	public object? Character
	{
		get
		{
			return GetValue(CharacterProperty);
		}
		set
		{
			SetValue(CharacterProperty, value);
		}
	}

	public double Size
	{
		get
		{
			return GetValue(SizeProperty);
		}
		set
		{
			SetValue(SizeProperty, value);
		}
	}

	public AvaloniaList<LuminaRatingItem> Items
	{
		get
		{
			return _items;
		}
		private set
		{
			SetAndRaise(ItemsProperty, ref _items, value);
		}
	}

	static LuminaRating()
	{
		ValueProperty = AvaloniaProperty.Register<LuminaRating, double>("Value", 0.0, inherits: false, BindingMode.TwoWay);
		CountProperty = AvaloniaProperty.Register<LuminaRating, int>("Count", 5);
		AllowClearProperty = AvaloniaProperty.Register<LuminaRating, bool>("AllowClear", defaultValue: true);
		AllowHalfProperty = AvaloniaProperty.Register<LuminaRating, bool>("AllowHalf", defaultValue: false);
		IsReadOnlyProperty = AvaloniaProperty.Register<LuminaRating, bool>("IsReadOnly", defaultValue: false);
		CharacterProperty = AvaloniaProperty.Register<LuminaRating, object?>("Character", "★");
		SizeProperty = AvaloniaProperty.Register<LuminaRating, double>("Size", 24.0);
		ItemsProperty = AvaloniaProperty.RegisterDirect<LuminaRating, AvaloniaList<LuminaRatingItem>>("Items", (LuminaRating rating) => rating.Items);
		ValueProperty.Changed.AddClassHandler(delegate(LuminaRating rating, AvaloniaPropertyChangedEventArgs<double> _)
		{
			rating.ApplyValue();
		});
		CountProperty.Changed.AddClassHandler(delegate(LuminaRating rating, AvaloniaPropertyChangedEventArgs<int> _)
		{
			rating.RebuildItems();
		});
		AllowHalfProperty.Changed.AddClassHandler(delegate(LuminaRating rating, AvaloniaPropertyChangedEventArgs<bool> _)
		{
			rating.RebuildItems();
		});
		CharacterProperty.Changed.AddClassHandler(delegate(LuminaRating rating, AvaloniaPropertyChangedEventArgs<object?> _)
		{
			rating.RebuildItems();
		});
		SizeProperty.Changed.AddClassHandler(delegate(LuminaRating rating, AvaloniaPropertyChangedEventArgs<double> _)
		{
			rating.RebuildItems();
		});
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_itemsHost = e.NameScope.Find<StackPanel>("PART_ItemsHost");
		RebuildItems();
	}

	internal void PreviewItem(LuminaRatingItem item, bool half)
	{
		if (!IsReadOnly)
		{
			int index = Items.IndexOf(item);
			if (index >= 0)
			{
				double preview = (double)index + ((AllowHalf && half) ? 0.5 : 1.0);
				ApplyValue(preview);
			}
		}
	}

	internal void CommitItem(LuminaRatingItem item, bool half)
	{
		if (!IsReadOnly)
		{
			int index = Items.IndexOf(item);
			if (index >= 0)
			{
				double nextValue = (double)index + ((AllowHalf && half) ? 0.5 : 1.0);
				SetCurrentValue(ValueProperty, (AllowClear && Math.Abs(Value - nextValue) < double.Epsilon) ? 0.0 : nextValue);
			}
		}
	}

	internal void RestoreValue()
	{
		ApplyValue();
	}

	private void RebuildItems()
	{
		Items.Clear();
		_itemsHost?.Children.Clear();
		int count = Math.Clamp(Count, 1, 20);
		for (int i = 0; i < count; i++)
		{
			LuminaRatingItem item = new LuminaRatingItem
			{
				Owner = this,
				Character = Character,
				AllowHalf = AllowHalf,
				Size = Size
			};
			Items.Add(item);
			_itemsHost?.Children.Add(item);
		}
		ApplyValue();
	}

	private void ApplyValue()
	{
		ApplyValue(Value);
	}

	private void ApplyValue(double value)
	{
		double normalized = Math.Clamp(value, 0.0, Items.Count);
		for (int i = 0; i < Items.Count; i++)
		{
			double ratio = Math.Clamp(normalized - (double)i, 0.0, 1.0);
			Items[i].SetSelectedRatio(ratio);
		}
	}
}
