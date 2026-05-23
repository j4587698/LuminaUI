using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class LocalizationResourcesShowcasePage : UserControl
{
    public LocalizationResourcesShowcasePage()
    {
        DataContext = new LocalizationResourcesShowcaseViewModel(CopyToClipboardAsync);
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
