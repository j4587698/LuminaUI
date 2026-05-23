using Avalonia.Controls;

namespace LuminaUI.Demo.ViewModels;

public sealed class ShowcaseReferenceViewModel
{
    public ShowcaseReferenceViewModel(Control previewContent, string axamlSource, string csharpSource, string viewModelSource)
    {
        PreviewContent = previewContent;
        AxamlSource = axamlSource;
        CSharpSource = csharpSource;
        ViewModelSource = viewModelSource;
        HasViewModelSource = !string.IsNullOrWhiteSpace(viewModelSource);
    }

    public Control PreviewContent { get; }

    public string AxamlSource { get; }

    public string CSharpSource { get; }

    public string ViewModelSource { get; }

    public bool HasViewModelSource { get; }
}
