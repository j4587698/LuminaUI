using Avalonia.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class DemoActionSheet : UserControl
{
    public DemoActionSheet()
        : this(new DemoActionSheetViewModel())
    {
    }

    public DemoActionSheet(DemoActionSheetViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
