using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class CardShowcaseViewModel : ObservableObject
{
    private WeakReference<Control>? _ownerReference;

    public CardShowcaseViewModel()
    {
        WindowGlassStatusText = LuminaLocalization.Get("Sandbox.Text.0220");
        CardGlassStatusText = LuminaLocalization.Get("Sandbox.Text.0015");
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private string _windowGlassStatusText;

    [ObservableProperty]
    private string _cardGlassStatusText;

    [ObservableProperty]
    private double _glassBackdropOffset = -48.0;

    [RelayCommand]
    private void MoveGlassBackdrop()
    {
        GlassBackdropOffset = GlassBackdropOffset < 0.0 ? 48.0 : -48.0;
    }

    [RelayCommand]
    private static void RefreshCachedGlass(LuminaCard? card)
    {
        card?.RefreshBackdrop();
    }

    [RelayCommand]
    private void RefreshGlass(object? parameter)
    {
        var owner = parameter switch
        {
            Control control => control,
            RoutedEventArgs { Source: Control source } => source,
            _ => null
        };

        if (owner != null)
        {
            _ownerReference = new WeakReference<Control>(owner);
        }

        UpdateGlassStatus(owner);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (_ownerReference?.TryGetTarget(out var owner) == true)
        {
            UpdateGlassStatus(owner);
        }
    }

    private void UpdateGlassStatus(Control? owner)
    {
        if (owner == null || TopLevel.GetTopLevel(owner) is not Window window)
        {
            WindowGlassStatusText = LuminaLocalization.Get(SandboxLocalization.WindowGlassUnavailable);
            CardGlassStatusText = LuminaLocalization.Get("Sandbox.Text.0018");
            return;
        }

        var actualTransparencyLevel = window.ActualTransparencyLevel;
        WindowGlassStatusText = WindowGlassSupport.GetWindowStatus(actualTransparencyLevel);
        CardGlassStatusText = WindowGlassSupport.GetCardStatus(actualTransparencyLevel);
    }
}
