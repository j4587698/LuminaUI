using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

public class LuminaCarouselOptions : AvaloniaObject
{
    private sealed class AutoPlayState(DispatcherTimer timer)
    {
        public DispatcherTimer Timer { get; } = timer;

        public bool IsPointerOver { get; set; }
    }

    private static readonly Dictionary<Carousel, AutoPlayState> AutoPlayStates;

    public static readonly AttachedProperty<bool> IsAutoPlayEnabledProperty;

    public static readonly AttachedProperty<TimeSpan> AutoPlayIntervalProperty;

    public static readonly AttachedProperty<bool> PauseOnPointerOverProperty;

    public static readonly AttachedProperty<bool> ShowNavigationButtonsProperty;

    public static readonly AttachedProperty<bool> ShowIndicatorsProperty;

    static LuminaCarouselOptions()
    {
        AutoPlayStates = new Dictionary<Carousel, AutoPlayState>();
        IsAutoPlayEnabledProperty = AvaloniaProperty.RegisterAttached<LuminaCarouselOptions, Carousel, bool>("IsAutoPlayEnabled", defaultValue: false);
        AutoPlayIntervalProperty = AvaloniaProperty.RegisterAttached<LuminaCarouselOptions, Carousel, TimeSpan>("AutoPlayInterval", TimeSpan.FromSeconds(3));
        PauseOnPointerOverProperty = AvaloniaProperty.RegisterAttached<LuminaCarouselOptions, Carousel, bool>("PauseOnPointerOver", defaultValue: false);
        ShowNavigationButtonsProperty = AvaloniaProperty.RegisterAttached<LuminaCarouselOptions, Carousel, bool>("ShowNavigationButtons", defaultValue: true);
        ShowIndicatorsProperty = AvaloniaProperty.RegisterAttached<LuminaCarouselOptions, Carousel, bool>("ShowIndicators", defaultValue: true);
        IsAutoPlayEnabledProperty.Changed.AddClassHandler((Carousel carousel, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateAutoPlay(carousel);
        });
        AutoPlayIntervalProperty.Changed.AddClassHandler((Carousel carousel, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateAutoPlayInterval(carousel);
        });
        PauseOnPointerOverProperty.Changed.AddClassHandler((Carousel carousel, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateAutoPlay(carousel);
        });
        ShowNavigationButtonsProperty.Changed.AddClassHandler((Carousel carousel, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateVisibilityClasses(carousel);
        });
        ShowIndicatorsProperty.Changed.AddClassHandler((Carousel carousel, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateVisibilityClasses(carousel);
        });
    }

    public static bool GetIsAutoPlayEnabled(Carousel element)
    {
        return element.GetValue(IsAutoPlayEnabledProperty);
    }

    public static void SetIsAutoPlayEnabled(Carousel element, bool value)
    {
        element.SetValue(IsAutoPlayEnabledProperty, value);
    }

    public static TimeSpan GetAutoPlayInterval(Carousel element)
    {
        return element.GetValue(AutoPlayIntervalProperty);
    }

    public static void SetAutoPlayInterval(Carousel element, TimeSpan value)
    {
        element.SetValue(AutoPlayIntervalProperty, value);
    }

    public static bool GetPauseOnPointerOver(Carousel element)
    {
        return element.GetValue(PauseOnPointerOverProperty);
    }

    public static void SetPauseOnPointerOver(Carousel element, bool value)
    {
        element.SetValue(PauseOnPointerOverProperty, value);
    }

    public static bool GetShowNavigationButtons(Carousel element)
    {
        return element.GetValue(ShowNavigationButtonsProperty);
    }

    public static void SetShowNavigationButtons(Carousel element, bool value)
    {
        element.SetValue(ShowNavigationButtonsProperty, value);
    }

    public static bool GetShowIndicators(Carousel element)
    {
        return element.GetValue(ShowIndicatorsProperty);
    }

    public static void SetShowIndicators(Carousel element, bool value)
    {
        element.SetValue(ShowIndicatorsProperty, value);
    }

    private static void UpdateAutoPlay(Carousel carousel)
    {
        if (GetIsAutoPlayEnabled(carousel))
        {
            AttachAutoPlay(carousel);
        }
        else
        {
            DetachAutoPlay(carousel);
        }
    }

    private static void UpdateVisibilityClasses(Carousel carousel)
    {
        carousel.Classes.Set("HideNavigationButtons", !GetShowNavigationButtons(carousel));
        carousel.Classes.Set("HideIndicators", !GetShowIndicators(carousel));
    }

    private static void AttachAutoPlay(Carousel carousel)
    {
        if (AutoPlayStates.ContainsKey(carousel))
        {
            UpdateAutoPlayInterval(carousel);
            return;
        }
        AutoPlayState state = new AutoPlayState(new DispatcherTimer());
        state.Timer.Tick += (_, _) => {
            Advance(carousel, state);
        };
        AutoPlayStates[carousel] = state;
        carousel.AttachedToVisualTree += OnCarouselAttachedToVisualTree;
        carousel.DetachedFromVisualTree += OnCarouselDetachedFromVisualTree;
        carousel.PointerEntered += OnCarouselPointerEntered;
        carousel.PointerExited += OnCarouselPointerExited;
        UpdateAutoPlayInterval(carousel);
        state.Timer.Start();
    }

    private static void DetachAutoPlay(Carousel carousel)
    {
        if (AutoPlayStates.Remove(carousel, out AutoPlayState? state))
        {
            carousel.AttachedToVisualTree -= OnCarouselAttachedToVisualTree;
            carousel.DetachedFromVisualTree -= OnCarouselDetachedFromVisualTree;
            carousel.PointerEntered -= OnCarouselPointerEntered;
            carousel.PointerExited -= OnCarouselPointerExited;
            state.Timer.Stop();
        }
    }

    private static void UpdateAutoPlayInterval(Carousel carousel)
    {
        if (AutoPlayStates.TryGetValue(carousel, out AutoPlayState? state))
        {
            state.Timer.Interval = NormalizeInterval(GetAutoPlayInterval(carousel));
        }
    }

    private static void Advance(Carousel carousel, AutoPlayState state)
    {
        if (GetIsAutoPlayEnabled(carousel) && carousel.IsAttachedToVisualTree() && carousel.IsEffectivelyEnabled && carousel.IsVisible && carousel.ItemCount > 1 && !carousel.IsSwiping && (!state.IsPointerOver || !GetPauseOnPointerOver(carousel)))
        {
            if (carousel.SelectedIndex >= carousel.ItemCount - 1)
            {
                carousel.SelectedIndex = 0;
            }
            else
            {
                carousel.Next();
            }
        }
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval)
    {
        return (interval < TimeSpan.FromMilliseconds(500)) ? TimeSpan.FromMilliseconds(500) : interval;
    }

    private static void OnCarouselAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Carousel carousel && AutoPlayStates.TryGetValue(carousel, out AutoPlayState? state))
        {
            state.Timer.Start();
        }
    }

    private static void OnCarouselDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Carousel carousel && AutoPlayStates.TryGetValue(carousel, out AutoPlayState? state))
        {
            state.Timer.Stop();
            state.IsPointerOver = false;
        }
    }

    private static void OnCarouselPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Carousel carousel && AutoPlayStates.TryGetValue(carousel, out AutoPlayState? state))
        {
            state.IsPointerOver = true;
        }
    }

    private static void OnCarouselPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Carousel carousel && AutoPlayStates.TryGetValue(carousel, out AutoPlayState? state))
        {
            state.IsPointerOver = false;
        }
    }
}
