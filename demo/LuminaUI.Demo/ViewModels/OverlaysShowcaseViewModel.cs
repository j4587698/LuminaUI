using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;
using LuminaUI.Localization;
using LuminaUI.Demo.Views;

namespace LuminaUI.Demo.ViewModels;

public partial class OverlaysShowcaseViewModel : ObservableObject
{
    private readonly Control _owner;

    public OverlaysShowcaseViewModel(Control owner)
    {
        _owner = owner;
    }

    [RelayCommand]
    private void OpenComponentDialog()
    {
        if (GetShell() is not { } shell)
        {
            return;
        }

        var dialog = new LuminaDialog
        {
            Title = T("Sandbox.Text.0614"),
            Content = new DeleteProjectDialogContent(new DeleteProjectDialogContentViewModel(shell.CloseDialog))
        };
        shell.ShowDialog(dialog);
    }

    [RelayCommand]
    private void OpenInlineDialog(object? parameter)
    {
        if (GetShell() is not { } shell || parameter is not LuminaDialog dialog)
        {
            return;
        }

        dialog.DataContext = this;
        shell.ShowDialog(dialog);
    }

    [RelayCommand]
    private async Task OpenWindowDialog()
    {
        var window = TopLevel.GetTopLevel(_owner) as Window;
        if (window == null) return;
        
        var dialog = new LuminaWindowDialog
        {
            Title = T(SandboxLocalization.OverlaysNativeWindowDialogTitle),
            ConfirmButtonText = T(SandboxLocalization.OverlaysNativeWindowDialogConfirm),
            ConfirmButtonTheme = "Danger",
            CancelButtonText = T("Sandbox.Text.0601"),
            ShowFooter = true,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        var content = new StackPanel { Margin = new Avalonia.Thickness(24) };
        content.Children.Add(new TextBlock 
        { 
            Text = T(SandboxLocalization.OverlaysNativeWindowDialogDescription),
            FontSize = 16
        });
        
        dialog.Content = content;
        
        await dialog.ShowDialog<LuminaDialogResult>(window);
    }

    [RelayCommand]
    private void CloseDialog()
    {
        GetShell()?.CloseDialog();
        GetTopView()?.CloseDialog();
    }

    [RelayCommand]
    private void OpenBottomSheet()
    {
        var shell = GetShell();
        if (shell == null)
        {
            return;
        }

        shell.ShowBottomSheet(new DemoActionSheet(new DemoActionSheetViewModel(shell.CloseBottomSheet)));
    }

    [RelayCommand]
    private void ShowToast(string? tone)
    {
        var toastDefinition = tone switch
        {
            "Success" => (T("Sandbox.Text.0617"), T("Sandbox.Text.0618"), "Success", TimeSpan.FromSeconds(4)),
            "Warning" => (T("Sandbox.Text.0619"), T("Sandbox.Text.0620"), "Warning", TimeSpan.FromSeconds(5)),
            "Danger" => (T("Sandbox.Text.0621"), T("Sandbox.Text.0622"), "Danger", TimeSpan.FromSeconds(6)),
            _ => (T("Sandbox.Text.0615"), T("Sandbox.Text.0616"), "Neutral", TimeSpan.FromSeconds(3))
        };

        ShowToastCore(GetShell(), toastDefinition.Item1, toastDefinition.Item2, toastDefinition.Item3, toastDefinition.Item4);
    }

    private static void ShowToastCore(ILuminaOverlayHost? host, string title, string message, string styleClass, TimeSpan duration)
    {
        void Show()
        {
            var toast = new LuminaToast
            {
                Classes = { styleClass },
                Content = CreateToastContent(title, message)
            };

            host?.ShowToast(toast, duration);
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Show();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Show).GetAwaiter().GetResult();
        }
    }

    private LuminaShell? GetShell()
    {
        return LuminaShell.FindFor(_owner);
    }

    private LuminaTopView? GetTopView()
    {
        return LuminaTopView.FindOuterFor(_owner);
    }

    private static Control CreateToastContent(string title, string message)
    {
        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.72
                }
            }
        };
    }

    private static string T(string key)
    {
        return LuminaLocalization.Get(key);
    }
}
