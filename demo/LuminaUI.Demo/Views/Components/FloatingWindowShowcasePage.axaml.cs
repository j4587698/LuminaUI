using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class FloatingWindowShowcasePage : LuminaPage
{
    private DemoFloatingWindow? _demoWindow;

    private readonly DispatcherTimer _karaokeTimer = new();
    private readonly List<LuminaKaraokeWord> _karaokeWords = [];
    private double _karaokePosition;
    private bool _karaokePlaying;
    private const double KaraokeDuration = 4.0;

    public IReadOnlyList<LuminaKaraokeWord> KaraokeWords => _karaokeWords;

    public double KaraokePosition
    {
        get => _karaokePosition;
        set
        {
            _karaokePosition = value;
            if (KaraokeDemo != null)
                KaraokeDemo.PlaybackPosition = value;
        }
    }

    public FloatingWindowShowcasePage()
    {
        DataContext = new StaticShowcaseViewModel();
        InitializeComponent();

        _karaokeWords.Add(new LuminaKaraokeWord("欢", 0, 0.5));
        _karaokeWords.Add(new LuminaKaraokeWord("迎", 0.5, 0.5));
        _karaokeWords.Add(new LuminaKaraokeWord("使", 1.0, 0.5));
        _karaokeWords.Add(new LuminaKaraokeWord("用", 1.5, 0.5));
        _karaokeWords.Add(new LuminaKaraokeWord("Lu", 2.0, 0.4));
        _karaokeWords.Add(new LuminaKaraokeWord("mi", 2.4, 0.4));
        _karaokeWords.Add(new LuminaKaraokeWord("na", 2.8, 0.4));
        _karaokeWords.Add(new LuminaKaraokeWord("UI", 3.2, 0.8));

        _karaokeTimer.Interval = TimeSpan.FromMilliseconds(33);
        _karaokeTimer.Tick += OnKaraokeTick;
    }

    ~FloatingWindowShowcasePage()
    {
        _karaokeTimer.Stop();
    }

    private void OnKaraokeTick(object? sender, EventArgs e)
    {
        var newPos = _karaokePosition + 0.033;
        if (newPos >= KaraokeDuration)
        {
            newPos = 0;
        }

        KaraokePosition = newPos;
    }

    private void OnKaraokeToggle(object? sender, RoutedEventArgs e)
    {
        _karaokePlaying = !_karaokePlaying;

        if (_karaokePlaying)
        {
            _karaokeTimer.Start();
            KaraokePlayIcon.Kind = Material.Icons.MaterialIconKind.Pause;
        }
        else
        {
            _karaokeTimer.Stop();
            KaraokePlayIcon.Kind = Material.Icons.MaterialIconKind.Play;
        }
    }

    private void OnKaraokeReset(object? sender, RoutedEventArgs e)
    {
        _karaokePlaying = false;
        _karaokeTimer.Stop();
        KaraokePosition = 0;
        KaraokePlayIcon.Kind = Material.Icons.MaterialIconKind.Play;
    }

    private void OnOpenDemo(object? sender, RoutedEventArgs e)
    {
        if (_demoWindow is { IsVisible: true })
        {
            _demoWindow.Activate();
            return;
        }

        _demoWindow = new DemoFloatingWindow();
        _demoWindow.Show();
    }
}
