using System.Collections.Generic;
using Avalonia.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuminaUI.Demo.ViewModels;

public partial class TransitioningContentControlShowcaseViewModel : ObservableObject
{
    public IReadOnlyList<PageTransitionChoice> TransitionChoices => PageTransitionChoices.All;

    public IReadOnlyList<TransitionContentPageItem> Pages { get; } =
    [
        new(1, "Page 1", "First content state"),
        new(2, "Page 2", "Second content state"),
        new(3, "Page 3", "Third content state")
    ];

    [ObservableProperty]
    private PageTransitionChoice _selectedTransitionChoice = PageTransitionChoices.Default;

    [ObservableProperty]
    private IPageTransition? _pageTransition = PageTransitionChoices.Default.CreateTransition();

    [ObservableProperty]
    private TransitionContentPageItem _currentPage;

    public TransitioningContentControlShowcaseViewModel()
    {
        _currentPage = Pages[0];
    }

    [RelayCommand]
    private void ShowPage(string? pageNumber)
    {
        if (!int.TryParse(pageNumber, out int number))
        {
            return;
        }

        foreach (TransitionContentPageItem page in Pages)
        {
            if (page.Number == number)
            {
                CurrentPage = page;
                return;
            }
        }
    }

    partial void OnSelectedTransitionChoiceChanged(PageTransitionChoice value)
    {
        PageTransition = value.CreateTransition();
    }
}

public sealed record TransitionContentPageItem(int Number, string Title, string Subtitle);
