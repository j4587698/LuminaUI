using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using LuminaUI.Localization;

namespace LuminaUI.Controls;

public class LuminaEmpty : TemplatedControl
{
	private bool _hasDescription;

	private bool _hasAction;

	private bool _hasCustomContent;

	private string _displayTitle = string.Empty;

	public static readonly StyledProperty<string?> TitleProperty;

	public static readonly StyledProperty<string?> DescriptionProperty;

	public static readonly StyledProperty<object?> IconProperty;

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty;

	public static readonly StyledProperty<object?> ActionProperty;

	public static readonly StyledProperty<IDataTemplate?> ActionTemplateProperty;

	public static readonly StyledProperty<object?> CustomContentProperty;

	public static readonly StyledProperty<IDataTemplate?> CustomContentTemplateProperty;

	public static readonly StyledProperty<bool> ShowIconProperty;

	public static readonly DirectProperty<LuminaEmpty, bool> HasDescriptionProperty;

	public static readonly DirectProperty<LuminaEmpty, bool> HasActionProperty;

	public static readonly DirectProperty<LuminaEmpty, bool> HasCustomContentProperty;

	public static readonly DirectProperty<LuminaEmpty, string> DisplayTitleProperty;

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

	public object? Action
	{
		get
		{
			return GetValue(ActionProperty);
		}
		set
		{
			SetValue(ActionProperty, value);
		}
	}

	public IDataTemplate? ActionTemplate
	{
		get
		{
			return GetValue(ActionTemplateProperty);
		}
		set
		{
			SetValue(ActionTemplateProperty, value);
		}
	}

	public object? CustomContent
	{
		get
		{
			return GetValue(CustomContentProperty);
		}
		set
		{
			SetValue(CustomContentProperty, value);
		}
	}

	public IDataTemplate? CustomContentTemplate
	{
		get
		{
			return GetValue(CustomContentTemplateProperty);
		}
		set
		{
			SetValue(CustomContentTemplateProperty, value);
		}
	}

	public bool ShowIcon
	{
		get
		{
			return GetValue(ShowIconProperty);
		}
		set
		{
			SetValue(ShowIconProperty, value);
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

	public bool HasAction
	{
		get
		{
			return _hasAction;
		}
		private set
		{
			SetAndRaise(HasActionProperty, ref _hasAction, value);
		}
	}

	public bool HasCustomContent
	{
		get
		{
			return _hasCustomContent;
		}
		private set
		{
			SetAndRaise(HasCustomContentProperty, ref _hasCustomContent, value);
		}
	}

	public string DisplayTitle
	{
		get
		{
			return _displayTitle;
		}
		private set
		{
			SetAndRaise(DisplayTitleProperty, ref _displayTitle, value);
		}
	}

	static LuminaEmpty()
	{
		TitleProperty = AvaloniaProperty.Register<LuminaEmpty, string?>("Title");
		DescriptionProperty = AvaloniaProperty.Register<LuminaEmpty, string?>("Description");
		IconProperty = AvaloniaProperty.Register<LuminaEmpty, object?>("Icon");
		IconTemplateProperty = AvaloniaProperty.Register<LuminaEmpty, IDataTemplate?>("IconTemplate");
		ActionProperty = AvaloniaProperty.Register<LuminaEmpty, object?>("Action");
		ActionTemplateProperty = AvaloniaProperty.Register<LuminaEmpty, IDataTemplate?>("ActionTemplate");
		CustomContentProperty = AvaloniaProperty.Register<LuminaEmpty, object?>("CustomContent");
		CustomContentTemplateProperty = AvaloniaProperty.Register<LuminaEmpty, IDataTemplate?>("CustomContentTemplate");
		ShowIconProperty = AvaloniaProperty.Register<LuminaEmpty, bool>("ShowIcon", defaultValue: true);
		HasDescriptionProperty = AvaloniaProperty.RegisterDirect<LuminaEmpty, bool>("HasDescription", (LuminaEmpty e) => e.HasDescription, null, unsetValue: false);
		HasActionProperty = AvaloniaProperty.RegisterDirect<LuminaEmpty, bool>("HasAction", (LuminaEmpty e) => e.HasAction, null, unsetValue: false);
		HasCustomContentProperty = AvaloniaProperty.RegisterDirect<LuminaEmpty, bool>("HasCustomContent", (LuminaEmpty e) => e.HasCustomContent, null, unsetValue: false);
		DisplayTitleProperty = AvaloniaProperty.RegisterDirect<LuminaEmpty, string>("DisplayTitle", (LuminaEmpty e) => e.DisplayTitle);
		TitleProperty.Changed.AddClassHandler(delegate(LuminaEmpty empty, AvaloniaPropertyChangedEventArgs _)
		{
			empty.UpdateState();
		});
		DescriptionProperty.Changed.AddClassHandler(delegate(LuminaEmpty empty, AvaloniaPropertyChangedEventArgs _)
		{
			empty.UpdateState();
		});
		ActionProperty.Changed.AddClassHandler(delegate(LuminaEmpty empty, AvaloniaPropertyChangedEventArgs _)
		{
			empty.UpdateState();
		});
		CustomContentProperty.Changed.AddClassHandler(delegate(LuminaEmpty empty, AvaloniaPropertyChangedEventArgs _)
		{
			empty.UpdateState();
		});
	}

	public LuminaEmpty()
	{
		UpdateState();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		UpdateState();
	}

	private void UpdateState()
	{
		HasDescription = !string.IsNullOrWhiteSpace(Description);
		HasAction = Action != null;
		HasCustomContent = CustomContent != null;
		DisplayTitle = ((!string.IsNullOrWhiteSpace(Title)) ? Title : LuminaLocalization.Get("Lumina.Page.Empty"));
	}
}
