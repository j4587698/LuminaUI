using System;
using System.Threading.Tasks;
using Avalonia;
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
        RebuildPlatformMenu();
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

    protected override async Task<bool> CanClose()
    {
        var result = await LuminaWindowMessageBox.ShowAsync(
            this,
            SandboxTextLocalizer.Localize("Confirm Exit"),
            SandboxTextLocalizer.Localize("Are you sure you want to exit the application?"),
            LuminaDialogButtons.YesNo,
            LuminaMessageBoxIcon.Question);

        return result == LuminaDialogResult.Yes;
    }

    protected override object? CreateDefaultAboutDialogContent()
    {
        return new LuminaUI.Demo.Views.Popups.AboutDialogView();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateWindowTitle();
        RebuildPlatformMenu();
    }

    private void RebuildPlatformMenu()
    {
        PlatformMenu.NativeMenu = CreatePlatformMenu();
    }

    private NativeMenu CreatePlatformMenu()
    {
        return new NativeMenu
        {
            Items =
            {
                CreateNativeMenuGroup(
                    SandboxLocalization.MenuFile,
                    CreateNativeMenuItem(SandboxLocalization.MenuNewWindow, "NewWindow"),
                    CreateNativeMenuItem(SandboxLocalization.MenuOpenSandbox, "OpenSandbox"),
                    new NativeMenuItemSeparator(),
                    CreateNativeMenuItem(SandboxLocalization.MenuClose, "CloseWindow")),
                CreateNativeMenuGroup(
                    SandboxLocalization.MenuView,
                    CreateNativeMenuItem(SandboxLocalization.MenuToggleSidebar, "ToggleSidebar"),
                    CreateNativeMenuItem(SandboxLocalization.MenuComponents, "OpenComponents"),
                    CreateNativeMenuItem(SandboxLocalization.MenuSettings, "OpenSettings")),
                CreateNativeMenuGroup(
                    SandboxLocalization.MenuHelp,
                    CreateNativeMenuItem(SandboxLocalization.MenuDocumentation, "OpenDocumentation"),
                    CreateNativeMenuItem(SandboxLocalization.MenuAbout, "OpenAbout"))
            }
        };
    }

    private static NativeMenuItem CreateNativeMenuGroup(string headerKey, params NativeMenuItemBase[] items)
    {
        var menu = new NativeMenu();
        foreach (var item in items)
        {
            menu.Items.Add(item);
        }

        return new NativeMenuItem
        {
            Header = LuminaLocalization.Get(headerKey),
            Menu = menu
        };
    }

    private NativeMenuItem CreateNativeMenuItem(string headerKey, string action)
    {
        var item = new NativeMenuItem
        {
            Header = LuminaLocalization.Get(headerKey),
            CommandParameter = action
        };
        item.Click += PlatformNativeMenuItem_OnClick;
        return item;
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
