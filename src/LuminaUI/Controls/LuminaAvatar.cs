using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaAvatar : ContentControl
{
    private bool _hasSource;

    private bool _hasCustomContent;

    private bool _hasInitials;

    private bool _hasBadge;

    private string? _initialsText;

    public static readonly StyledProperty<IImage?> SourceProperty = AvaloniaProperty.Register<LuminaAvatar, IImage?>(nameof(Source));

    public static readonly StyledProperty<string?> DisplayNameProperty = AvaloniaProperty.Register<LuminaAvatar, string?>(nameof(DisplayName));

    public static readonly StyledProperty<string?> InitialsProperty = AvaloniaProperty.Register<LuminaAvatar, string?>(nameof(Initials));

    public static readonly StyledProperty<object?> BadgeProperty = AvaloniaProperty.Register<LuminaAvatar, object?>(nameof(Badge));

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<LuminaAvatar, double>(nameof(Size), 40.0);

    public static readonly DirectProperty<LuminaAvatar, bool> HasSourceProperty = AvaloniaProperty.RegisterDirect<LuminaAvatar, bool>(nameof(HasSource), (LuminaAvatar avatar) => avatar.HasSource, null, unsetValue: false);

    public static readonly DirectProperty<LuminaAvatar, bool> HasCustomContentProperty = AvaloniaProperty.RegisterDirect<LuminaAvatar, bool>(nameof(HasCustomContent), (LuminaAvatar avatar) => avatar.HasCustomContent, null, unsetValue: false);

    public static readonly DirectProperty<LuminaAvatar, bool> HasInitialsProperty = AvaloniaProperty.RegisterDirect<LuminaAvatar, bool>(nameof(HasInitials), (LuminaAvatar avatar) => avatar.HasInitials, null, unsetValue: false);

    public static readonly DirectProperty<LuminaAvatar, string?> InitialsTextProperty = AvaloniaProperty.RegisterDirect<LuminaAvatar, string?>(nameof(InitialsText), (LuminaAvatar avatar) => avatar.InitialsText);

    public static readonly DirectProperty<LuminaAvatar, bool> HasBadgeProperty = AvaloniaProperty.RegisterDirect<LuminaAvatar, bool>(nameof(HasBadge), (LuminaAvatar avatar) => avatar.HasBadge, null, unsetValue: false);

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string? DisplayName
    {
        get => GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public string? Initials
    {
        get => GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    public object? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool HasSource
    {
        get
        {
            return _hasSource;
        }
        private set
        {
            SetAndRaise(HasSourceProperty, ref _hasSource, value);
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

    public bool HasInitials
    {
        get
        {
            return _hasInitials;
        }
        private set
        {
            SetAndRaise(HasInitialsProperty, ref _hasInitials, value);
        }
    }

    public string? InitialsText
    {
        get
        {
            return _initialsText;
        }
        private set
        {
            SetAndRaise(InitialsTextProperty, ref _initialsText, value);
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

    public LuminaAvatar()
    {
        UpdateVisualState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty || change.Property == ContentControl.ContentProperty || change.Property == InitialsProperty || change.Property == DisplayNameProperty || change.Property == BadgeProperty)
        {
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        HasSource = Source != null;
        HasCustomContent = !HasSource && Content != null;
        InitialsText = ResolveInitials();
        HasInitials = !HasSource && !HasCustomContent && !string.IsNullOrWhiteSpace(InitialsText);
        HasBadge = Badge != null && (Badge is not string text || !string.IsNullOrWhiteSpace(text));
    }

    private string ResolveInitials()
    {
        if (!string.IsNullOrWhiteSpace(Initials))
        {
            return Initials.Trim();
        }
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            return "?";
        }
        string[] parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }
        return (parts.Length == 1) ? parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant() : string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant();
    }
}
