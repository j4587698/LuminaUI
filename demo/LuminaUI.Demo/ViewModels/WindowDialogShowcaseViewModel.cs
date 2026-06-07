using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class WindowDialogShowcaseViewModel : ObservableObject
{
    private readonly Control _owner;

    [ObservableProperty]
    private string _messageBoxResultText = "Result: None";

    [ObservableProperty]
    private string _customResultText = "Result: None";

    public WindowDialogShowcaseViewModel(Control owner)
    {
        _owner = owner;
    }

    private Window? GetOwnerWindow() => TopLevel.GetTopLevel(_owner) as Window;

    [RelayCommand]
    private async Task ShowOkCancel()
    {
        if (GetOwnerWindow() is not { } window) return;

        var result = await LuminaWindowMessageBox.ShowAsync(
            window,
            T("Sandbox.Text.1341"),
            T("Sandbox.Text.1342"),
            LuminaDialogButtons.OkCancel,
            LuminaMessageBoxIcon.Question);

        MessageBoxResultText = $"{T("Sandbox.Text.1343")}: {result}";
    }

    [RelayCommand]
    private async Task ShowYesNo()
    {
        if (GetOwnerWindow() is not { } window) return;

        var result = await LuminaWindowMessageBox.ShowAsync(
            window,
            T("Sandbox.Text.1344"),
            T("Sandbox.Text.1345"),
            LuminaDialogButtons.YesNo,
            LuminaMessageBoxIcon.Info);

        MessageBoxResultText = $"{T("Sandbox.Text.1343")}: {result}";
    }

    [RelayCommand]
    private async Task ShowYesNoCancel()
    {
        if (GetOwnerWindow() is not { } window) return;

        var result = await LuminaWindowMessageBox.ShowAsync(
            window,
            T("Sandbox.Text.1341"),
            T("Sandbox.Text.1342"),
            LuminaDialogButtons.YesNoCancel,
            LuminaMessageBoxIcon.Warning);

        MessageBoxResultText = $"{T("Sandbox.Text.1343")}: {result}";
    }

    [RelayCommand]
    private async Task ShowDanger()
    {
        if (GetOwnerWindow() is not { } window) return;

        var result = await LuminaWindowMessageBox.ShowAsync(
            window,
            T("Sandbox.Text.1346"),
            T("Sandbox.Text.1347"),
            LuminaDialogButtons.OkCancel,
            LuminaMessageBoxIcon.Error,
            "Danger"); // Makes the OK button red

        MessageBoxResultText = $"{T("Sandbox.Text.1343")}: {result}";
    }

    [RelayCommand]
    private async Task ShowSelectDialog()
    {
        if (GetOwnerWindow() is not { } window) return;

        var dialog = new LuminaWindowDialog
        {
            Title = T("Sandbox.Text.1348"),
            Buttons = LuminaDialogButtons.OkCancel,
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stack = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 12, MinWidth = 300 };
        stack.Children.Add(new Avalonia.Controls.TextBlock { Text = T("Sandbox.Text.1349") });
        
        var comboBox = new Avalonia.Controls.ComboBox { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        comboBox.Items.Add(T(SandboxLocalization.WindowDialogsEnvironmentDevelopment));
        comboBox.Items.Add(T(SandboxLocalization.WindowDialogsEnvironmentStaging));
        comboBox.Items.Add(T(SandboxLocalization.WindowDialogsEnvironmentProduction));
        comboBox.SelectedIndex = 0;
        
        stack.Children.Add(comboBox);
        dialog.Content = stack;

        var result = await dialog.ShowDialog<LuminaDialogResult>(window);
        CustomResultText = result == LuminaDialogResult.Ok 
            ? $"{T("Sandbox.Text.1350")}: {comboBox.SelectedItem}" 
            : T("Sandbox.Text.1351");
    }

    [RelayCommand]
    private async Task ShowFormDialog()
    {
        if (GetOwnerWindow() is not { } window) return;

        var dialog = new LuminaWindowDialog
        {
            Title = T("Sandbox.Text.1352"),
            Buttons = LuminaDialogButtons.OkCancel,
            ConfirmButtonText = T("Sandbox.Text.1353"),
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stack = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 12, MinWidth = 350 };
        stack.Children.Add(new Avalonia.Controls.TextBlock { Text = T("Sandbox.Text.1354") });
        stack.Children.Add(new Avalonia.Controls.TextBox { PlaceholderText = T("Sandbox.Text.1355") });
        stack.Children.Add(new Avalonia.Controls.TextBlock { Text = T("Sandbox.Text.1356") });
        stack.Children.Add(new Avalonia.Controls.TextBox { PlaceholderText = T("Sandbox.Text.1357") });

        dialog.Content = stack;

        var result = await dialog.ShowDialog<LuminaDialogResult>(window);
        CustomResultText = $"{T("Sandbox.Text.1358")}: {result}";
    }

    [RelayCommand]
    private async Task ShowCustomFooter()
    {
        if (GetOwnerWindow() is not { } window) return;

        var dialog = new LuminaWindowDialog
        {
            Title = T("Sandbox.Text.1339"),
            Buttons = LuminaDialogButtons.Custom, // Disable auto-generated footer
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var content = new Avalonia.Controls.TextBlock 
        { 
            Text = T("Sandbox.Text.1359"),
            Margin = new Avalonia.Thickness(24),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        };
        dialog.Content = content;

        // Create Custom Footer
        var footerBorder = new Avalonia.Controls.Border 
        { 
            Padding = new Avalonia.Thickness(16), 
            Background = Avalonia.Media.Brushes.Transparent
        };
        
        var customButton = new Avalonia.Controls.Button 
        { 
            Content = T("Sandbox.Text.1360"), 
            Classes = { "Primary", "Large" },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        customButton.Click += (_, _) => dialog.Close(LuminaDialogResult.Ok);
        
        footerBorder.Child = customButton;
        dialog.Footer = footerBorder;

        await dialog.ShowDialog<LuminaDialogResult>(window);
    }

    [RelayCommand]
    private async Task ShowNoFooter()
    {
        if (GetOwnerWindow() is not { } window) return;

        var dialog = new LuminaWindowDialog
        {
            Title = T("Sandbox.Text.1361"),
            ShowFooter = false, // Completely hide the footer area
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stack = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 20, MinWidth = 300 };
        stack.Children.Add(new Avalonia.Controls.TextBlock 
        { 
            Text = T("Sandbox.Text.1362"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        
        var btn = new Avalonia.Controls.Button { Content = T("Sandbox.Text.1363"), Classes = { "Danger" } };
        btn.Click += (_, _) => dialog.Close(LuminaDialogResult.Cancel);
        stack.Children.Add(btn);

        dialog.Content = stack;

        await dialog.ShowDialog<LuminaDialogResult>(window);
    }

    private static string T(string key)
    {
        return LuminaLocalization.Get(key);
    }
}
