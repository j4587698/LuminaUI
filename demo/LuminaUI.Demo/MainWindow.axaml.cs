using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Media;
using LuminaUI.Controls;
using LuminaUI.Localization;
using LuminaUI.Theming;

namespace LuminaUI.Demo;

public partial class MainWindow : LuminaWindow
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateWindowTitle();
        UpdateThemeButtonState();

        LuminaLocalization.LanguageChanged += OnLanguageChanged;
        LuminaThemeManager.ThemeModeChanged += OnThemeModeChanged;
        ActualThemeVariantChanged += OnActualThemeVariantChanged;

        if (OperatingSystem.IsMacOS())
        {
            var brand = this.FindControl<StackPanel>("TitleBarBrand");
            if (brand != null)
            {
                brand.IsVisible = false;
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        LuminaLocalization.LanguageChanged -= OnLanguageChanged;
        LuminaThemeManager.ThemeModeChanged -= OnThemeModeChanged;
        ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        base.OnClosed(e);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateWindowTitle();
    }

    private void PlatformNativeMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (sender is not NativeMenuItem { CommandParameter: string action })
        {
            return;
        }

        switch (action)
        {
            case "NewWindow":
                new MainWindow().Show();
                break;
            case "OpenSandbox":
                NavigateFromMenu("NavDesignSystem", SandboxLocalization.MenuActionOpenedSandbox);
                break;
            case "CloseWindow":
                Close();
                break;
            case "ToggleSidebar":
                ToggleSidebarFromMenu();
                break;
            case "OpenComponents":
                NavigateFromMenu("NavButtons", SandboxLocalization.MenuActionOpenedComponents);
                break;
            case "OpenSettings":
                NavigateFromMenu("NavSettings", SandboxLocalization.MenuActionOpenedSettings);
                break;
            case "OpenDocumentation":
                NavigateFromMenu("NavLocalizationResources", SandboxLocalization.MenuActionOpenedDocumentation);
                break;
            case "OpenAbout":
                SandboxRoot.ShowAboutDialog();
                break;
        }
    }

    private void ThemeModeSystemMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        SetThemeMode(LuminaThemeMode.System);
    }

    private void ThemeModeLightMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        SetThemeMode(LuminaThemeMode.Light);
    }

    private void ThemeModeDarkMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        SetThemeMode(LuminaThemeMode.Dark);
    }

    private void OnThemeModeChanged(object? sender, EventArgs e)
    {
        UpdateThemeButtonState();
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeButtonState();
    }

    private void NavigateFromMenu(string routeKey, string messageKey)
    {
        SandboxRoot.NavigateToRoute(routeKey, closeMenuOnNavigate: false);
        SandboxRoot.ShowMenuNotification(
            LuminaLocalization.Get(SandboxLocalization.AppTitle),
            LuminaLocalization.Get(messageKey),
            NotificationType.Information);
    }

    private void ToggleSidebarFromMenu()
    {
        var isOpen = SandboxRoot.ToggleShellMenu();
        var messageKey = isOpen
            ? SandboxLocalization.MenuActionSidebarOpened
            : SandboxLocalization.MenuActionSidebarClosed;

        SandboxRoot.ShowMenuNotification(
            LuminaLocalization.Get(SandboxLocalization.MenuToggleSidebar),
            LuminaLocalization.Get(messageKey),
            NotificationType.Information);
    }

    private void UpdateWindowTitle()
    {
        Title = LuminaLocalization.Get(SandboxLocalization.AppTitle);
    }

    private void SetThemeMode(LuminaThemeMode themeMode)
    {
        LuminaThemeManager.SetThemeMode(themeMode);
        RefreshWindowMaterial();
        UpdateThemeButtonState();
    }

    private void UpdateThemeButtonState()
    {
        var themeMode = LuminaThemeManager.CurrentThemeMode;

        TitleBarSystemIcon.IsVisible = themeMode == LuminaThemeMode.System;
        TitleBarSunIcon.IsVisible = themeMode == LuminaThemeMode.Light;
        TitleBarMoonIcon.IsVisible = themeMode == LuminaThemeMode.Dark;

        TitleBarThemeSystemMenuItem.FontWeight = themeMode == LuminaThemeMode.System ? FontWeight.DemiBold : FontWeight.Normal;
        TitleBarThemeLightMenuItem.FontWeight = themeMode == LuminaThemeMode.Light ? FontWeight.DemiBold : FontWeight.Normal;
        TitleBarThemeDarkMenuItem.FontWeight = themeMode == LuminaThemeMode.Dark ? FontWeight.DemiBold : FontWeight.Normal;
    }
}
