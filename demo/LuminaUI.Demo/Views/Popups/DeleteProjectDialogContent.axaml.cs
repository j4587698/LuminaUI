using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DeleteProjectDialogContent : UserControl
{
    public DeleteProjectDialogContent()
        : this(new DeleteProjectDialogContentViewModel())
    {
    }

    public DeleteProjectDialogContent(DeleteProjectDialogContentViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
