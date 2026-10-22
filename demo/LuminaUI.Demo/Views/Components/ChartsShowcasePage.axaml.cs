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
    private ChartsShowcaseViewModel? _viewModel;
    private ObservableCollection<LuminaChartPoint>? _livePoints;
    private int _liveTick;
    private bool _perfUpdatePending;

    public ChartsShowcasePage()
    {
        DataContext = new ChartsShowcaseViewModel();
        InitializeComponent();
        _viewModel = (ChartsShowcaseViewModel)DataContext;
        _livePoints = (ObservableCollection<LuminaChartPoint>)_viewModel.LiveSeries[0].Points!;

        LoadStressButton.Click += OnLoadStressClicked;
        ResetStressButton.Click += OnResetStressClicked;
        LiveStartButton.Click += OnLiveStartClicked;
        LiveStopButton.Click += OnLiveStopClicked;
        StressChart.RenderCompleted += OnStressChartRenderCompleted;
        _liveTimer.Tick += OnLiveTimerTick;
        Unloaded += OnUnloaded;
    }

    private void OnLoadStressClicked(object? sender, RoutedEventArgs e)
    {
        StressChart.Series = _viewModel!.StressSeries;
    }

    private void OnResetStressClicked(object? sender, RoutedEventArgs e)
    {
        StressChart.Series = _viewModel!.AreaSeries;
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
        if (_livePoints == null)
        {
            return;
        }

        _liveTick++;
        double wave = Math.Sin(_liveTick * 0.35) * 14.0 + Math.Sin(_liveTick * 0.09) * 9.0;
        double y = 50.0 + wave + (_random.NextDouble() - 0.5) * 6.0;
        var point = new LuminaChartPoint(_liveTick, Math.Round(y, 2));
        _livePoints!.Add(point);

        while (_livePoints.Count > MaxLivePoints)
        {
            _livePoints.RemoveAt(0);
        }

        LiveChart.XAxis.Minimum = _liveTick - (MaxLivePoints - 1);
        LiveChart.XAxis.Maximum = _liveTick;

        LiveStatusText.Text = LuminaLocalization.Format(SandboxLocalization.ChartsLiveStatusFormat, _livePoints.Count, _liveTick);
        LiveLatestText.Text = LuminaLocalization.Format(SandboxLocalization.ChartsLatestFormat, point.Y);
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
        if (_viewModel == null)
        {
            return;
        }

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

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _liveTimer.Stop();
        LoadStressButton.Click -= OnLoadStressClicked;
        ResetStressButton.Click -= OnResetStressClicked;
        LiveStartButton.Click -= OnLiveStartClicked;
        LiveStopButton.Click -= OnLiveStopClicked;
        StressChart.RenderCompleted -= OnStressChartRenderCompleted;
        _liveTimer.Tick -= OnLiveTimerTick;
        Unloaded -= OnUnloaded;
        _viewModel = null;
        _livePoints = null;
    }
}