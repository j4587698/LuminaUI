using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class NavigationPageShowcaseViewModel : ObservableObject
{
    private int _nextPageNumber = 2;

    public IReadOnlyList<PageTransitionChoice> TransitionChoices => PageTransitionChoices.All;

    [ObservableProperty]
    private PageTransitionChoice _selectedTransitionChoice = PageTransitionChoices.Default;

    [ObservableProperty]
    private IPageTransition? _pageTransition = PageTransitionChoices.Default.CreateTransition();

    [RelayCommand]
    private async Task PushNavigationAsync(NavigationPage? navigationPage)
    {
        if (navigationPage == null)
        {
            return;
        }

        int pageNumber = _nextPageNumber;
        _nextPageNumber = _nextPageNumber == 2 ? 3 : 2;
        await navigationPage.PushAsync(CreateNavigationPage(pageNumber, navigationPage));
    }

    [RelayCommand]
    private async Task PopNavigationAsync(NavigationPage? navigationPage)
    {
        if (navigationPage != null)
        {
            await navigationPage.PopAsync();
        }
    }

    partial void OnSelectedTransitionChoiceChanged(PageTransitionChoice value)
    {
        PageTransition = value.CreateTransition();
    }

    private ContentPage CreateNavigationPage(int pageNumber, NavigationPage navigationPage)
    {
        var pushButton = new Button
        {
            Name = "PushGeneratedNavigationButton",
            Classes = { "Primary", "Small" },
            Content = pageNumber == 2 ? "推入页面 3" : "推入页面 2",
            HorizontalAlignment = HorizontalAlignment.Left,
            Command = PushNavigationCommand,
            CommandParameter = navigationPage
        };

        var popButton = new Button
        {
            Name = "PopGeneratedNavigationButton",
            Classes = { "Outline", "Small" },
            Content = "弹出页面",
            HorizontalAlignment = HorizontalAlignment.Left,
            Command = PopNavigationCommand,
            CommandParameter = navigationPage
        };

        return new ContentPage
        {
            Header = $"页面 {pageNumber}",
            Content = CreatePageContent(
                $"页面 {pageNumber}",
                "此页面已被推入 NavigationPage 的堆栈中。",
                $"堆栈层级: {pageNumber}",
                pushButton,
                popButton)
        };
    }

    private static Control CreatePageContent(string title, string subtitle, string meta, params Control[] actions)
    {
        var actionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { }
        };

        foreach (Control action in actions)
        {
            actionPanel.Children.Add(action);
        }

        var contentPanel = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 34,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = subtitle,
                    Classes = { "Helper" },
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 360,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new Border
                {
                    Padding = new Thickness(10, 5),
                    CornerRadius = new CornerRadius(999),
                    Background = Brush.Parse("#14000000"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = meta,
                        FontSize = 12
                    }
                }
            }
        };

        Grid.SetRow(actionPanel, 1);

        return new Grid
        {
            Margin = new Thickness(16, 68, 16, 16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            Children =
            {
                contentPanel,
                actionPanel
            }
        };
    }
}
