using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class ShowcaseReferencePage : LuminaPage
{
    public ShowcaseReferencePage()
    {
        DataContext = new ShowcaseReferenceViewModel(new Border(), string.Empty, string.Empty, string.Empty);
        InitializeComponent();
    }

    public ShowcaseReferencePage(Control previewContent, ShowcaseSourceInfo sourceInfo)
    {
        CopyPageMetadata(previewContent);
        DataContext = new ShowcaseReferenceViewModel(
            previewContent,
            ShowcaseSourceProvider.ReadSource(sourceInfo.AxamlPath),
            ShowcaseSourceProvider.ReadSource(sourceInfo.CSharpPath),
            sourceInfo.ViewModelPath == null
                ? string.Empty
                : ShowcaseSourceProvider.ReadSource(sourceInfo.ViewModelPath));

        InitializeComponent();
    }

    private void CopyPageMetadata(Control previewContent)
    {
        CompactPreviewLayout(previewContent);

        if (previewContent is not LuminaPage page)
        {
            return;
        }

        Header = page.Header;
        ShellTitle = page.ShellTitle;
        ShellSubtitle = page.ShellSubtitle;
        ShellActions = page.ShellActions;
        NavigationKey = page.NavigationKey;
        CloseShellMenuOnNavigate = page.CloseShellMenuOnNavigate;
        ShowShellChrome = page.ShowShellChrome;
        ShowShellHeader = page.ShowShellHeader;
    }

    private static void CompactPreviewLayout(Control previewContent)
    {
        if (previewContent is ContentPage contentPage)
        {
            contentPage.Padding = new Thickness(0);
        }

        foreach (var control in EnumerateLogicalControls(previewContent))
        {
            if (control is not ScrollViewer { Name: "PageScroll" } scrollViewer)
            {
                continue;
            }

            scrollViewer.Margin = new Thickness(0);

            if (scrollViewer.Content is not StackPanel stackPanel)
            {
                continue;
            }

            stackPanel.ClearValue(Layoutable.WidthProperty);

            if (stackPanel.Margin.Top >= 24 || stackPanel.Margin.Bottom >= 32)
            {
                stackPanel.Margin = new Thickness(0, 12, 0, 24);
            }
        }
    }

    private static IEnumerable<Control> EnumerateLogicalControls(object root)
    {
        if (root is Control control)
        {
            yield return control;
        }

        if (root is not ILogical logical)
        {
            yield break;
        }

        foreach (var child in logical.LogicalChildren)
        {
            foreach (var descendant in EnumerateLogicalControls(child))
            {
                yield return descendant;
            }
        }
    }
}
