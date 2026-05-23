using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaBadge : ContentControl
{
	private bool _hasBadge;

	private bool _hasBadgeText;

	private bool _hasWrappedContent;

	private string? _displayText;

	private Thickness _contentMargin;

	private Control? _badgeContainer;

	public static readonly StyledProperty<object?> BadgeContentProperty = AvaloniaProperty.Register<LuminaBadge, object?>("BadgeContent");

	public static readonly StyledProperty<int?> CountProperty = AvaloniaProperty.Register<LuminaBadge, int?>("Count");

	public static readonly StyledProperty<int> MaxCountProperty = AvaloniaProperty.Register<LuminaBadge, int>("MaxCount", 99);

	public static readonly StyledProperty<int> OverflowCountProperty = AvaloniaProperty.Register<LuminaBadge, int>("OverflowCount", 0);

	public static readonly StyledProperty<bool> ShowZeroProperty = AvaloniaProperty.Register<LuminaBadge, bool>("ShowZero", defaultValue: false);

	public static readonly StyledProperty<bool> IsDotProperty = AvaloniaProperty.Register<LuminaBadge, bool>("IsDot", defaultValue: false);

	public static readonly StyledProperty<LuminaBadgeCornerPosition> CornerPositionProperty = AvaloniaProperty.Register<LuminaBadge, LuminaBadgeCornerPosition>("CornerPosition", LuminaBadgeCornerPosition.TopRight);

	public static readonly StyledProperty<bool> ReserveBadgeSpaceProperty = AvaloniaProperty.Register<LuminaBadge, bool>("ReserveBadgeSpace", defaultValue: false);

	public static readonly StyledProperty<double> BadgeFontSizeProperty = AvaloniaProperty.Register<LuminaBadge, double>("BadgeFontSize", 10.0);

	public static readonly StyledProperty<double> BadgeMinWidthProperty = AvaloniaProperty.Register<LuminaBadge, double>("BadgeMinWidth", 18.0);

	public static readonly StyledProperty<double> BadgeMinHeightProperty = AvaloniaProperty.Register<LuminaBadge, double>("BadgeMinHeight", 18.0);

	public static readonly StyledProperty<Thickness> BadgePaddingProperty = AvaloniaProperty.Register<LuminaBadge, Thickness>("BadgePadding", new Thickness(5.0, 0.0));

	public static readonly StyledProperty<double> OffsetXProperty = AvaloniaProperty.Register<LuminaBadge, double>("OffsetX", 0.0);

	public static readonly StyledProperty<double> OffsetYProperty = AvaloniaProperty.Register<LuminaBadge, double>("OffsetY", 0.0);

	public static readonly StyledProperty<ControlTheme?> BadgeThemeProperty = AvaloniaProperty.Register<LuminaBadge, ControlTheme?>("BadgeTheme");

	public static readonly DirectProperty<LuminaBadge, bool> HasBadgeProperty = AvaloniaProperty.RegisterDirect<LuminaBadge, bool>("HasBadge", (LuminaBadge badge) => badge.HasBadge, null, unsetValue: false);

	public static readonly DirectProperty<LuminaBadge, bool> HasBadgeTextProperty = AvaloniaProperty.RegisterDirect<LuminaBadge, bool>("HasBadgeText", (LuminaBadge badge) => badge.HasBadgeText, null, unsetValue: false);

	public static readonly DirectProperty<LuminaBadge, bool> HasWrappedContentProperty = AvaloniaProperty.RegisterDirect<LuminaBadge, bool>("HasWrappedContent", (LuminaBadge badge) => badge.HasWrappedContent, null, unsetValue: false);

	public static readonly DirectProperty<LuminaBadge, string?> DisplayTextProperty = AvaloniaProperty.RegisterDirect<LuminaBadge, string?>("DisplayText", (LuminaBadge badge) => badge.DisplayText);

	public static readonly DirectProperty<LuminaBadge, Thickness> ContentMarginProperty = AvaloniaProperty.RegisterDirect<LuminaBadge, Thickness>("ContentMargin", (LuminaBadge badge) => badge.ContentMargin);

	public object? BadgeContent
	{
		get
		{
			return GetValue(BadgeContentProperty);
		}
		set
		{
			SetValue(BadgeContentProperty, value);
		}
	}

	public int? Count
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

	public int MaxCount
	{
		get
		{
			return GetValue(MaxCountProperty);
		}
		set
		{
			SetValue(MaxCountProperty, value);
		}
	}

	public int OverflowCount
	{
		get
		{
			return GetValue(OverflowCountProperty);
		}
		set
		{
			SetValue(OverflowCountProperty, value);
		}
	}

	public bool ShowZero
	{
		get
		{
			return GetValue(ShowZeroProperty);
		}
		set
		{
			SetValue(ShowZeroProperty, value);
		}
	}

	public bool IsDot
	{
		get
		{
			return GetValue(IsDotProperty);
		}
		set
		{
			SetValue(IsDotProperty, value);
		}
	}

	public LuminaBadgeCornerPosition CornerPosition
	{
		get
		{
			return GetValue(CornerPositionProperty);
		}
		set
		{
			SetValue(CornerPositionProperty, value);
		}
	}

	public bool ReserveBadgeSpace
	{
		get
		{
			return GetValue(ReserveBadgeSpaceProperty);
		}
		set
		{
			SetValue(ReserveBadgeSpaceProperty, value);
		}
	}

	public double BadgeFontSize
	{
		get
		{
			return GetValue(BadgeFontSizeProperty);
		}
		set
		{
			SetValue(BadgeFontSizeProperty, value);
		}
	}

	public double BadgeMinWidth
	{
		get
		{
			return GetValue(BadgeMinWidthProperty);
		}
		set
		{
			SetValue(BadgeMinWidthProperty, value);
		}
	}

	public double BadgeMinHeight
	{
		get
		{
			return GetValue(BadgeMinHeightProperty);
		}
		set
		{
			SetValue(BadgeMinHeightProperty, value);
		}
	}

	public Thickness BadgePadding
	{
		get
		{
			return GetValue(BadgePaddingProperty);
		}
		set
		{
			SetValue(BadgePaddingProperty, value);
		}
	}

	public double OffsetX
	{
		get
		{
			return GetValue(OffsetXProperty);
		}
		set
		{
			SetValue(OffsetXProperty, value);
		}
	}

	public double OffsetY
	{
		get
		{
			return GetValue(OffsetYProperty);
		}
		set
		{
			SetValue(OffsetYProperty, value);
		}
	}

	public ControlTheme? BadgeTheme
	{
		get
		{
			return GetValue(BadgeThemeProperty);
		}
		set
		{
			SetValue(BadgeThemeProperty, value);
		}
	}

	public bool HasBadge
	{
		get
		{
			return _hasBadge;
		}
		private set
		{
			SetAndRaise(HasBadgeProperty, ref _hasBadge, value);
		}
	}

	public bool HasBadgeText
	{
		get
		{
			return _hasBadgeText;
		}
		private set
		{
			SetAndRaise(HasBadgeTextProperty, ref _hasBadgeText, value);
		}
	}

	public bool HasWrappedContent
	{
		get
		{
			return _hasWrappedContent;
		}
		private set
		{
			SetAndRaise(HasWrappedContentProperty, ref _hasWrappedContent, value);
		}
	}

	public string? DisplayText
	{
		get
		{
			return _displayText;
		}
		private set
		{
			SetAndRaise(DisplayTextProperty, ref _displayText, value);
		}
	}

	public Thickness ContentMargin
	{
		get
		{
			return _contentMargin;
		}
		private set
		{
			SetAndRaise(ContentMarginProperty, ref _contentMargin, value);
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_badgeContainer != null)
		{
			_badgeContainer.SizeChanged -= OnBadgeSizeChanged;
		}
		base.OnApplyTemplate(e);
		_badgeContainer = e.NameScope.FindRequired<Control>("PART_Badge");
		if (_badgeContainer != null)
		{
			_badgeContainer.SizeChanged += OnBadgeSizeChanged;
		}
		UpdateBadgePlacement();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (_badgeContainer != null)
		{
			_badgeContainer.SizeChanged -= OnBadgeSizeChanged;
			_badgeContainer = null;
		}
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == BadgeContentProperty || change.Property == CountProperty || change.Property == MaxCountProperty || change.Property == OverflowCountProperty || change.Property == ShowZeroProperty || change.Property == IsDotProperty || change.Property == ContentControl.ContentProperty)
		{
			UpdateState();
		}
		else if (change.Property == CornerPositionProperty || change.Property == ReserveBadgeSpaceProperty || change.Property == BadgeMinWidthProperty || change.Property == BadgeMinHeightProperty || change.Property == OffsetXProperty || change.Property == OffsetYProperty)
		{
			UpdateBadgePlacement();
		}
	}

	private void UpdateState()
	{
		HasWrappedContent = base.Content != null;
		if (IsDot)
		{
			DisplayText = null;
			HasBadgeText = false;
			HasBadge = true;
			UpdateBadgePlacement();
		}
		else
		{
			DisplayText = ResolveText();
			HasBadgeText = !string.IsNullOrWhiteSpace(DisplayText);
			HasBadge = HasBadgeText;
			UpdateBadgePlacement();
		}
	}

	private string? ResolveText()
	{
		if (BadgeContent != null)
		{
			return BadgeContent.ToString();
		}
		if (!Count.HasValue || (Count == 0 && !ShowZero))
		{
			return null;
		}
		int max = Math.Max(1, (OverflowCount > 0) ? OverflowCount : MaxCount);
		return (Count > max) ? $"{max}+" : Count.Value.ToString();
	}

	private void OnBadgeSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		UpdateBadgePlacement();
	}

	private void UpdateBadgePlacement()
	{
		if (_badgeContainer == null)
		{
			return;
		}
		LuminaBadgeCornerPosition cornerPosition = CornerPosition;
		bool flag = ((cornerPosition == LuminaBadgeCornerPosition.TopRight || cornerPosition == LuminaBadgeCornerPosition.BottomRight) ? true : false);
		bool isRight = flag;
		cornerPosition = CornerPosition;
		flag = (uint)(cornerPosition - 2) <= 1u;
		bool isBottom = flag;
		_badgeContainer.HorizontalAlignment = ((!isRight) ? HorizontalAlignment.Left : HorizontalAlignment.Right);
		_badgeContainer.VerticalAlignment = ((!isBottom) ? VerticalAlignment.Top : VerticalAlignment.Bottom);
		if (!HasWrappedContent)
		{
			ContentMargin = default(Thickness);
			_badgeContainer.RenderTransform = ((OffsetX == 0.0 && OffsetY == 0.0) ? null : new TranslateTransform(OffsetX, OffsetY));
			return;
		}
		double badgeWidth = ResolveBadgeWidth();
		double badgeHeight = ResolveBadgeHeight();
		int horizontal = (isRight ? 1 : (-1));
		int vertical = (isBottom ? 1 : (-1));
		if (ReserveBadgeSpace)
		{
			ContentMargin = new Thickness(badgeWidth / 2.0, badgeHeight / 2.0, badgeWidth / 2.0, badgeHeight / 2.0);
			_badgeContainer.RenderTransform = ((OffsetX == 0.0 && OffsetY == 0.0) ? null : new TranslateTransform(OffsetX, OffsetY));
		}
		else
		{
			ContentMargin = default(Thickness);
			_badgeContainer.RenderTransform = new TranslateTransform((double)horizontal * badgeWidth / 2.0 + OffsetX, (double)vertical * badgeHeight / 2.0 + OffsetY);
		}
	}

	private double ResolveBadgeWidth()
	{
		Control? badgeContainer = _badgeContainer;
		if (badgeContainer != null && badgeContainer.Bounds.Width > 0.0)
		{
			return badgeContainer.Bounds.Width;
		}
		return IsDot ? 10.0 : BadgeMinWidth;
	}

	private double ResolveBadgeHeight()
	{
		Control? badgeContainer = _badgeContainer;
		if (badgeContainer != null && badgeContainer.Bounds.Height > 0.0)
		{
			return badgeContainer.Bounds.Height;
		}
		return IsDot ? 10.0 : BadgeMinHeight;
	}
}
