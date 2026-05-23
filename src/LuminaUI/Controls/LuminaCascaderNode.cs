using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;

namespace LuminaUI.Controls;

public class LuminaCascaderNode : AvaloniaObject
{
	public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<LuminaCascaderNode, bool>("IsActive", defaultValue: false);

	public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<LuminaCascaderNode, bool>("IsLoading", defaultValue: false);

	public string Label { get; set; } = string.Empty;

	public object? Value { get; set; }

	public AvaloniaList<LuminaCascaderNode> Children { get; set; } = new AvaloniaList<LuminaCascaderNode>();

	public bool IsSelectable { get; set; } = true;

	public bool HasUnloadedChildren { get; set; }

	public bool IsLoaded { get; set; }

	public Func<LuminaCascaderNode, Task<IEnumerable<LuminaCascaderNode>>>? LoadChildrenAsync { get; set; }

	public bool IsLeaf => Children.Count == 0 && !HasUnloadedChildren && LoadChildrenAsync == null;

	public bool IsActive
	{
		get
		{
			return GetValue(IsActiveProperty);
		}
		set
		{
			SetValue(IsActiveProperty, value);
		}
	}

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
}
