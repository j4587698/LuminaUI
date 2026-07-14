using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using LuminaUI.Controls;

namespace LuminaUI.Demo.Views;

public partial class DemoFloatingWindow : LuminaFloatingWindow
{
    public DemoFloatingWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PositionAtScreenBottom();
        UpdatePassthroughState();
    }

    private void OnDrag(object? sender, PointerPressedEventArgs e)
    {
        BeginDrag(e);
    }

    private void OnToggleLock(object? sender, RoutedEventArgs e)
    {
        IsLocked = !IsLocked;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
