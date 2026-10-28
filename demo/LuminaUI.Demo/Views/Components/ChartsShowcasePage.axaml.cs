using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LuminaUI.Charts;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;
using LuminaUI.Localization;

namespace LuminaUI.Demo.Views;

public partial class ChartsShowcasePage : LuminaPage
{
    private const int MaxLivePoints = 80;

    private readonly DispatcherTimer _liveTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly Random _random = new(7);
    private readonly ChartsShowcaseViewModel _viewModel;
    private readonly ObservableCollection<LuminaChartPoint> _livePoints;
    private int _liveTick;
    private double _latestLiveValue;
    private bool _perfUpdatePending;
    private bool _perfTextShown;

    public ChartsShowcasePage()
    {
        _viewModel = new ChartsShowcaseViewModel();
        DataContext = _viewModel;
        InitializeComponent();
        _livePoints = (ObservableCollection<LuminaChartPoint>)_viewModel.LiveSeries[0].Points!;

        // Buttons, the chart and the timer all share the page's lifetime, so these handlers stay
        // attached for good. The shell caches pages (and the reference tab host unloads them when
        // switching to the source tab), so Unloaded must not tear them down - only stop the timer.
        LoadStressButton.Click += OnLoadStressClicked;
        ResetStressButton.Click += OnResetStressClicked;
        LiveStartButton.Click += OnLiveStartClicked;
        LiveStopButton.Click += OnLiveStopClicked;
        StressChart.RenderCompleted += OnStressChartRenderCompleted;
        _liveTimer.Tick += OnLiveTimerTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoadStressClicked(object? sender, RoutedEventArgs e)
    {
        StressChart.Series = _viewModel.StressSeries;
    }

    private void OnResetStressClicked(object? sender, RoutedEventArgs e)
    {
        StressChart.Series = _viewModel.AreaSeries;
    }

    private void OnLiveStartClicked(object? sender, RoutedEventArgs e)
    {
        _liveTimer.Start();
    }

    private void OnLiveStopClicked(object? sender, RoutedEventArgs e)
    {
        _liveTimer.Stop();
    }

    private void AppendLivePoint()
    {
        _liveTick++;
        double wave = Math.Sin(_liveTick * 0.35) * 14.0 + Math.Sin(_liveTick * 0.09) * 9.0;
        double y = 50.0 + wave + (_random.NextDouble() - 0.5) * 6.0;
        var point = new LuminaChartPoint(_liveTick, Math.Round(y, 2));
        _livePoints.Add(point);

        while (_livePoints.Count > MaxLivePoints)
        {
            _livePoints.RemoveAt(0);
        }

        LiveChart.XAxis.Minimum = _liveTick - (MaxLivePoints - 1);
        LiveChart.XAxis.Maximum = _liveTick;
        _latestLiveValue = point.Y;
        UpdateLiveTexts();
    }

    /// <summary>
    /// Assigning Text from code replaces the XAML {l10n:Loc} binding, so these labels are
    /// re-formatted here whenever the data or the language changes.
    /// </summary>
    private void UpdateLiveTexts()
    {
        if (_liveTick == 0)
        {
            return;
        }

        LiveStatusText.Text = LuminaLocalization.Format(SandboxLocalization.ChartsLiveStatusFormat, _livePoints.Count, _liveTick);
        LiveLatestText.Text = LuminaLocalization.Format(SandboxLocalization.ChartsLatestFormat, _latestLiveValue);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLiveTexts();
        if (_perfTextShown)
        {
            UpdatePerfText();
        }
    }

    private void OnStressChartRenderCompleted(object? sender, EventArgs e)
    {
        // Coalesce: pointer moves can trigger many render passes per second and there is no
        // point queueing one dispatcher callback for each of them.
        if (_perfUpdatePending)
        {
            return;
        }

        _perfUpdatePending = true;
        Dispatcher.UIThread.Post(UpdatePerfText, DispatcherPriority.Background);
    }

    private void UpdatePerfText()
    {
        _perfUpdatePending = false;
        _perfTextShown = true;
        PerfText.Text = LuminaLocalization.Format(
            SandboxLocalization.ChartsPerfFormat,
            StressChart.LastBuildMilliseconds,
            StressChart.LastRenderMilliseconds,
            StressChart.LastMoveRenderMilliseconds);
    }

    private void OnLiveTimerTick(object? sender, EventArgs e)
    {
        AppendLivePoint();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // LanguageChanged is static, so it is only held while the page is shown; the texts are
        // refreshed on load in case the language changed while the page was cached off-screen.
        LuminaLocalization.LanguageChanged -= OnLanguageChanged;
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
        OnLanguageChanged(this, EventArgs.Empty);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        // A running DispatcherTimer is rooted by the dispatcher and would keep feeding (and
        // holding on to) a page that is no longer visible.
        _liveTimer.Stop();
        LuminaLocalization.LanguageChanged -= OnLanguageChanged;
    }
}