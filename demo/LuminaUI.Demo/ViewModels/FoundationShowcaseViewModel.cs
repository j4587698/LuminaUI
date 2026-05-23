using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class FoundationShowcaseViewModel : ObservableObject
{
    public FoundationShowcaseViewModel()
    {
        RefreshValidationErrors();
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private string _invalidEmail = "duplicate@lumina.dev";

    [ObservableProperty]
    private IReadOnlyList<object> _invalidEmailErrors = [];

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshValidationErrors();
    }

    private void RefreshValidationErrors()
    {
        InvalidEmailErrors =
        [
            SandboxTextLocalizer.Localize("Email already exists."),
            SandboxTextLocalizer.Localize("Use a unique address before saving.")
        ];
    }
}
