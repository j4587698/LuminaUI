using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace LuminaUI.Controls;

internal sealed class LuminaOverlayInputPaneAvoidance
{
    private readonly Control _owner;
    private readonly Func<bool> _isDialogOpen;
    private readonly Func<bool> _isBottomSheetOpen;

    private TopLevel? _topLevel;
    private IInputPane? _inputPane;
    private Control? _dialogContainer;
    private Control? _bottomSheetContainer;
    private Thickness _dialogContainerBaseMargin;
    private Thickness _bottomSheetContainerBaseMargin;
    private bool _isDialogMarginApplied;
    private bool _isBottomSheetMarginApplied;
    private InputPaneState _inputPaneState = InputPaneState.Closed;
    private Rect _occludedRect;

    public LuminaOverlayInputPaneAvoidance(Control owner, Func<bool> isDialogOpen, Func<bool> isBottomSheetOpen)
    {
        _owner = owner;
        _isDialogOpen = isDialogOpen;
        _isBottomSheetOpen = isBottomSheetOpen;
    }

    public void AttachToVisualTree()
    {
        _topLevel = TopLevel.GetTopLevel(_owner);
        _inputPane = _topLevel?.InputPane;
        SyncInputPaneSubscription();
        UpdateInputPaneState();
        UpdateMargins();
    }

    public void ApplyTemplate(Control? dialogContainer, Control? bottomSheetContainer)
    {
        ResetMargins();

        _dialogContainer = dialogContainer;
        _bottomSheetContainer = bottomSheetContainer;
        _dialogContainerBaseMargin = dialogContainer?.Margin ?? default;
        _bottomSheetContainerBaseMargin = bottomSheetContainer?.Margin ?? default;
        _isDialogMarginApplied = false;
        _isBottomSheetMarginApplied = false;

        UpdateMargins();
    }

    public void DetachFromVisualTree()
    {
        DetachInputPane();
        ResetMargins();

        _topLevel = null;
        _inputPane = null;
        _dialogContainer = null;
        _bottomSheetContainer = null;
        _inputPaneState = InputPaneState.Closed;
        _occludedRect = default;
    }

    public void UpdateOverlayState()
    {
        UpdateInputPaneState();
        UpdateMargins();
    }

    private void SyncInputPaneSubscription()
    {
        DetachInputPane();

        if (_inputPane == null)
        {
            _inputPaneState = InputPaneState.Closed;
            _occludedRect = default;
            return;
        }

        _inputPane.StateChanged += OnInputPaneStateChanged;
    }

    private void DetachInputPane()
    {
        if (_inputPane != null)
        {
            _inputPane.StateChanged -= OnInputPaneStateChanged;
        }
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        _inputPaneState = e.NewState;
        _occludedRect = e.EndRect;
        UpdateMargins();
    }

    private void UpdateInputPaneState()
    {
        if (_inputPane == null)
        {
            _inputPaneState = InputPaneState.Closed;
            _occludedRect = default;
            return;
        }

        _inputPaneState = _inputPane.State;
        _occludedRect = _inputPane.OccludedRect;
    }

    private void UpdateMargins()
    {
        double bottomInset = ResolveBottomInset();
        UpdateTargetMargin(_dialogContainer, _dialogContainerBaseMargin, _isDialogOpen(), bottomInset, ref _isDialogMarginApplied);
        UpdateTargetMargin(_bottomSheetContainer, _bottomSheetContainerBaseMargin, _isBottomSheetOpen(), bottomInset, ref _isBottomSheetMarginApplied);
    }

    private void UpdateTargetMargin(Control? target, Thickness baseMargin, bool isOpen, double bottomInset, ref bool isApplied)
    {
        if (target == null)
        {
            isApplied = false;
            return;
        }

        if (!isOpen || bottomInset <= 0)
        {
            if (isApplied)
            {
                target.Margin = baseMargin;
                isApplied = false;
            }
            return;
        }

        target.Margin = new Thickness(
            baseMargin.Left,
            baseMargin.Top,
            baseMargin.Right,
            baseMargin.Bottom + bottomInset);
        isApplied = true;
    }

    private void ResetMargins()
    {
        if (_dialogContainer != null && _isDialogMarginApplied)
        {
            _dialogContainer.Margin = _dialogContainerBaseMargin;
            _isDialogMarginApplied = false;
        }

        if (_bottomSheetContainer != null && _isBottomSheetMarginApplied)
        {
            _bottomSheetContainer.Margin = _bottomSheetContainerBaseMargin;
            _isBottomSheetMarginApplied = false;
        }
    }

    private double ResolveBottomInset()
    {
        if (_inputPaneState != InputPaneState.Open || _topLevel == null || _occludedRect.Height <= 0)
        {
            return 0;
        }

        return Math.Max(0, _topLevel.ClientSize.Height - _occludedRect.Top);
    }
}
