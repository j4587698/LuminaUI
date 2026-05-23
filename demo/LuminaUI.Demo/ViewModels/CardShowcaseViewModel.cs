using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class CardShowcaseViewModel : ObservableObject
{
    private WeakReference<Control>? _ownerReference;

    public CardShowcaseViewModel()
    {
        WindowGlassStatusText = SandboxTextLocalizer.Localize("Checking window glass...");
        CardGlassStatusText = SandboxTextLocalizer.Localize("Checking card glass...");
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private string _windowGlassStatusText;

    [ObservableProperty]
    private string _cardGlassStatusText;

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
            CardGlassStatusText = SandboxTextLocalizer.Localize("Card glass: backdrop blur.");
            return;
        }

        var actualTransparencyLevel = window.ActualTransparencyLevel;
        WindowGlassStatusText = WindowGlassSupport.GetWindowStatus(actualTransparencyLevel);
        CardGlassStatusText = WindowGlassSupport.GetCardStatus(actualTransparencyLevel);
    }
}
