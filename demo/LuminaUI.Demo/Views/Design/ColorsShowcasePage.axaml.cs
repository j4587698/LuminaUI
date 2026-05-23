using Avalonia.Controls;
using Avalonia.Input.Platform;
using LuminaUI.Demo.ViewModels;
using System.Threading.Tasks;

namespace LuminaUI.Demo.Views;

public partial class ColorsShowcasePage : UserControl
{
    public ColorsShowcasePage()
    {
        DataContext = new ColorsShowcaseViewModel(CopyToClipboardAsync);
        InitializeComponent();
    }

    private async Task CopyToClipboardAsync(string text)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
