using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;

namespace LuminaUI.Controls;

public static class LuminaWindowMessageBox
{
    /// <summary>
    /// Shows a standard message box dialog.
    /// </summary>
    /// <param name="owner">The parent window.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The text message to display.</param>
    /// <param name="buttons">The buttons to display in the footer.</param>
    /// <param name="confirmButtonTheme">The CSS class for the primary/confirm button (e.g. "Primary", "Danger").</param>
    /// <returns>A task returning the dialog result.</returns>
    public static Task<LuminaDialogResult> ShowAsync(
        Window owner,
        string title,
        string message,
        LuminaDialogButtons buttons = LuminaDialogButtons.OkCancel,
        LuminaMessageBoxIcon icon = LuminaMessageBoxIcon.None,
        string? confirmButtonTheme = null)
    {
        var dialog = new LuminaWindowDialog
        {
            Title = title,
            Buttons = buttons,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (!string.IsNullOrEmpty(confirmButtonTheme))
        {
            dialog.ConfirmButtonTheme = confirmButtonTheme;
        }

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 420,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontSize = 14
        };
        
        if (icon == LuminaMessageBoxIcon.None)
        {
            textBlock.Margin = new Avalonia.Thickness(24);
            dialog.Content = textBlock;
        }
        else
        {
            var stackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 16,
                Margin = new Avalonia.Thickness(24)
            };
            stackPanel.Children.Add(CreateIconControl(icon));
            stackPanel.Children.Add(textBlock);
            dialog.Content = stackPanel;
        }

        return dialog.ShowDialog<LuminaDialogResult>(owner);
    }

    private static Control CreateIconControl(LuminaMessageBoxIcon icon)
    {
        string data = icon switch
        {
            LuminaMessageBoxIcon.Info => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z",
            LuminaMessageBoxIcon.Warning => "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z",
            LuminaMessageBoxIcon.Error => "M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z",
            LuminaMessageBoxIcon.Question => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm2.07-7.75l-.9.92C13.45 12.9 13 13.5 13 15h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H8c0-2.21 1.79-4 4-4s4 1.79 4 4c0 .88-.36 1.68-.93 2.25z",
            _ => ""
        };

        var pathIcon = new PathIcon
        {
            Data = Geometry.Parse(data),
            Width = 32,
            Height = 32,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        string brushKey = icon switch
        {
            LuminaMessageBoxIcon.Warning => "LuminaWarningBrush",
            LuminaMessageBoxIcon.Error => "LuminaDangerBrush",
            LuminaMessageBoxIcon.Info => "LuminaPrimaryBrush",
            LuminaMessageBoxIcon.Question => "LuminaPrimaryBrush",
            _ => "LuminaTextBrush"
        };
        
        pathIcon.Bind(PathIcon.ForegroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(brushKey));

        return pathIcon;
    }
}
