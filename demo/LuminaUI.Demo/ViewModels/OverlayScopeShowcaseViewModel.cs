using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;
using LuminaUI.Localization;
using LuminaUI.Demo.Views;

namespace LuminaUI.Demo.ViewModels;

public partial class OverlayScopeShowcaseViewModel : ObservableObject
{
    private readonly Control _owner;
    private readonly LuminaShell _scopeShell;

    public OverlayScopeShowcaseViewModel(Control owner, LuminaShell scopeShell)
    {
        _owner = owner;
        _scopeShell = scopeShell;
    }

    [RelayCommand]
    private void ShowRootOverlayMarker()
    {
        var root = _owner.GetVisualAncestors().OfType<SandboxRootView>().FirstOrDefault();
        root?.NotificationCenter.Show(
            T("Sandbox.Overlays.HostScope.RootMarker.Title"),
            T("Sandbox.Overlays.HostScope.RootMarker.Message"),
            NotificationType.Information,
            TimeSpan.FromSeconds(10));
    }

    [RelayCommand]
    private void OpenShellDrawer()
    {
        _scopeShell.ShowDrawer(new LuminaDrawer
        {
            Placement = DrawerPlacement.Right,
            DrawerLength = 300,
            Content = CreateScopeContent(
                "Sandbox.Overlays.HostScope.ShellDrawer.Title",
                "Sandbox.Overlays.HostScope.ShellDrawer.Message",
                _scopeShell.CloseDrawer)
        });
    }

    [RelayCommand]
    private void OpenTopDrawer()
    {
        ILuminaOverlayHost? host = FindTopOverlayHost();
        if (host == null)
        {
            return;
        }

        host.ShowDrawer(new LuminaDrawer
        {
            Placement = DrawerPlacement.Right,
            DrawerLength = 380,
            Content = CreateScopeContent(
                "Sandbox.Overlays.HostScope.TopDrawer.Title",
                "Sandbox.Overlays.HostScope.TopDrawer.Message",
                host.CloseDrawer)
        });
    }

    [RelayCommand]
    private void OpenShellBottomSheet()
    {
        _scopeShell.ShowBottomSheet(CreateScopeContent(
            "Sandbox.Overlays.HostScope.ShellSheet.Title",
            "Sandbox.Overlays.HostScope.ShellSheet.Message",
            _scopeShell.CloseBottomSheet));
    }

    [RelayCommand]
    private void OpenTopBottomSheet()
    {
        ILuminaOverlayHost? host = FindTopOverlayHost();
        if (host == null)
        {
            return;
        }

        host.ShowBottomSheet(CreateScopeContent(
            "Sandbox.Overlays.HostScope.TopSheet.Title",
            "Sandbox.Overlays.HostScope.TopSheet.Message",
            host.CloseBottomSheet));
    }

    private ILuminaOverlayHost? FindTopOverlayHost()
    {
        return (ILuminaOverlayHost?)LuminaTopView.FindOuterFor(_owner) ?? FindOuterShell();
    }

    private LuminaShell? FindOuterShell()
    {
        return (_owner as LuminaShell) ?? _owner.GetVisualAncestors().OfType<LuminaShell>().LastOrDefault();
    }

    private static Control CreateScopeContent(string titleKey, string messageKey, Action close)
    {
        return new StackPanel
        {
            Spacing = 14,
            MinWidth = 260,
            MaxWidth = 560,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children =
            {
                new StackPanel
                {
                    Spacing = 6,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = T(titleKey),
                            FontSize = 20,
                            FontWeight = FontWeight.SemiBold,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = T(messageKey),
                            Opacity = 0.72,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                },
                new Button
                {
                    Content = T("Sandbox.Text.0600"),
                    Classes = { "Outline" },
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Command = new RelayCommand(close)
                }
            }
        };
    }

    private static string T(string key)
    {
        return LuminaLocalization.Get(key);
    }
}
