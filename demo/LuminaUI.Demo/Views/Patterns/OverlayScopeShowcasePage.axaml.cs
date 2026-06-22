using Avalonia.Controls;
using Avalonia.Threading;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class OverlayScopeShowcasePage : LuminaPage
{
    public OverlayScopeShowcasePage()
    {
        InitializeComponent();
        DataContext = new OverlayScopeShowcaseViewModel(this, ScopeShell);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => PageScroll.Offset = default, DispatcherPriority.Loaded);
    }
}
