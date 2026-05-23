using System;
using System.Collections.Generic;
using Avalonia.Animation;

namespace LuminaUI.Demo.ViewModels;

public sealed record PageTransitionChoice(string Name, Func<IPageTransition?> Create)
{
    public IPageTransition? CreateTransition()
    {
        return Create();
    }

    public override string ToString()
    {
        return Name;
    }
}

public static class PageTransitionChoices
{
    public static IReadOnlyList<PageTransitionChoice> All { get; } =
    [
        new("None", () => null),
        new("Fade", () => new CrossFade(TimeSpan.FromMilliseconds(220))),
        new("Slide horizontal", () => new PageSlide(TimeSpan.FromMilliseconds(240), PageSlide.SlideAxis.Horizontal)),
        new("Slide vertical", () => new PageSlide(TimeSpan.FromMilliseconds(240), PageSlide.SlideAxis.Vertical)),
        new("Slide + fade", CreateSlideFade),
        new("3D flip", () => new Rotate3DTransition(TimeSpan.FromMilliseconds(360), PageSlide.SlideAxis.Horizontal, null))
    ];

    public static PageTransitionChoice Default => All[2];

    private static IPageTransition CreateSlideFade()
    {
        var transition = new CompositePageTransition();
        transition.PageTransitions.Add(new PageSlide(TimeSpan.FromMilliseconds(240), PageSlide.SlideAxis.Horizontal));
        transition.PageTransitions.Add(new CrossFade(TimeSpan.FromMilliseconds(240)));
        return transition;
    }
}
