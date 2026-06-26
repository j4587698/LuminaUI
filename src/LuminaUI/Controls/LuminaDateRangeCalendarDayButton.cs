using System;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;

namespace LuminaUI.Controls;

[PseudoClasses(":pressed", ":range-start", ":range-end", ":range-between", ":range-selected", ":range-preview-endpoint", ":range-single")]
public class LuminaDateRangeCalendarDayButton : ContentControl
{
    private bool _isPointerPressedInside;

    private bool _isRangeStart;

    private bool _isRangeEnd;

    private bool _isRangeBetween;

    private bool _isRangeSelected;

    private bool _isRangePreviewEndpoint;

    private bool _isRangeSingle;

    public DateTime Date { get; set; }

    public bool IsRangeStart
    {
        get
        {
            return _isRangeStart;
        }
        set
        {
            SetPseudoClass(ref _isRangeStart, ":range-start", value);
        }
    }

    public bool IsRangeEnd
    {
        get
        {
            return _isRangeEnd;
        }
        set
        {
            SetPseudoClass(ref _isRangeEnd, ":range-end", value);
        }
    }

    public bool IsRangeBetween
    {
        get
        {
            return _isRangeBetween;
        }
        set
        {
            SetPseudoClass(ref _isRangeBetween, ":range-between", value);
        }
    }

    public bool IsRangeSelected
    {
        get
        {
            return _isRangeSelected;
        }
        set
        {
            SetPseudoClass(ref _isRangeSelected, ":range-selected", value);
        }
    }

    public bool IsRangePreviewEndpoint
    {
        get
        {
            return _isRangePreviewEndpoint;
        }
        set
        {
            SetPseudoClass(ref _isRangePreviewEndpoint, ":range-preview-endpoint", value);
        }
    }

    public bool IsRangeSingle
    {
        get
        {
            return _isRangeSingle;
        }
        set
        {
            SetPseudoClass(ref _isRangeSingle, ":range-single", value);
        }
    }

    public event EventHandler<LuminaDateRangeCalendarDateEventArgs>? DateSelected;

    public event EventHandler<LuminaDateRangeCalendarDateEventArgs>? DatePreviewed;

    internal void ResetRangeState()
    {
        IsRangeStart = false;
        IsRangeEnd = false;
        IsRangeBetween = false;
        IsRangeSelected = false;
        IsRangePreviewEndpoint = false;
        IsRangeSingle = false;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (IsEnabled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            PseudoClasses.Set(":pressed", value: true);
            _isPointerPressedInside = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        bool shouldSelect = _isPointerPressedInside;
        _isPointerPressedInside = false;
        PseudoClasses.Set(":pressed", value: false);
        if (IsEnabled && shouldSelect)
        {
            DateSelected?.Invoke(this, new LuminaDateRangeCalendarDateEventArgs(Date));
            e.Handled = true;
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (IsEnabled)
        {
            DatePreviewed?.Invoke(this, new LuminaDateRangeCalendarDateEventArgs(Date));
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerPressedInside = false;
        PseudoClasses.Set(":pressed", value: false);
    }

    private void SetPseudoClass(ref bool field, string pseudoClass, bool value)
    {
        field = value;
        PseudoClasses.Set(pseudoClass, value);
    }
}
