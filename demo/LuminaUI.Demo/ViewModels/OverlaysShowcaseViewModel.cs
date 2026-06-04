using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;
using LuminaUI.Services;
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

        var content = new DeleteProjectDialogContent(new DeleteProjectDialogContentViewModel(shell.CloseDialog));
        SandboxTextLocalizer.Apply(content);

        var dialog = new LuminaDialog
        {
            Title = SandboxTextLocalizer.Localize("Component dialog"),
            Content = content
        };
        SandboxTextLocalizer.Apply(dialog);
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
        SandboxTextLocalizer.Apply(dialog);
        shell.ShowDialog(dialog);
    }

    [RelayCommand]
    private void OpenTopDialog()
    {
        if (GetTopView() is not { } topView)
        {
            return;
        }

        var content = new DeleteProjectDialogContent(new DeleteProjectDialogContentViewModel(topView.CloseDialog));
        SandboxTextLocalizer.Apply(content);

        var dialog = new LuminaDialog
        {
            Title = SandboxTextLocalizer.Localize("Top view dialog"),
            Content = content
        };
        SandboxTextLocalizer.Apply(dialog);
        SandboxTextLocalizer.Apply(dialog);
        topView.ShowDialog(dialog);
    }

    [RelayCommand]
    private async Task OpenWindowDialog()
    {
        var window = TopLevel.GetTopLevel(_owner) as Window;
        if (window == null) return;
        
        var dialog = new LuminaWindowDialog
        {
            Title = "PC Native Window Dialog",
            ConfirmButtonText = "Confirm Action",
            ConfirmButtonTheme = "Danger",
            CancelButtonText = "Cancel",
            ShowFooter = true,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        var content = new StackPanel { Margin = new Avalonia.Thickness(24) };
        content.Children.Add(new TextBlock 
        { 
            Text = "This is a real OS window dialog, you can drag it out of the main window.",
            FontSize = 16
        });
        
        dialog.Content = content;
        
        await dialog.ShowDialog<bool>(window);
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
        var sheet = new DemoActionSheet(new DemoActionSheetViewModel(
            shell == null ? null : () => shell.CloseBottomSheet()));
        SandboxTextLocalizer.Apply(sheet);
        LuminaBottomSheetService.Instance.Show(_owner, sheet);
    }

    [RelayCommand]
    private void OpenTopBottomSheet()
    {
        var topView = GetTopView();
        var sheet = new DemoActionSheet(new DemoActionSheetViewModel(
            topView == null ? null : () => topView.CloseBottomSheet()));
        SandboxTextLocalizer.Apply(sheet);
        LuminaBottomSheetService.Instance.ShowAtTop(_owner, sheet);
    }

    [RelayCommand]
    private void ShowToast(string? tone)
    {
        var toastDefinition = tone switch
        {
            "Success" => ("Published", "The component package is ready.", "Success", TimeSpan.FromSeconds(4), false),
            "Warning" => ("Review needed", "Two tokens use fallback colors.", "Warning", TimeSpan.FromSeconds(5), false),
            "Danger" => ("Build failed", "Resolve the missing resource key.", "Danger", TimeSpan.FromSeconds(6), false),
            "Top" => ("Top view", "This toast is hosted by LuminaTopView.", "Neutral", TimeSpan.FromSeconds(3), true),
            _ => ("Saved", "The local draft was updated.", "Neutral", TimeSpan.FromSeconds(3), false)
        };

        ILuminaOverlayHost? host = toastDefinition.Item5
            ? GetTopView()
            : GetShell();

        if (host == null)
        {
            return;
        }

        void Show()
        {
            var toast = new LuminaToast
            {
                Classes = { toastDefinition.Item3 },
                Content = CreateToastContent(
                    SandboxTextLocalizer.Localize(toastDefinition.Item1),
                    SandboxTextLocalizer.Localize(toastDefinition.Item2))
            };

            SandboxTextLocalizer.Apply(toast);
            host.ShowToast(toast, toastDefinition.Item4);
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
}
