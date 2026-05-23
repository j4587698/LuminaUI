using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Demo.Views;

namespace LuminaUI.Demo.ViewModels;

public partial class DetailContentPageViewModel : ObservableObject
{
    [RelayCommand]
    private void GoBack(object? parameter)
    {
        var owner = parameter switch
        {
            Control control => control,
            RoutedEventArgs { Source: Control source } => source,
            _ => null
        };

        var nav = owner?.GetVisualAncestors().OfType<NavigationPage>().FirstOrDefault();
        if (nav != null)
        {
            nav.Content = new DesignSystemPage();
        }
    }
}
