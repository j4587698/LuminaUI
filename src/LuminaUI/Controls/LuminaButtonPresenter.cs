using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaButtonPresenter : ContentControl
{
	private Grid? _layoutRoot;

	private Control? _iconPresenter;

	private Control? _loadingPresenter;

	private Control? _contentPresenter;

	private bool _hasIcon;

	private bool _hasContent;

	private bool _showsIcon;

	private bool _showsLoading;

	private IBrush? _effectiveIconForeground;

	public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<LuminaButtonPresenter, object?>("Icon");

	public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<LuminaButtonPresenter, IDataTemplate?>("IconTemplate");

	public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<LuminaButtonPresenter, IBrush?>("IconForeground");

	public static readonly StyledProperty<bool> SyncIconForegroundProperty = AvaloniaProperty.Register<LuminaButtonPresenter, bool>("SyncIconForeground", defaultValue: true);

	public static readonly StyledProperty<LuminaIconPlacement> IconPlacementProperty = AvaloniaProperty.Register<LuminaButtonPresenter, LuminaIconPlacement>("IconPlacement", LuminaIconPlacement.Left);

	public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<LuminaButtonPresenter, double>("IconSize", 16.0);

	public static readonly StyledProperty<double> IconSpacingProperty = AvaloniaProperty.Register<LuminaButtonPresenter, double>("IconSpacing", 8.0);

	public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<LuminaButtonPresenter, bool>("IsLoading", defaultValue: false);

	public static readonly StyledProperty<LuminaLoadingKind> LoadingKindProperty = AvaloniaProperty.Register<LuminaButtonPresenter, LuminaLoadingKind>("LoadingKind", LuminaLoadingKind.Ring);

	public static readonly DirectProperty<LuminaButtonPresenter, bool> HasIconProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>("HasIcon", (LuminaButtonPresenter presenter) => presenter.HasIcon, null, unsetValue: false);

	public static readonly DirectProperty<LuminaButtonPresenter, bool> HasContentProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>("HasContent", (LuminaButtonPresenter presenter) => presenter.HasContent, null, unsetValue: false);

	public static readonly DirectProperty<LuminaButtonPresenter, bool> ShowsIconProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>("ShowsIcon", (LuminaButtonPresenter presenter) => presenter.ShowsIcon, null, unsetValue: false);

	public static readonly DirectProperty<LuminaButtonPresenter, bool> ShowsLoadingProperty = AvaloniaProperty.RegisterDirect<LuminaButtonPresenter, bool>("ShowsLoading", (LuminaButtonPresenter presenter) => presenter.ShowsLoading, null, unsetValue: false);

	public static readonly DirectProperty<LuminaButtonPresenter, IBrush?> EffectiveIconForegroundProperty = AvaloniaProperty.RegisterDirect("EffectiveIconForeground", (LuminaButtonPresenter presenter) => presenter.EffectiveIconForeground);

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

	public IBrush? IconForeground
	{
		get
		{
			return GetValue(IconForegroundProperty);
		}
		set
		{
			SetValue(IconForegroundProperty, value);
		}
	}

	public bool SyncIconForeground
	{
		get
		{
			return GetValue(SyncIconForegroundProperty);
		}
		set
		{
			SetValue(SyncIconForegroundProperty, value);
		}
	}

	public LuminaIconPlacement IconPlacement
	{
		get
		{
			return GetValue(IconPlacementProperty);
		}
		set
		{
			SetValue(IconPlacementProperty, value);
		}
	}

	public double IconSize
	{
		get
		{
			return GetValue(IconSizeProperty);
		}
		set
		{
			SetValue(IconSizeProperty, value);
		}
	}

	public double IconSpacing
	{
		get
		{
			return GetValue(IconSpacingProperty);
		}
		set
		{
			SetValue(IconSpacingProperty, value);
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

	public LuminaLoadingKind LoadingKind
	{
		get
		{
			return GetValue(LoadingKindProperty);
		}
		set
		{
			SetValue(LoadingKindProperty, value);
		}
	}

	public bool HasIcon
	{
		get
		{
			return _hasIcon;
		}
		private set
		{
			SetAndRaise(HasIconProperty, ref _hasIcon, value);
		}
	}

	public bool HasContent
	{
		get
		{
			return _hasContent;
		}
		private set
		{
			SetAndRaise(HasContentProperty, ref _hasContent, value);
		}
	}

	public bool ShowsIcon
	{
		get
		{
			return _showsIcon;
		}
		private set
		{
			SetAndRaise(ShowsIconProperty, ref _showsIcon, value);
		}
	}

	public bool ShowsLoading
	{
		get
		{
			return _showsLoading;
		}
		private set
		{
			SetAndRaise(ShowsLoadingProperty, ref _showsLoading, value);
		}
	}

	public IBrush? EffectiveIconForeground
	{
		get
		{
			return _effectiveIconForeground;
		}
		private set
		{
			SetAndRaise(EffectiveIconForegroundProperty, ref _effectiveIconForeground, value);
		}
	}

	protected override Type StyleKeyOverride => typeof(LuminaButtonPresenter);

	public LuminaButtonPresenter()
	{
		UpdateState();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_layoutRoot = e.NameScope.Find<Grid>("PART_LayoutRoot");
		_iconPresenter = e.NameScope.Find<Control>("PART_IconPresenter");
		_loadingPresenter = e.NameScope.Find<Control>("PART_LoadingPresenter");
		_contentPresenter = e.NameScope.Find<Control>("PART_ContentPresenter");
		UpdateState();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IconProperty || change.Property == ContentControl.ContentProperty || change.Property == IconForegroundProperty || change.Property == SyncIconForegroundProperty || change.Property == IconPlacementProperty || change.Property == IconSizeProperty || change.Property == IconSpacingProperty || change.Property == IsLoadingProperty || change.Property == TemplatedControl.ForegroundProperty)
		{
			UpdateState();
		}
	}

	private void UpdateState()
	{
		HasIcon = Icon != null;
		HasContent = base.Content != null && (!(base.Content is string text) || !string.IsNullOrWhiteSpace(text));
		ShowsLoading = IsLoading;
		ShowsIcon = HasIcon && !ShowsLoading;
		EffectiveIconForeground = IconForeground ?? base.Foreground;
		ApplyIconForeground();
		UpdateButtonLayout();
	}

	private void ApplyIconForeground()
	{
		if (SyncIconForeground && Icon != null)
		{
			ApplyForegroundToIcon(Icon, EffectiveIconForeground);
		}
	}

	private void ApplyForegroundToIcon(object icon, IBrush? foreground)
	{
		if (!(icon is TemplatedControl templatedControl))
		{
			if (!(icon is TextBlock textBlock))
			{
				if (!(icon is ContentPresenter contentPresenter))
				{
					if (!(icon is Shape shape))
					{
						if (icon is Panel panel)
						{
							{
								foreach (Control child in panel.Children)
								{
									ApplyForegroundToIcon(child, foreground);
								}
								return;
							}
						}
						Control? control;
						if (icon is Decorator decorator)
						{
							Control? child2 = decorator.Child;
							if (child2 != null)
							{
								ApplyForegroundToIcon(child2, foreground);
								return;
							}
							control = (Control)icon;
						}
						else
						{
							control = icon as Control;
							if (control == null)
							{
								return;
							}
						}
						SetIconValue(control, TextElement.ForegroundProperty, foreground);
					}
					else
					{
						SetIconValue(shape, Shape.FillProperty, foreground);
						SetIconValue(shape, Shape.StrokeProperty, foreground);
					}
				}
				else
				{
					SetIconValue(contentPresenter, ContentPresenter.ForegroundProperty, foreground);
				}
			}
			else
			{
				SetIconValue(textBlock, TextBlock.ForegroundProperty, foreground);
			}
			return;
		}
		SetIconValue(templatedControl, TemplatedControl.ForegroundProperty, foreground);
		if (templatedControl is ContentControl contentControl)
		{
			object? content = contentControl.Content;
			if (content != null && content != contentControl)
			{
				ApplyForegroundToIcon(content, foreground);
			}
		}
	}

	private static void SetIconValue<T>(AvaloniaObject target, StyledProperty<T> property, T value)
	{
		target.SetValue(property, value);
	}

	private void UpdateButtonLayout()
	{
		if (_layoutRoot == null || _iconPresenter == null || _loadingPresenter == null || _contentPresenter == null)
		{
			return;
		}
		bool hasBoth = (ShowsIcon || ShowsLoading) && HasContent;
		LuminaIconPlacement iconPlacement = IconPlacement;
		bool flag = (uint)iconPlacement <= 1u;
		bool isHorizontal = flag;
		iconPlacement = IconPlacement;
		flag = ((iconPlacement == LuminaIconPlacement.Left || iconPlacement == LuminaIconPlacement.Top) ? true : false);
		bool isIconFirst = flag;
		double spacing = (hasBoth ? Math.Max(0.0, IconSpacing) : 0.0);
		double iconSize = Math.Max(0.0, IconSize);
		_iconPresenter.Width = iconSize;
		_iconPresenter.Height = iconSize;
		_loadingPresenter.Width = iconSize;
		_loadingPresenter.Height = iconSize;
		_layoutRoot.ColumnDefinitions.Clear();
		_layoutRoot.RowDefinitions.Clear();
		if (isHorizontal)
		{
			_layoutRoot.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			_layoutRoot.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			if (hasBoth)
			{
				_layoutRoot.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
			}
			int iconColumn = ((hasBoth && !isIconFirst) ? 1 : 0);
			int contentColumn = ((hasBoth && isIconFirst) ? 1 : 0);
			MoveElement(_iconPresenter, 0, iconColumn);
			MoveElement(_loadingPresenter, 0, iconColumn);
			MoveElement(_contentPresenter, 0, contentColumn);
			Thickness iconMargin = ((!hasBoth) ? default(Thickness) : (isIconFirst ? new Thickness(0.0, 0.0, spacing, 0.0) : new Thickness(spacing, 0.0, 0.0, 0.0)));
			_iconPresenter.Margin = iconMargin;
			_loadingPresenter.Margin = iconMargin;
			_contentPresenter.Margin = default(Thickness);
		}
		else
		{
			_layoutRoot.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			_layoutRoot.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			if (hasBoth)
			{
				_layoutRoot.RowDefinitions.Add(new RowDefinition
				{
					Height = GridLength.Auto
				});
			}
			int iconRow = ((hasBoth && !isIconFirst) ? 1 : 0);
			int contentRow = ((hasBoth && isIconFirst) ? 1 : 0);
			MoveElement(_iconPresenter, iconRow, 0);
			MoveElement(_loadingPresenter, iconRow, 0);
			MoveElement(_contentPresenter, contentRow, 0);
			Thickness iconMargin2 = ((!hasBoth) ? default(Thickness) : (isIconFirst ? new Thickness(0.0, 0.0, 0.0, spacing) : new Thickness(0.0, spacing, 0.0, 0.0)));
			_iconPresenter.Margin = iconMargin2;
			_loadingPresenter.Margin = iconMargin2;
			_contentPresenter.Margin = default(Thickness);
		}
	}

	private static void MoveElement(Control control, int row, int column)
	{
		Grid.SetRow(control, row);
		Grid.SetColumn(control, column);
	}
}
