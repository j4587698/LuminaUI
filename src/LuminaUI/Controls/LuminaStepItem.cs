using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace LuminaUI.Controls;

[PseudoClasses(new string[] { ":first", ":last", ":current", ":finished", ":wait", ":error", ":vertical" })]
public class LuminaStepItem : HeaderedContentControl
{
	public const string PC_First = ":first";

	public const string PC_Last = ":last";

	public const string PC_Current = ":current";

	public const string PC_Finished = ":finished";

	public const string PC_Wait = ":wait";

	public const string PC_Error = ":error";

	public const string PC_Vertical = ":vertical";

	public static readonly StyledProperty<LuminaStepStatus> StatusProperty;

	public static readonly StyledProperty<object?> IconProperty;

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

	public static readonly StyledProperty<int> StepNumberProperty;

	public static readonly DirectProperty<LuminaStepItem, bool> IsCurrentProperty;

	private bool _isCurrent;

	public LuminaStepStatus Status
	{
		get
		{
			return GetValue(StatusProperty);
		}
		set
		{
			SetValue(StatusProperty, value);
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

	public int StepNumber
	{
		get
		{
			return GetValue(StepNumberProperty);
		}
		set
		{
			SetValue(StepNumberProperty, value);
		}
	}

	public bool IsCurrent
	{
		get
		{
			return _isCurrent;
		}
		private set
		{
			SetAndRaise(IsCurrentProperty, ref _isCurrent, value);
		}
	}

	static LuminaStepItem()
	{
		StatusProperty = AvaloniaProperty.Register<LuminaStepItem, LuminaStepStatus>("Status", LuminaStepStatus.Wait);
		IconProperty = AvaloniaProperty.Register<LuminaStepItem, object?>("Icon");
		IconTemplateProperty = AvaloniaProperty.Register<LuminaStepItem, IDataTemplate?>("IconTemplate");
		StepNumberProperty = AvaloniaProperty.Register<LuminaStepItem, int>("StepNumber", 0);
		IsCurrentProperty = AvaloniaProperty.RegisterDirect<LuminaStepItem, bool>("IsCurrent", (LuminaStepItem item) => item.IsCurrent, null, unsetValue: false);
		StatusProperty.Changed.AddClassHandler(delegate(LuminaStepItem item, AvaloniaPropertyChangedEventArgs<LuminaStepStatus> _)
		{
			item.UpdateStatusPseudoClass();
		});
		IconProperty.Changed.AddClassHandler(delegate(LuminaStepItem item, AvaloniaPropertyChangedEventArgs<object?> _)
		{
			item.UpdateIconState();
		});
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		UpdateStatusPseudoClass();
		UpdateIconState();
	}

	internal void SetEdgeState(bool isFirst, bool isLast)
	{
		base.PseudoClasses.Set(":first", isFirst);
		base.PseudoClasses.Set(":last", isLast);
	}

	internal void SetCurrentState(bool isCurrent)
	{
		IsCurrent = isCurrent;
		base.PseudoClasses.Set(":current", isCurrent);
	}

	internal void SetDirectionState(LuminaStepsDirection direction)
	{
		base.PseudoClasses.Set(":vertical", direction == LuminaStepsDirection.Vertical);
	}

	internal void SetIfUnset<T>(AvaloniaProperty<T> property, T value)
	{
		if (!IsSet(property))
		{
			SetCurrentValue(property, value);
		}
	}

	private void UpdateStatusPseudoClass()
	{
		base.PseudoClasses.Set(":finished", Status == LuminaStepStatus.Finish);
		base.PseudoClasses.Set(":wait", Status == LuminaStepStatus.Wait);
		base.PseudoClasses.Set(":error", Status == LuminaStepStatus.Error);
	}

	private void UpdateIconState()
	{
	}
}
