using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaBanner : HeaderedContentControl
{
	private Button? _closeButton;

	public static readonly StyledProperty<LuminaBannerType> TypeProperty;

	public static readonly StyledProperty<bool> CanCloseProperty;

	public static readonly StyledProperty<bool> ShowIconProperty;

	public static readonly StyledProperty<object?> IconProperty;

	public static readonly StyledProperty<ICommand?> CloseCommandProperty;

	public static readonly DirectProperty<LuminaBanner, bool> HasHeaderProperty;

	private bool _hasHeader;

	public static readonly DirectProperty<LuminaBanner, object?> EffectiveIconProperty;

	private object? _effectiveIcon;

	public LuminaBannerType Type
	{
		get
		{
			return GetValue(TypeProperty);
		}
		set
		{
			SetValue(TypeProperty, value);
		}
	}

	public bool CanClose
	{
		get
		{
			return GetValue(CanCloseProperty);
		}
		set
		{
			SetValue(CanCloseProperty, value);
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

	public ICommand? CloseCommand
	{
		get
		{
			return GetValue(CloseCommandProperty);
		}
		set
		{
			SetValue(CloseCommandProperty, value);
		}
	}

	public bool HasHeader
	{
		get
		{
			return _hasHeader;
		}
		private set
		{
			SetAndRaise(HasHeaderProperty, ref _hasHeader, value);
		}
	}

	public object? EffectiveIcon
	{
		get
		{
			return _effectiveIcon;
		}
		private set
		{
			SetAndRaise(EffectiveIconProperty, ref _effectiveIcon, value);
		}
	}

	static LuminaBanner()
	{
		TypeProperty = AvaloniaProperty.Register<LuminaBanner, LuminaBannerType>("Type", LuminaBannerType.Info);
		CanCloseProperty = AvaloniaProperty.Register<LuminaBanner, bool>("CanClose", defaultValue: false);
		ShowIconProperty = AvaloniaProperty.Register<LuminaBanner, bool>("ShowIcon", defaultValue: true);
		IconProperty = AvaloniaProperty.Register<LuminaBanner, object?>("Icon");
		CloseCommandProperty = AvaloniaProperty.Register<LuminaBanner, ICommand?>("CloseCommand");
		HasHeaderProperty = AvaloniaProperty.RegisterDirect<LuminaBanner, bool>("HasHeader", (LuminaBanner banner) => banner.HasHeader, null, unsetValue: false);
		EffectiveIconProperty = AvaloniaProperty.RegisterDirect("EffectiveIcon", (LuminaBanner banner) => banner.EffectiveIcon);
		HeaderedContentControl.HeaderProperty.Changed.AddClassHandler(delegate(LuminaBanner banner, AvaloniaPropertyChangedEventArgs _)
		{
			banner.UpdateState();
		});
		IconProperty.Changed.AddClassHandler(delegate(LuminaBanner banner, AvaloniaPropertyChangedEventArgs _)
		{
			banner.UpdateState();
		});
		TypeProperty.Changed.AddClassHandler(delegate(LuminaBanner banner, AvaloniaPropertyChangedEventArgs _)
		{
			banner.UpdateState();
		});
	}

	public LuminaBanner()
	{
		UpdateState();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		if (_closeButton != null)
		{
			_closeButton.Click -= OnCloseButtonClick;
		}
		_closeButton = e.NameScope.FindRequired<Button>("PART_CloseButton");
		if (_closeButton != null)
		{
			_closeButton.Click += OnCloseButtonClick;
		}
	}

	private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
	{
		base.IsVisible = false;
		ICommand? command = CloseCommand;
		if (command != null && command.CanExecute(null))
		{
			command.Execute(null);
		}
	}

	private void UpdateState()
	{
		HasHeader = base.Header != null;
		object? icon = Icon;
		object? obj = icon;
		if (obj == null)
		{
			LuminaBannerType type = Type;
			if (1 == 0)
			{
			}
			string text = type switch
			{
				LuminaBannerType.Success => "OK", 
				LuminaBannerType.Warning => "!", 
				LuminaBannerType.Danger => "!", 
				_ => "i", 
			};
			if (1 == 0)
			{
			}
			obj = text;
		}
		EffectiveIcon = obj;
	}
}
