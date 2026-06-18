using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class TextInputShowcaseViewModel : ObservableObject
{
    private static readonly KeyGesture AlternateSaveGesture = new KeyGesture(Key.Enter, KeyModifiers.Control);

    private static readonly KeyGesture AlternatePaletteGesture = new KeyGesture(Key.P, KeyModifiers.Control);

    private bool _saveShortcutTriggered;

    private string? _lastShortcutActionKey;

    public ObservableCollection<KeyGesture> SaveGestures { get; } = new ObservableCollection<KeyGesture>();

    public ObservableCollection<KeyGesture> PaletteGestures { get; } = new ObservableCollection<KeyGesture>();

    public string SaveGestureStorageText => LuminaKeyGestureSerializer.Serialize(SaveGesture);

    public string SaveGestureStorageDisplayText => LuminaLocalization.Format(SandboxLocalization.TextInputsStorageFormat, SaveGestureStorageText);

    [ObservableProperty]
    private KeyGesture? _saveGesture = new KeyGesture(Key.S, KeyModifiers.Control);

    [ObservableProperty]
    private KeyGesture? _paletteGesture = new KeyGesture(Key.K, KeyModifiers.Control | KeyModifiers.Shift);

    [ObservableProperty]
    private KeyGesture? _renameGesture = new KeyGesture(Key.F2);

    [ObservableProperty]
    private string _shortcutStatus = string.Empty;

    [ObservableProperty]
    private string _multiShortcutStatus = string.Empty;

    public TextInputShowcaseViewModel()
    {
        RefreshSaveGestures();
        RefreshPaletteGestures();
        RefreshShortcutStatus();
        RefreshMultiShortcutStatus();
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    partial void OnSaveGestureChanged(KeyGesture? value)
    {
        RefreshSaveGestures();
        OnPropertyChanged(nameof(SaveGestureStorageText));
        OnPropertyChanged(nameof(SaveGestureStorageDisplayText));
        RefreshShortcutStatus();
    }

    partial void OnPaletteGestureChanged(KeyGesture? value)
    {
        RefreshPaletteGestures();
        RefreshMultiShortcutStatus();
    }

    partial void OnRenameGestureChanged(KeyGesture? value)
    {
        RefreshMultiShortcutStatus();
    }

    [RelayCommand]
    private void TriggerSaveShortcut()
    {
        _saveShortcutTriggered = true;
        RefreshShortcutStatus();
    }

    [RelayCommand]
    private void TriggerShortcutAction(string? actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
        {
            return;
        }

        _lastShortcutActionKey = actionKey;
        RefreshMultiShortcutStatus();
    }

    private void RefreshSaveGestures()
    {
        SaveGestures.Clear();
        AddGesture(SaveGestures, SaveGesture);
        AddGesture(SaveGestures, AlternateSaveGesture);
    }

    private void RefreshPaletteGestures()
    {
        PaletteGestures.Clear();
        AddGesture(PaletteGestures, PaletteGesture);
        AddGesture(PaletteGestures, AlternatePaletteGesture);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SaveGestureStorageDisplayText));
        RefreshShortcutStatus();
        RefreshMultiShortcutStatus();
    }

    private void RefreshShortcutStatus()
    {
        ShortcutStatus = _saveShortcutTriggered
            ? LuminaLocalization.Format(SandboxLocalization.TextInputsShortcutTriggeredFormat, SaveGestureStorageText)
            : LuminaLocalization.Get(SandboxLocalization.TextInputsShortcutStatusHint);
    }

    private void RefreshMultiShortcutStatus()
    {
        MultiShortcutStatus = string.IsNullOrWhiteSpace(_lastShortcutActionKey)
            ? LuminaLocalization.Get(SandboxLocalization.TextInputsMultipleShortcutStatusHint)
            : LuminaLocalization.Format(
                SandboxLocalization.TextInputsShortcutActionTriggeredFormat,
                LuminaLocalization.Get(_lastShortcutActionKey));
    }

    private static void AddGesture(ObservableCollection<KeyGesture> target, KeyGesture? gesture)
    {
        if (gesture != null && !target.Contains(gesture))
        {
            target.Add(gesture);
        }
    }
}
