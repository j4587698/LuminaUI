using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace LuminaUI.Controls;

[PseudoClasses(":selected")]
public class LuminaRatingItem : TemplatedControl
{
    public const string PART_SelectedClip = "PART_SelectedClip";

    public const string PC_Selected = ":selected";

    private Border? _selectedClip;

    private double _selectedWidth;

    private double _selectedRatio;

    private bool _isHalfPreview;

    public static readonly StyledProperty<object?> CharacterProperty = AvaloniaProperty.Register<LuminaRatingItem, object?>(nameof(Character), "★");

    public static readonly StyledProperty<bool> AllowHalfProperty = AvaloniaProperty.Register<LuminaRatingItem, bool>(nameof(AllowHalf), defaultValue: false);

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<LuminaRatingItem, double>(nameof(Size), 24.0);

    public static readonly DirectProperty<LuminaRatingItem, double> SelectedWidthProperty = AvaloniaProperty.RegisterDirect<LuminaRatingItem, double>(nameof(SelectedWidth), (LuminaRatingItem item) => item.SelectedWidth, null, 0.0);

    internal LuminaRating? Owner { get; set; }

    public object? Character
    {
        get => GetValue(CharacterProperty);
        set => SetValue(CharacterProperty, value);
    }

    public bool AllowHalf
    {
        get => GetValue(AllowHalfProperty);
        set => SetValue(AllowHalfProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double SelectedWidth
    {
        get
        {
            return _selectedWidth;
        }
        private set
        {
            SetAndRaise(SelectedWidthProperty, ref _selectedWidth, value);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _selectedClip = e.NameScope.Find<Border>("PART_SelectedClip");
        UpdateSelectedWidth();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Visual.BoundsProperty)
        {
            UpdateSelectedWidth();
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        UpdateHalfPreview(e);
        Owner?.PreviewItem(this, _isHalfPreview);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        UpdateHalfPreview(e);
        Owner?.PreviewItem(this, _isHalfPreview);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Owner?.RestoreValue();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        UpdateHalfPreview(e);
        Owner?.CommitItem(this, _isHalfPreview);
        e.Handled = true;
    }

    internal void SetSelectedRatio(double ratio)
    {
        _selectedRatio = Math.Clamp(ratio, 0.0, 1.0);
        PseudoClasses.Set(":selected", _selectedRatio > 0.0);
        UpdateSelectedWidth();
    }

    private void UpdateHalfPreview(PointerEventArgs e)
    {
        if (!AllowHalf)
        {
            _isHalfPreview = false;
        }
        else
        {
            _isHalfPreview = e.GetPosition(this).X <= Bounds.Width * 0.5;
        }
    }

    private void UpdateSelectedWidth()
    {
        SelectedWidth = Math.Max(0.0, Bounds.Width * _selectedRatio);
        if (_selectedClip != null)
        {
            _selectedClip.Width = SelectedWidth;
        }
    }
}
