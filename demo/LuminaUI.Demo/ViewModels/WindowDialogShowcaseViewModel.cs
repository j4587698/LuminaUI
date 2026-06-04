using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;

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
            SandboxTextLocalizer.Localize("Save Changes"),
            SandboxTextLocalizer.Localize("Do you want to save the changes before closing this document?"),
            LuminaDialogButtons.OkCancel,
            LuminaMessageBoxIcon.Question);

        MessageBoxResultText = $"{SandboxTextLocalizer.Localize("Result")}: {result}";
    }

    [RelayCommand]
    private async Task ShowYesNo()
    {
        if (GetOwnerWindow() is not { } window) return;

        var result = await LuminaWindowMessageBox.ShowAsync(
            window,
            SandboxTextLocalizer.Localize("Network Request"),
            SandboxTextLocalizer.Localize("This action requires connecting to the internet. Continue?"),
            LuminaDialogButtons.YesNo,
            LuminaMessageBoxIcon.Info);

        MessageBoxResultText = $"{SandboxTextLocalizer.Localize("Result")}: {result}";
    }

    [RelayCommand]
    private async Task ShowYesNoCancel()
    {
        if (GetOwnerWindow() is not { } window) return;

        var result = await LuminaWindowMessageBox.ShowAsync(
            window,
            SandboxTextLocalizer.Localize("Save Changes"),
            SandboxTextLocalizer.Localize("Do you want to save the changes before closing this document?"),
            LuminaDialogButtons.YesNoCancel,
            LuminaMessageBoxIcon.Warning);

        MessageBoxResultText = $"{SandboxTextLocalizer.Localize("Result")}: {result}";
    }

    [RelayCommand]
    private async Task ShowDanger()
    {
        if (GetOwnerWindow() is not { } window) return;

        var result = await LuminaWindowMessageBox.ShowAsync(
            window,
            SandboxTextLocalizer.Localize("Delete Repository"),
            SandboxTextLocalizer.Localize("Are you sure you want to permanently delete this repository? This action cannot be undone."),
            LuminaDialogButtons.OkCancel,
            LuminaMessageBoxIcon.Error,
            "Danger"); // Makes the OK button red

        MessageBoxResultText = $"{SandboxTextLocalizer.Localize("Result")}: {result}";
    }

    [RelayCommand]
    private async Task ShowSelectDialog()
    {
        if (GetOwnerWindow() is not { } window) return;

        var dialog = new LuminaWindowDialog
        {
            Title = SandboxTextLocalizer.Localize("Select Option"),
            Buttons = LuminaDialogButtons.OkCancel,
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stack = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 12, MinWidth = 300 };
        stack.Children.Add(new Avalonia.Controls.TextBlock { Text = SandboxTextLocalizer.Localize("Please select an environment to deploy to:") });
        
        var comboBox = new Avalonia.Controls.ComboBox { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        comboBox.Items.Add("Development");
        comboBox.Items.Add("Staging");
        comboBox.Items.Add("Production");
        comboBox.SelectedIndex = 0;
        
        stack.Children.Add(comboBox);
        dialog.Content = stack;

        var result = await dialog.ShowDialog<LuminaDialogResult>(window);
        CustomResultText = result == LuminaDialogResult.Ok 
            ? $"{SandboxTextLocalizer.Localize("Selected")}: {comboBox.SelectedItem}" 
            : SandboxTextLocalizer.Localize("Deployment Cancelled");
    }

    [RelayCommand]
    private async Task ShowFormDialog()
    {
        if (GetOwnerWindow() is not { } window) return;

        var dialog = new LuminaWindowDialog
        {
            Title = SandboxTextLocalizer.Localize("New User Profile"),
            Buttons = LuminaDialogButtons.OkCancel,
            ConfirmButtonText = SandboxTextLocalizer.Localize("Create"),
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stack = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 12, MinWidth = 350 };
        stack.Children.Add(new Avalonia.Controls.TextBlock { Text = SandboxTextLocalizer.Localize("Username") });
        stack.Children.Add(new Avalonia.Controls.TextBox { PlaceholderText = SandboxTextLocalizer.Localize("Enter username") });
        stack.Children.Add(new Avalonia.Controls.TextBlock { Text = SandboxTextLocalizer.Localize("Email Address") });
        stack.Children.Add(new Avalonia.Controls.TextBox { PlaceholderText = SandboxTextLocalizer.Localize("Enter email") });

        dialog.Content = stack;

        var result = await dialog.ShowDialog<LuminaDialogResult>(window);
        CustomResultText = $"{SandboxTextLocalizer.Localize("Form Result")}: {result}";
    }

    [RelayCommand]
    private async Task ShowCustomFooter()
    {
        if (GetOwnerWindow() is not { } window) return;

        var dialog = new LuminaWindowDialog
        {
            Title = SandboxTextLocalizer.Localize("Custom Footer Template"),
            Buttons = LuminaDialogButtons.Custom, // Disable auto-generated footer
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var content = new Avalonia.Controls.TextBlock 
        { 
            Text = SandboxTextLocalizer.Localize("This dialog uses a completely custom footer element provided via the Footer property."),
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
            Content = SandboxTextLocalizer.Localize("I Understand"), 
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
            Title = SandboxTextLocalizer.Localize("No Footer"),
            ShowFooter = false, // Completely hide the footer area
            SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stack = new Avalonia.Controls.StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 20, MinWidth = 300 };
        stack.Children.Add(new Avalonia.Controls.TextBlock 
        { 
            Text = SandboxTextLocalizer.Localize("This dialog has ShowFooter=False. You can build your own buttons inside the content area."),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        
        var btn = new Avalonia.Controls.Button { Content = SandboxTextLocalizer.Localize("Close Window"), Classes = { "Danger" } };
        btn.Click += (_, _) => dialog.Close(LuminaDialogResult.Cancel);
        stack.Children.Add(btn);

        dialog.Content = stack;

        await dialog.ShowDialog<LuminaDialogResult>(window);
    }
}
