using System;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Controls;

public class LuminaPage : ContentPage
{
    public static readonly StyledProperty<LuminaPageState> PageStateProperty = AvaloniaProperty.Register<LuminaPage, LuminaPageState>(nameof(PageState), LuminaPageState.Normal);

    public static readonly StyledProperty<string?> NavigationKeyProperty = AvaloniaProperty.Register<LuminaPage, string?>(nameof(NavigationKey));

    public static readonly StyledProperty<object?> LoadingContentProperty = AvaloniaProperty.Register<LuminaPage, object?>(nameof(LoadingContent));

    public static readonly StyledProperty<object?> EmptyContentProperty = AvaloniaProperty.Register<LuminaPage, object?>(nameof(EmptyContent));

    public static readonly StyledProperty<object?> ErrorContentProperty = AvaloniaProperty.Register<LuminaPage, object?>(nameof(ErrorContent));

    public static readonly StyledProperty<object?> ShellTitleProperty = AvaloniaProperty.Register<LuminaPage, object?>(nameof(ShellTitle));

    public static readonly StyledProperty<object?> ShellSubtitleProperty = AvaloniaProperty.Register<LuminaPage, object?>(nameof(ShellSubtitle));

    public static readonly StyledProperty<object?> ShellActionsProperty = AvaloniaProperty.Register<LuminaPage, object?>(nameof(ShellActions));

    public static readonly StyledProperty<bool> CloseShellMenuOnNavigateProperty = AvaloniaProperty.Register<LuminaPage, bool>(nameof(CloseShellMenuOnNavigate), defaultValue: true);

    public static readonly StyledProperty<bool> ShowShellChromeProperty = AvaloniaProperty.Register<LuminaPage, bool>(nameof(ShowShellChrome), defaultValue: true);

    public static readonly StyledProperty<bool> ShowShellHeaderProperty = AvaloniaProperty.Register<LuminaPage, bool>(nameof(ShowShellHeader), defaultValue: true);

    protected override Type StyleKeyOverride => typeof(LuminaPage);

    public LuminaPageState PageState
    {
        get => GetValue(PageStateProperty);
        set => SetValue(PageStateProperty, value);
    }

    public string? NavigationKey
    {
        get => GetValue(NavigationKeyProperty);
        set => SetValue(NavigationKeyProperty, value);
    }

    public object? LoadingContent
    {
        get => GetValue(LoadingContentProperty);
        set => SetValue(LoadingContentProperty, value);
    }

    public object? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    public object? ErrorContent
    {
        get => GetValue(ErrorContentProperty);
        set => SetValue(ErrorContentProperty, value);
    }

    public object? ShellTitle
    {
        get => GetValue(ShellTitleProperty);
        set => SetValue(ShellTitleProperty, value);
    }

    public object? ShellSubtitle
    {
        get => GetValue(ShellSubtitleProperty);
        set => SetValue(ShellSubtitleProperty, value);
    }

    public object? ShellActions
    {
        get => GetValue(ShellActionsProperty);
        set => SetValue(ShellActionsProperty, value);
    }

    public bool CloseShellMenuOnNavigate
    {
        get => GetValue(CloseShellMenuOnNavigateProperty);
        set => SetValue(CloseShellMenuOnNavigateProperty, value);
    }

    public bool ShowShellChrome
    {
        get => GetValue(ShowShellChromeProperty);
        set => SetValue(ShowShellChromeProperty, value);
    }

    public bool ShowShellHeader
    {
        get => GetValue(ShowShellHeaderProperty);
        set => SetValue(ShowShellHeaderProperty, value);
    }

    public LuminaShell? Shell => FindShell();

    public LuminaTopView? TopView => LuminaTopView.FindFor(this);

    public LuminaTopView? OuterTopView => LuminaTopView.FindOuterFor(this);

    public void ShowToast(object? content)
    {
        FindOverlayHost()?.ShowToast(content);
    }

    public void ShowToast(object? content, TimeSpan duration)
    {
        FindOverlayHost()?.ShowToast(content, duration);
    }

    public void ClearToast()
    {
        FindOverlayHost()?.ClearToast();
    }

    public bool NavigateTo(string navigationKey)
    {
        return FindShell()?.NavigateTo(navigationKey) ?? false;
    }

    public void ShowDialog(object? content)
    {
        FindOverlayHost()?.ShowDialog(content);
    }

    public void CloseDialog()
    {
        FindOverlayHost()?.CloseDialog();
    }

    public void ShowBottomSheet(object? content)
    {
        FindOverlayHost()?.ShowBottomSheet(content);
    }

    public void CloseBottomSheet()
    {
        FindOverlayHost()?.CloseBottomSheet();
    }

    public void ShowDrawer(object? content)
    {
        FindOverlayHost()?.ShowDrawer(content);
    }

    public void CloseDrawer()
    {
        FindOverlayHost()?.CloseDrawer();
    }

    public void ShowTopToast(object? content)
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.ShowToast(content);
    }

    public void ShowTopToast(object? content, TimeSpan duration)
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.ShowToast(content, duration);
    }

    public void ClearTopToast()
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.ClearToast();
    }

    public void ShowTopDialog(object? content)
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.ShowDialog(content);
    }

    public void CloseTopDialog()
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.CloseDialog();
    }

    public void ShowTopBottomSheet(object? content)
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.ShowBottomSheet(content);
    }

    public void CloseTopBottomSheet()
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.CloseBottomSheet();
    }

    public void ShowTopDrawer(object? content)
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.ShowDrawer(content);
    }

    public void CloseTopDrawer()
    {
        LuminaOverlayHostResolver.FindTopFor(this)?.CloseDrawer();
    }

    private LuminaShell? FindShell()
    {
        return LuminaShell.FindFor(this);
    }

    private ILuminaOverlayHost? FindOverlayHost()
    {
        return LuminaOverlayHostResolver.FindFor(this);
    }
}
