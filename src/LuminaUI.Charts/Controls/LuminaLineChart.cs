using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace LuminaUI.Charts;

/// <summary>
/// A lookless, custom-drawn line chart that renders one or more <see cref="LuminaChartSeries"/>
/// with grid lines, axis labels, optional area fills, and a hover crosshair with a tooltip.
/// </summary>
/// <remarks>
/// Rendering is layered for performance: all data-derived state (axis scale, geometry, axis
/// labels, series brushes) is built once and cached, and only rebuilt when data or a content
/// property changes. Pointer movement repaints only the thin overlay (crosshair, markers,
/// tooltip). Large series are decimated into per-pixel-column min/max segments so per-frame work
/// stays bounded by the plot width. Data changes (<see cref="Series"/> or reactive point
/// collections) can also be animated as a line-to-line morph via <see cref="TransitionDuration"/>,
/// which makes live/streaming charts render smoothly.
/// </remarks>
public class LuminaLineChart : Control
{
    private const double PlotTop = 8.0;
    private const double PlotRight = 14.0;
    private const double PlotLeftPad = 10.0;
    private const double PlotBottomPad = 4.0;
    private const double TooltipCornerRadius = 6.0;
    private const double TooltipPadding = 8.0;
    private const int SubsetMarkersLimit = 600;
    private const double LegendBandHeight = ChartDrawing.LegendBandHeight;
    private const double LegendBandGap = ChartDrawing.LegendBandGap;
    private static readonly DashStyle CrosshairDashStyle = new([4.0, 3.0], 0.0);

    private readonly List<RenderSeries> _renderSeries = new();
    private readonly List<double> _xTickValues = new();
    private readonly List<double> _yTickValues = new();
    private readonly List<FormattedText> _xTickLabels = new();
    private readonly List<FormattedText> _yTickLabels = new();
    private readonly List<OverlayMatch> _overlayMatches = new();
    private readonly List<BarHighlight> _barHighlights = new();
    private readonly List<OverlayMatch> _scatterHighlights = new();
    private readonly List<(FormattedText Text, IBrush Color)> _cachedLegendItems = new();
    private double _cachedLegendContentWidth;
    private readonly Stopwatch _buildStopwatch = new();
    private readonly Stopwatch _renderStopwatch = new();

    /// <summary>
    /// Occurs when a render pass completes. Useful for diagnostics and benchmarking.
    /// Raised on the UI thread inside the render pass, so handlers must stay cheap and must not
    /// trigger synchronous repaints.
    /// </summary>
    public event EventHandler? RenderCompleted;

    private DrawingGroup? _cachedStaticDrawing;
    private Pen? _cachedCrosshairPen;
    private readonly List<FormattedText> _cachedTooltipLines = new();
    private readonly List<(LuminaChartSeries Series, double X, double Y)> _cachedTooltipMatches = new();
    private Size _cachedTooltipSize;

    private LuminaChartAxis _xAxis = new();
    private LuminaChartAxis _yAxis = new();

    private INotifyCollectionChanged? _seriesSubscription;
    private HashSet<INotifyCollectionChanged> _pointsSubscriptions = new();
    private HashSet<LuminaChartSeries> _seriesItemSubscriptions = new();
    private DispatcherTimer? _transitionTimer;
    private readonly Stopwatch _transitionSw = new();
    private List<(Point[]? Line, Point[]? Area, Point[]? Raw)>? _transitionPrev;

    private Typeface _labelTypeface;
    private AxisScale _xScale;
    private AxisScale _yScale;
    private Rect _plotRect;
    private Point? _hoverPosition;
    private bool _isDirty = true;
    private Size _lastLayoutSize;
    private bool _rebuildDueToDataChange;
    private bool _inTransition;
    private double _transitionT;
    private double _lastBuildMilliseconds;
    private double _lastMoveRenderMilliseconds;
    private double _lastRenderMilliseconds;

    public static readonly StyledProperty<IList<LuminaChartSeries>?> SeriesProperty =
        AvaloniaProperty.Register<LuminaLineChart, IList<LuminaChartSeries>?>(nameof(Series));

    public static readonly StyledProperty<bool> ShowGridLinesProperty =
        AvaloniaProperty.Register<LuminaLineChart, bool>(nameof(ShowGridLines), true);

    public static readonly StyledProperty<bool> ShowAxisLabelsProperty =
        AvaloniaProperty.Register<LuminaLineChart, bool>(nameof(ShowAxisLabels), true);

    public static readonly StyledProperty<int> HorizontalGridLineCountProperty =
        AvaloniaProperty.Register<LuminaLineChart, int>(nameof(HorizontalGridLineCount), 5);

    public static readonly StyledProperty<int> VerticalGridLineCountProperty =
        AvaloniaProperty.Register<LuminaLineChart, int>(nameof(VerticalGridLineCount), 6);

    public static readonly StyledProperty<double> GridLineThicknessProperty =
        AvaloniaProperty.Register<LuminaLineChart, double>(nameof(GridLineThickness), 1.0);

    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<LuminaLineChart, IBrush?>(nameof(GridLineBrush));

    public static readonly StyledProperty<IBrush?> AxisLabelBrushProperty =
        AvaloniaProperty.Register<LuminaLineChart, IBrush?>(nameof(AxisLabelBrush));

    public static readonly StyledProperty<double> AxisLabelFontSizeProperty =
        AvaloniaProperty.Register<LuminaLineChart, double>(nameof(AxisLabelFontSize), 11.0);

    public static readonly StyledProperty<bool> EnableCrosshairProperty =
        AvaloniaProperty.Register<LuminaLineChart, bool>(nameof(EnableCrosshair), true);

    public static readonly StyledProperty<IBrush?> CrosshairBrushProperty =
        AvaloniaProperty.Register<LuminaLineChart, IBrush?>(nameof(CrosshairBrush));

    public static readonly StyledProperty<IBrush?> TooltipBackgroundBrushProperty =
        AvaloniaProperty.Register<LuminaLineChart, IBrush?>(nameof(TooltipBackgroundBrush));

    public static readonly StyledProperty<IBrush?> TooltipTextBrushProperty =
        AvaloniaProperty.Register<LuminaLineChart, IBrush?>(nameof(TooltipTextBrush));

    public static readonly StyledProperty<double> HoverThresholdProperty =
        AvaloniaProperty.Register<LuminaLineChart, double>(nameof(HoverThreshold), 28.0);

    public static readonly StyledProperty<int> DecimationThresholdProperty =
        AvaloniaProperty.Register<LuminaLineChart, int>(nameof(DecimationThreshold), 2000);

    public static readonly StyledProperty<double> TransitionDurationProperty =
        AvaloniaProperty.Register<LuminaLineChart, double>(nameof(TransitionDuration), 350.0);

    public static readonly StyledProperty<bool> ShowLegendProperty =
        AvaloniaProperty.Register<LuminaLineChart, bool>(nameof(ShowLegend), false);

    public static readonly StyledProperty<LuminaChartLegendPosition> LegendPositionProperty =
        AvaloniaProperty.Register<LuminaLineChart, LuminaChartLegendPosition>(nameof(LegendPosition), LuminaChartLegendPosition.TopRight);

    public static readonly StyledProperty<IBrush?> LegendTextBrushProperty =
        AvaloniaProperty.Register<LuminaLineChart, IBrush?>(nameof(LegendTextBrush));

    public static readonly StyledProperty<LuminaChartAxis> XAxisProperty =
        AvaloniaProperty.Register<LuminaLineChart, LuminaChartAxis>(nameof(XAxis), coerce: CoerceXAxis);

    public static readonly StyledProperty<LuminaChartAxis> YAxisProperty =
        AvaloniaProperty.Register<LuminaLineChart, LuminaChartAxis>(nameof(YAxis), coerce: CoerceYAxis);

    private static readonly HashSet<AvaloniaProperty> RebuildProperties = new()
    {
        SeriesProperty,
        HorizontalGridLineCountProperty,
        VerticalGridLineCountProperty,
        AxisLabelBrushProperty,
        AxisLabelFontSizeProperty,
        DecimationThresholdProperty,
        ShowLegendProperty,
        LegendPositionProperty,
        Visual.BoundsProperty
    };

    private static readonly HashSet<AvaloniaProperty> InvalidateOnlyProperties = new()
    {
        ShowGridLinesProperty,
        ShowAxisLabelsProperty,
        GridLineThicknessProperty,
        GridLineBrushProperty,
        EnableCrosshairProperty,
        CrosshairBrushProperty,
        TooltipBackgroundBrushProperty,
        TooltipTextBrushProperty,
        HoverThresholdProperty,
        TransitionDurationProperty,
        LegendTextBrushProperty
    };

    public LuminaLineChart()
    {
        ClipToBounds = true;
        SetValue(SeriesProperty, new List<LuminaChartSeries>());
        SetValue(XAxisProperty, _xAxis);
        SetValue(YAxisProperty, _yAxis);
        _xAxis.PropertyChanged += OnAxisChanged;
        _yAxis.PropertyChanged += OnAxisChanged;
        ResourcesChanged += OnResourcesChanged;
        _labelTypeface = ChartDrawing.CreateTypeface(this);
    }

    private static LuminaChartAxis CoerceXAxis(AvaloniaObject instance, LuminaChartAxis value)
    {
        return value ?? (instance as LuminaLineChart)?._xAxis ?? new LuminaChartAxis();
    }

    private static LuminaChartAxis CoerceYAxis(AvaloniaObject instance, LuminaChartAxis value)
    {
        return value ?? (instance as LuminaLineChart)?._yAxis ?? new LuminaChartAxis();
    }

    private void OnAxisChanged(object? sender, PropertyChangedEventArgs e)
    {
        _isDirty = true;
        InvalidateTooltipCache();
        InvalidateVisual();
    }

    private void InvalidateTooltipCache()
    {
        _cachedTooltipMatches.Clear();
        _cachedTooltipLines.Clear();
        _cachedTooltipSize = default;
    }

    public IList<LuminaChartSeries>? Series
    {
        get => GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public bool ShowGridLines
    {
        get => GetValue(ShowGridLinesProperty);
        set => SetValue(ShowGridLinesProperty, value);
    }

    public bool ShowAxisLabels
    {
        get => GetValue(ShowAxisLabelsProperty);
        set => SetValue(ShowAxisLabelsProperty, value);
    }

    public int HorizontalGridLineCount
    {
        get => GetValue(HorizontalGridLineCountProperty);
        set => SetValue(HorizontalGridLineCountProperty, value);
    }

    public int VerticalGridLineCount
    {
        get => GetValue(VerticalGridLineCountProperty);
        set => SetValue(VerticalGridLineCountProperty, value);
    }

    public double GridLineThickness
    {
        get => GetValue(GridLineThicknessProperty);
        set => SetValue(GridLineThicknessProperty, value);
    }

    public IBrush? GridLineBrush
    {
        get => GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public IBrush? AxisLabelBrush
    {
        get => GetValue(AxisLabelBrushProperty);
        set => SetValue(AxisLabelBrushProperty, value);
    }

    public double AxisLabelFontSize
    {
        get => GetValue(AxisLabelFontSizeProperty);
        set => SetValue(AxisLabelFontSizeProperty, value);
    }

    public bool EnableCrosshair
    {
        get => GetValue(EnableCrosshairProperty);
        set => SetValue(EnableCrosshairProperty, value);
    }

    public IBrush? CrosshairBrush
    {
        get => GetValue(CrosshairBrushProperty);
        set => SetValue(CrosshairBrushProperty, value);
    }

    public IBrush? TooltipBackgroundBrush
    {
        get => GetValue(TooltipBackgroundBrushProperty);
        set => SetValue(TooltipBackgroundBrushProperty, value);
    }

    public IBrush? TooltipTextBrush
    {
        get => GetValue(TooltipTextBrushProperty);
        set => SetValue(TooltipTextBrushProperty, value);
    }

    public double HoverThreshold
    {
        get => GetValue(HoverThresholdProperty);
        set => SetValue(HoverThresholdProperty, value);
    }

    public int DecimationThreshold
    {
        get => GetValue(DecimationThresholdProperty);
        set => SetValue(DecimationThresholdProperty, value);
    }

    /// <summary>Gets or sets the X axis. Configure a fixed viewport or category mode here.</summary>
    public LuminaChartAxis XAxis
    {
        get => GetValue(XAxisProperty);
        set => SetValue(XAxisProperty, value);
    }

    /// <summary>Gets or sets the Y axis.</summary>
    public LuminaChartAxis YAxis
    {
        get => GetValue(YAxisProperty);
        set => SetValue(YAxisProperty, value);
    }

    /// <summary>
    /// Milliseconds to animate from the previous data state to the next. Set to 0 to disable.
    /// </summary>
    public double TransitionDuration
    {
        get => GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    /// <summary>Gets or sets whether to render a legend identifying each series by name and color.</summary>
    public bool ShowLegend
    {
        get => GetValue(ShowLegendProperty);
        set => SetValue(ShowLegendProperty, value);
    }

    /// <summary>Gets or sets where the legend is placed.</summary>
    public LuminaChartLegendPosition LegendPosition
    {
        get => GetValue(LegendPositionProperty);
        set => SetValue(LegendPositionProperty, value);
    }

    /// <summary>Gets or sets the legend text brush.</summary>
    public IBrush? LegendTextBrush
    {
        get => GetValue(LegendTextBrushProperty);
        set => SetValue(LegendTextBrushProperty, value);
    }

    /// <summary>
    /// Gets the last full data/geometry rebuild duration in milliseconds. Diagnostics helper.
    /// </summary>
    public double LastBuildMilliseconds => _lastBuildMilliseconds;

    /// <summary>
    /// Gets the duration in milliseconds of a repaint that only moved the crosshair/tooltip. Diagnostics helper.
    /// </summary>
    public double LastMoveRenderMilliseconds => _lastMoveRenderMilliseconds;

    /// <summary>
    /// Gets the total Render pass duration in milliseconds. Diagnostics helper.
    /// </summary>
    public double LastRenderMilliseconds => _lastRenderMilliseconds;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _renderStopwatch.Restart();

        if (Bounds.Width <= 0.0 || Bounds.Height <= 0.0)
        {
            _isDirty = true;
            _lastRenderMilliseconds = 0.0;
            return;
        }

        try
        {
        bool didRebuild = false;
        if (_isDirty)
        {
            didRebuild = true;
            bool wantTransition = _rebuildDueToDataChange && TransitionDuration > 0.0;
            _transitionPrev = wantTransition ? CaptureTransitionPoints() : _transitionPrev is { Count: > 0 } ? _transitionPrev : null;

            Rebuild();
            if (_renderSeries.Count == 0)
            {
                _inTransition = false;
                StopTransitionTimer();
                _renderStopwatch.Stop();
                _lastRenderMilliseconds = _renderStopwatch.Elapsed.TotalMilliseconds;
                RenderCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (wantTransition && _transitionPrev is { Count: > 0 })
            {
                _inTransition = true;
                _transitionT = 0.0;
                _transitionSw.Restart();
                EnsureTransitionTimer();
            }
            else
            {
                _inTransition = false;
                _transitionPrev = null;
                StopTransitionTimer();
            }

            _rebuildDueToDataChange = false;
            _isDirty = false;
        }

        if (_inTransition)
        {
            DrawStaticLayer(context);
        }
        else
        {
            if (_cachedStaticDrawing == null)
            {
                var staticDrawing = new DrawingGroup();
                using (DrawingContext staticContext = staticDrawing.Open())
                {
                    DrawStaticLayer(staticContext);
                }

                _cachedStaticDrawing = staticDrawing;
            }

            _cachedStaticDrawing.Draw(context);
        }

        if (_hoverPosition is { } hover)
        {
            DrawOverlay(context, hover);
        }

        // The legend stays above the hover overlay so tooltips never cover it.
        if (ShowLegend && _renderSeries.Count > 0)
        {
            DrawLegend(context);
        }

        _renderStopwatch.Stop();
        _lastRenderMilliseconds = _renderStopwatch.Elapsed.TotalMilliseconds;
        if (!didRebuild)
        {
            _lastMoveRenderMilliseconds = _lastRenderMilliseconds;
        }

        RenderCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _renderStopwatch.Stop();
            _cachedStaticDrawing = null;
            _isDirty = true;
            System.Diagnostics.Trace.TraceError("LuminaUI.Charts render failed: {0}", exception);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!EnableCrosshair)
        {
            return;
        }

        Point position = e.GetPosition(this);
        double threshold = Math.Max(1.0, HoverThreshold);
        Rect activeArea = _plotRect.Inflate(threshold);
        if (!activeArea.Contains(position))
        {
            // Outside the interactive area there is nothing to track. Drop the hover state so
            // subsequent repaints (live data, animations) skip hit-testing entirely, and repaint
            // once so the stale crosshair/tooltip is cleared.
            if (_hoverPosition.HasValue)
            {
                _hoverPosition = null;
                InvalidateTooltipCache();
                InvalidateVisual();
            }

            return;
        }

        _hoverPosition = position;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoverPosition != null)
        {
            _hoverPosition = null;
            InvalidateTooltipCache();
            InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _xAxis.PropertyChanged -= OnAxisChanged;
        _xAxis.PropertyChanged += OnAxisChanged;
        _yAxis.PropertyChanged -= OnAxisChanged;
        _yAxis.PropertyChanged += OnAxisChanged;

        UpdateSeriesSubscription(null, Series);
        _isDirty = true;
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopTransitionTimer();
        _cachedStaticDrawing = null;
        InvalidateTooltipCache();
        _cachedCrosshairPen = null;
        if (_seriesSubscription != null)
        {
            _seriesSubscription.CollectionChanged -= OnSeriesCollectionChanged;
            _seriesSubscription = null;
        }

        foreach (INotifyCollectionChanged sub in _pointsSubscriptions)
        {
            sub.CollectionChanged -= OnPointsCollectionChanged;
        }

        _pointsSubscriptions.Clear();

        _xAxis.PropertyChanged -= OnAxisChanged;
        _yAxis.PropertyChanged -= OnAxisChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeriesProperty)
        {
            UpdateSeriesSubscription(change.GetOldValue<IList<LuminaChartSeries>?>(), change.GetNewValue<IList<LuminaChartSeries>?>());
            _rebuildDueToDataChange = true;
            _cachedStaticDrawing = null;
        }
        else if (change.Property == XAxisProperty)
        {
            var oldAxis = change.GetOldValue<LuminaChartAxis>();
            var newAxis = change.GetNewValue<LuminaChartAxis>() ?? new LuminaChartAxis();
            if (oldAxis != null && !ReferenceEquals(oldAxis, newAxis))
            {
                oldAxis.PropertyChanged -= OnAxisChanged;
            }

            _xAxis = newAxis;
            _xAxis.PropertyChanged -= OnAxisChanged;
            _xAxis.PropertyChanged += OnAxisChanged;
            _cachedStaticDrawing = null;
            InvalidateTooltipCache();
            _isDirty = true;
            InvalidateVisual();
        }
        else if (change.Property == YAxisProperty)
        {
            var oldAxis = change.GetOldValue<LuminaChartAxis>();
            var newAxis = change.GetNewValue<LuminaChartAxis>() ?? new LuminaChartAxis();
            if (oldAxis != null && !ReferenceEquals(oldAxis, newAxis))
            {
                oldAxis.PropertyChanged -= OnAxisChanged;
            }

            _yAxis = newAxis;
            _yAxis.PropertyChanged -= OnAxisChanged;
            _yAxis.PropertyChanged += OnAxisChanged;
            _cachedStaticDrawing = null;
            InvalidateTooltipCache();
            _isDirty = true;
            InvalidateVisual();
        }

        if (change.Property == Visual.BoundsProperty)
        {
            var bounds = change.GetNewValue<Rect>();
            var newSize = new Size(bounds.Width, bounds.Height);
            if (_lastLayoutSize != newSize)
            {
                _lastLayoutSize = newSize;
                _isDirty = true;
            }

            _cachedStaticDrawing = null;
            InvalidateVisual();
        }
        else if (RebuildProperties.Contains(change.Property))
        {
            _cachedStaticDrawing = null;
            _isDirty = true;
            InvalidateVisual();
        }
        else if (InvalidateOnlyProperties.Contains(change.Property))
        {
            _cachedStaticDrawing = null;
            InvalidateTooltipCache();
            _cachedCrosshairPen = null;
            InvalidateVisual();
        }
    }

    private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e)
    {
        _cachedStaticDrawing = null;
        InvalidateTooltipCache();
        _cachedCrosshairPen = null;
        _isDirty = true;
        InvalidateVisual();
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _rebuildDueToDataChange = true;
        _isDirty = true;
        SyncDataSubscriptions();
        InvalidateVisual();
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _rebuildDueToDataChange = true;
        _isDirty = true;
        InvalidateVisual();
    }

    private void OnSeriesItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only geometry-affecting changes need the transition animation; style-only changes
        // (color, thickness, title) still rebuild, they just skip the morph.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(LuminaChartSeries.Points))
        {
            _rebuildDueToDataChange = true;

            // A replaced Points list needs its own change subscription right away, otherwise
            // edits made before the next render pass go unnoticed.
            SyncDataSubscriptions();
        }

        _cachedStaticDrawing = null;
        InvalidateTooltipCache();
        _isDirty = true;
        InvalidateVisual();
    }

    private void UpdateSeriesSubscription(IList<LuminaChartSeries>? old, IList<LuminaChartSeries>? next)
    {
        if (_seriesSubscription != null)
        {
            _seriesSubscription.CollectionChanged -= OnSeriesCollectionChanged;
            _seriesSubscription = null;
        }

        if (next is INotifyCollectionChanged nextObservable)
        {
            _seriesSubscription = nextObservable;
            _seriesSubscription.CollectionChanged += OnSeriesCollectionChanged;
        }

        SyncDataSubscriptions();
    }

    /// <summary>
    /// Keeps change subscriptions in sync with the current series list: one subscription per
    /// observable <c>Points</c> collection and one per series object, so both in-place point
    /// edits and series property edits (color, thickness, title) repaint the chart.
    /// </summary>
    private void SyncDataSubscriptions()
    {
        var desiredPoints = new HashSet<INotifyCollectionChanged>();
        var desiredSeries = new HashSet<LuminaChartSeries>();
        IList<LuminaChartSeries>? series = Series;
        if (series != null)
        {
            foreach (LuminaChartSeries item in series)
            {
                if (item == null)
                {
                    continue;
                }

                desiredSeries.Add(item);
                if (item.Points is INotifyCollectionChanged points)
                {
                    desiredPoints.Add(points);
                }
            }
        }

        foreach (INotifyCollectionChanged stale in _pointsSubscriptions)
        {
            if (!desiredPoints.Contains(stale))
            {
                stale.CollectionChanged -= OnPointsCollectionChanged;
            }
        }

        foreach (INotifyCollectionChanged add in desiredPoints)
        {
            if (!_pointsSubscriptions.Contains(add))
            {
                add.CollectionChanged += OnPointsCollectionChanged;
            }
        }

        foreach (LuminaChartSeries stale in _seriesItemSubscriptions)
        {
            if (!desiredSeries.Contains(stale))
            {
                stale.PropertyChanged -= OnSeriesItemPropertyChanged;
            }
        }

        foreach (LuminaChartSeries add in desiredSeries)
        {
            if (!_seriesItemSubscriptions.Contains(add))
            {
                add.PropertyChanged += OnSeriesItemPropertyChanged;
            }
        }

        _pointsSubscriptions = desiredPoints;
        _seriesItemSubscriptions = desiredSeries;
    }

    private void EnsureTransitionTimer()
    {
        if (_transitionTimer == null)
        {
            _transitionTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher.UIThread)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _transitionTimer.Tick += OnTransitionTick;
        }

        if (_transitionTimer.IsEnabled == false)
        {
            _transitionTimer.Start();
        }
    }

    private void StopTransitionTimer()
    {
        if (_transitionTimer is { IsEnabled: true })
        {
            _transitionTimer.Stop();
        }
    }

    private void OnTransitionTick(object? sender, EventArgs e)
    {
        _transitionT = Math.Min(1.0, _transitionSw.Elapsed.TotalMilliseconds / Math.Max(1.0, TransitionDuration));
        if (_transitionT >= 1.0)
        {
            _inTransition = false;
            _transitionPrev = null;
            StopTransitionTimer();
        }

        InvalidateVisual();
    }

    private List<(Point[]? Line, Point[]? Area, Point[]? Raw)> CaptureTransitionPoints()
    {
        var snapshot = new List<(Point[]? Line, Point[]? Area, Point[]? Raw)>(_renderSeries.Count);
        bool morphing = _inTransition && _transitionPrev != null && _transitionT > 0.0 && _transitionT < 1.0;
        for (int index = 0; index < _renderSeries.Count; index++)
        {
            RenderSeries render = _renderSeries[index];
            if (morphing && index < _transitionPrev!.Count)
            {
                var (prevLine, prevArea, prevRaw) = _transitionPrev[index];
                snapshot.Add((
                    MorphPoints(prevLine, render.LinePoints, _transitionT),
                    MorphPoints(prevArea, render.AreaPoints, _transitionT),
                    MorphPoints(prevRaw, render.RawPoints, _transitionT)));
            }
            else
            {
                snapshot.Add((render.LinePoints, render.AreaPoints, render.RawPoints));
            }
        }

        return snapshot;
    }

    private static Point[]? MorphPoints(Point[]? from, Point[]? to, double t)
    {
        if (from is not { Length: > 0 })
        {
            return to;
        }

        if (to is not { Length: > 0 })
        {
            return from;
        }

        if (t <= 0.0)
        {
            return from;
        }

        if (t >= 1.0)
        {
            return to;
        }

        int common = Math.Min(from.Length, to.Length);
        var result = new Point[to.Length];
        for (int index = 0; index < common; index++)
        {
            result[index] = Lerp(from[index], to[index], t);
        }

        for (int index = common; index < to.Length; index++)
        {
            result[index] = to[index];
        }

        return result;
    }

    private void Rebuild()
    {
        _lastBuildMilliseconds = 0.0;
        _buildStopwatch.Reset();
        _buildStopwatch.Start();
        try
        {
            RebuildCore();
        }
        finally
        {
            _buildStopwatch.Stop();
            _lastBuildMilliseconds = _buildStopwatch.Elapsed.TotalMilliseconds;
        }
    }

    private void RebuildCore()
    {
        _cachedStaticDrawing = null;
        // Cached tooltip text is keyed by series/value only, so a rebuild (font size, label
        // formatter, culture, ...) must drop it as well.
        InvalidateTooltipCache();
        _renderSeries.Clear();
        _xTickValues.Clear();
        _yTickValues.Clear();
        _xTickLabels.Clear();
        _yTickLabels.Clear();

        SyncDataSubscriptions();

        List<LuminaChartSeries> visible = GetVisibleSeries();
        if (visible.Count == 0)
        {
            return;
        }

        (_xScale, _yScale) = ComputeScales(visible, Math.Max(1, VerticalGridLineCount), Math.Max(1, HorizontalGridLineCount));

        BuildTickLabels();

        Rect plotRect = ComputePlotRect();
        if (plotRect.Width <= 0.0 || plotRect.Height <= 0.0)
        {
            return;
        }

        _plotRect = plotRect;
        int barGroupCount = 0;
        foreach (LuminaChartSeries item in visible)
        {
            if (item is LuminaBarSeries)
            {
                barGroupCount++;
            }
        }

        int barGroupIndex = 0;
        for (int index = 0; index < visible.Count; index++)
        {
            RenderSeries? render = BuildRenderSeries(visible[index], index, barGroupIndex, barGroupCount);
            if (render != null)
            {
                _renderSeries.Add(render);
                if (visible[index] is LuminaBarSeries)
                {
                    barGroupIndex++;
                }
            }
        }

        BuildLegendCache();
    }

    private void BuildLegendCache()
    {
        _cachedLegendItems.Clear();
        _cachedLegendContentWidth = 0.0;
        if (!ShowLegend || _renderSeries.Count == 0)
        {
            return;
        }

        IBrush textBrush = LegendTextBrush ?? AxisLabelBrush ?? Brushes.Gray;
        double fontSize = ChartDrawing.LegendFontSize;

        for (int index = 0; index < _renderSeries.Count; index++)
        {
            RenderSeries render = _renderSeries[index];
            string title = string.IsNullOrEmpty(render.Source.Title)
                ? $"S{index + 1}"
                : render.Source.Title!;
            FormattedText text = ChartDrawing.CreateText(title, _labelTypeface, textBrush, fontSize);
            _cachedLegendItems.Add((text, render.Stroke));
        }

        _cachedLegendContentWidth = ChartDrawing.MeasureLegend(_cachedLegendItems);
    }

    private void BuildTickLabels()
    {
        _labelTypeface = ChartDrawing.CreateTypeface(this);
        IBrush labelBrush = AxisLabelBrush ?? Brushes.Gray;
        double fontSize = AxisLabelFontSize;

        if (_yScale.Step > 0.0 && double.IsFinite(_yScale.Step))
        {
            for (double y = TickStart(_yScale.Min, _yScale.Step); y <= _yScale.Max + 1e-9; y += _yScale.Step)
            {
                _yTickValues.Add(y);
                if (ShowAxisLabels)
                {
                    string text = _yAxis.LabelFormatter?.Invoke(y) ?? FormatTick(y, _yScale.Step);
                    _yTickLabels.Add(ChartDrawing.CreateText(text, _labelTypeface, labelBrush, fontSize));
                }
            }
        }

        if (_xScale.Step > 0.0 && double.IsFinite(_xScale.Step))
        {
            if (_xAxis.Kind == LuminaChartAxisKind.Category)
            {
                int step = Math.Max(1, (int)Math.Round(_xScale.Step));
                int start = (int)Math.Max(0, Math.Ceiling(_xScale.Min));
                int end = (int)Math.Floor(_xScale.Max);
                for (int i = start; i <= end; i += step)
                {
                    _xTickValues.Add(i);
                    if (ShowAxisLabels)
                    {
                        string text = FormatCategoryLabel(i);
                        _xTickLabels.Add(ChartDrawing.CreateText(text, _labelTypeface, labelBrush, fontSize));
                    }
                }
            }
            else
            {
                for (double x = TickStart(_xScale.Min, _xScale.Step); x <= _xScale.Max + 1e-9; x += _xScale.Step)
                {
                    _xTickValues.Add(x);
                    if (ShowAxisLabels)
                    {
                        string text = _xAxis.LabelFormatter?.Invoke(x) ?? FormatTick(x, _xScale.Step);
                        _xTickLabels.Add(ChartDrawing.CreateText(text, _labelTypeface, labelBrush, fontSize));
                    }
                }
            }
        }
    }

    private string FormatCategoryLabel(int index)
    {
        if (_xAxis.Categories != null && index >= 0 && index < _xAxis.Categories.Count)
        {
            return _xAxis.Categories[index];
        }

        return _xAxis.LabelFormatter?.Invoke(index) ?? index.ToString(CultureInfo.CurrentCulture);
    }

    private Rect ComputePlotRect()
    {
        double maxYLabelWidth = 0.0;
        double yLabelHeight = 0.0;
        foreach (FormattedText label in _yTickLabels)
        {
            maxYLabelWidth = Math.Max(maxYLabelWidth, label.Width);
            yLabelHeight = label.Height;
        }

        double left = PlotLeftPad + maxYLabelWidth;
        double top = PlotTop;
        double bottomMargin = PlotBottomPad;
        if (ShowAxisLabels)
        {
            bottomMargin += yLabelHeight + PlotBottomPad;
        }

        if (ShowLegend)
        {
            if (LegendPosition is LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.TopRight)
            {
                top += LegendBandHeight + LegendBandGap;
            }
            else
            {
                bottomMargin += LegendBandHeight + LegendBandGap;
            }
        }

        double width = Math.Max(0.0, Bounds.Width - left - PlotRight);
        double height = Math.Max(0.0, Bounds.Height - top - bottomMargin);
        return new Rect(left, top, width, height);
    }

    private RenderSeries? BuildRenderSeries(LuminaChartSeries source, int paletteIndex, int barGroupIndex, int barGroupCount)
    {
        var render = new RenderSeries { Source = source };
        render.Stroke = source.Stroke ?? ChartDrawing.ResolvePaletteBrush(this, paletteIndex);
        render.Fill = source.Fill ?? (source.ShowArea ? ChartDrawing.CreateTranslucent(render.Stroke, 0.22) : null);
        render.Thickness = Math.Max(1.0, source.StrokeThickness);
        render.LinePen = new Pen(render.Stroke, render.Thickness, null, PenLineCap.Round, PenLineJoin.Round);
        render.MarkerPen = new Pen(render.Stroke, Math.Max(1.0, render.Thickness));
        render.BarPen = new Pen(render.Stroke, Math.Min(1.5, Math.Max(0.5, render.Thickness)));
        render.HighlightRingPen = new Pen(render.Stroke, 1.5);
        render.HighlightMarkerPen = new Pen(render.Stroke, 2.0);

        int count = MapFinitePoints(render, source);
        if (count == 0)
        {
            return null;
        }

        switch (source)
        {
            case LuminaBarSeries bar:
                BuildBars(render, bar, barGroupIndex, barGroupCount);
                break;
            case LuminaScatterSeries scatter:
                BuildScatter(render, scatter);
                break;
            case LuminaSplineSeries spline:
                BuildSpline(render, spline);
                break;
            default:
                BuildLineAndArea(render, source);
                break;
        }

        return render;
    }

    private void BuildLineAndArea(RenderSeries render, LuminaChartSeries source)
    {
        int threshold = Math.Max(4, DecimationThreshold);
        if (render.Px.Length > threshold && _plotRect.Width >= 4.0)
        {
            render.Decimated = true;
            (render.LinePoints, render.AreaPoints) = Decimate(render);
            render.RawPoints = render.LinePoints;
        }
        else
        {
            render.LinePoints = ToPointArray(render.Px, render.Py);
            render.AreaPoints = source.ShowArea ? render.LinePoints : null;
            render.RawPoints = render.LinePoints;
        }

        Point[]? markerPoints = render.RawPoints;
        if (source.IsStep && render.LinePoints is { Length: > 0 })
        {
            render.LinePoints = ToStepPoints(render.LinePoints);
            render.AreaPoints = source.ShowArea ? render.LinePoints : null;
        }

        render.Line = BuildLineGeometry(render.LinePoints);
        if (render.AreaPoints is { Length: >= 2 })
        {
            render.Area = BuildAreaGeometry(render.AreaPoints, _plotRect.Bottom);
        }

        if (source.ShowMarkers && !render.Decimated && markerPoints is { Length: <= SubsetMarkersLimit })
        {
            render.Markers = BuildMarkerGeometry(markerPoints, 2.5);
        }
    }

    private static Point[] ToStepPoints(Point[] points)
    {
        if (points.Length < 2)
        {
            return points;
        }

        var result = new List<Point>(points.Length * 2 - 1);
        result.Add(points[0]);
        for (int index = 1; index < points.Length; index++)
        {
            result.Add(new Point(points[index].X, points[index - 1].Y));
            result.Add(points[index]);
        }

        return result.ToArray();
    }

    private void BuildBars(RenderSeries render, LuminaBarSeries series, int barGroupIndex, int barGroupCount)
    {
        render.IsBar = true;
        render.BarCornerRadius = Math.Max(0.0, series.CornerRadius);
        render.Fill ??= render.Stroke;
        render.BarRects = BuildBarRects(render, Math.Clamp(series.BarWidthRatio, 0.01, 1.0), barGroupIndex, barGroupCount);
        render.Markers = null;
    }

    private void BuildScatter(RenderSeries render, LuminaScatterSeries series)
    {
        render.IsScatter = true;
        render.MarkerRadius = Math.Max(0.5, series.MarkerSize);
        render.Markers = BuildMarkerGeometry(ToPointArray(render.Px, render.Py), render.MarkerRadius);
    }

    private void BuildSpline(RenderSeries render, LuminaSplineSeries series)
    {
        Point[] splinePoints = BuildSplinePoints(render.Px, render.Py, Math.Max(2, series.SamplesPerSegment));
        render.LinePoints = splinePoints;
        render.RawPoints = ToPointArray(render.Px, render.Py);
        render.Line = BuildLineGeometry(splinePoints);
        if (series.ShowArea)
        {
            render.Area = BuildAreaGeometry(splinePoints, _plotRect.Bottom);
        }

        if (series.ShowMarkers && render.Px.Length <= SubsetMarkersLimit)
        {
            render.Markers = BuildMarkerGeometry(render.RawPoints, 2.5);
        }
    }

    private Rect[] BuildBarRects(RenderSeries render, double widthRatio, int barGroupIndex, int barGroupCount)
    {
        if (render.Px.Length == 0)
        {
            return [];
        }

        double slot = ComputeBarSlot(render.Px);
        int groupCount = Math.Max(1, barGroupCount);
        double band = slot / groupCount;
        double width = Math.Max(1.0, band * widthRatio);
        double baseline = Math.Clamp(MapY(Math.Max(0.0, _yScale.Min), _plotRect, _yScale), _plotRect.Top, _plotRect.Bottom);
        var rects = new Rect[render.Px.Length];
        for (int index = 0; index < render.Px.Length; index++)
        {
            double top = render.Py[index];
            double bandLeft = render.Px[index] - slot / 2.0 + barGroupIndex * band;
            double left = bandLeft + (band - width) / 2.0;
            rects[index] = new Rect(left, Math.Min(top, baseline), width, Math.Max(0.5, Math.Abs(baseline - top)));
        }

        return rects;
    }

    private static double ComputeBarSlot(double[] px)
    {
        double minGap = double.PositiveInfinity;
        for (int index = 1; index < px.Length; index++)
        {
            double gap = px[index] - px[index - 1];
            if (gap > 0.0 && gap < minGap)
            {
                minGap = gap;
            }
        }

        if (double.IsFinite(minGap))
        {
            return minGap;
        }

        return px.Length > 1 ? (px[^1] - px[0]) / (px.Length - 1) : 0.0;
    }

    private static Point[] BuildSplinePoints(double[] px, double[] py, int samplesPerSegment)
    {
        int count = px.Length;
        if (count < 2)
        {
            return ToPointArray(px, py);
        }

        var result = new List<Point>((count - 1) * samplesPerSegment + 1);
        result.Add(new Point(px[0], py[0]));
        for (int index = 0; index < count - 1; index++)
        {
            Point p0 = index > 0 ? new Point(px[index - 1], py[index - 1]) : new Point(px[index], py[index]);
            Point p1 = new Point(px[index], py[index]);
            Point p2 = new Point(px[index + 1], py[index + 1]);
            Point p3 = index + 2 < count ? new Point(px[index + 2], py[index + 2]) : p2;
            for (int sample = 1; sample <= samplesPerSegment; sample++)
            {
                double t = sample / (double)samplesPerSegment;
                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        return result.ToArray();
    }

    private static Point CatmullRom(Point p0, Point p1, Point p2, Point p3, double t)
    {
        double t2 = t * t;
        double t3 = t2 * t;
        double x = 0.5 * ((2.0 * p1.X) + (-p0.X + p2.X) * t + (2.0 * p0.X - 5.0 * p1.X + 4.0 * p2.X - p3.X) * t2 + (-p0.X + 3.0 * p1.X - 3.0 * p2.X + p3.X) * t3);
        double y = 0.5 * ((2.0 * p1.Y) + (-p0.Y + p2.Y) * t + (2.0 * p0.Y - 5.0 * p1.Y + 4.0 * p2.Y - p3.Y) * t2 + (-p0.Y + 3.0 * p1.Y - 3.0 * p2.Y + p3.Y) * t3);
        return new Point(x, y);
    }

    private int MapFinitePoints(RenderSeries render, LuminaChartSeries source)
    {
        if (source.Points == null || source.Points.Count == 0)
        {
            render.Px = [];
            render.Py = [];
            render.Values = [];
            return 0;
        }

        bool categoryX = _xAxis.Kind == LuminaChartAxisKind.Category;
        var px = new List<double>(Math.Min(source.Points.Count, 4096));
        var py = new List<double>(px.Capacity);
        var values = new List<LuminaChartPoint>(px.Capacity);
        bool sorted = true;
        double previous = double.NegativeInfinity;
        int pointIndex = 0;

        foreach (LuminaChartPoint point in source.Points)
        {
            if (point.IsEmpty)
            {
                pointIndex++;
                continue;
            }

            double xValue = categoryX ? pointIndex : point.X;
            double pixelX = MapX(xValue, _plotRect, _xScale);
            double pixelY = MapY(point.Y, _plotRect, _yScale);
            px.Add(pixelX);
            py.Add(pixelY);
            values.Add(point);
            pointIndex++;
            if (pixelX < previous)
            {
                sorted = false;
            }

            previous = pixelX;
        }

        render.Px = px.ToArray();
        render.Py = py.ToArray();
        render.Values = values.ToArray();
        render.Sorted = sorted;
        render.Order = sorted ? null : BuildOrder(render.Px);
        return render.Px.Length;
    }

    private static int[] BuildOrder(double[] px)
    {
        int count = px.Length;
        var order = new int[count];
        for (int index = 0; index < count; index++)
        {
            order[index] = index;
        }

        Array.Sort(order, (a, b) => px[a].CompareTo(px[b]));
        return order;
    }

    private (Point[] Line, Point[]? Envelope) Decimate(RenderSeries render)
    {
        int pointCount = render.Px.Length;
        int bucketCount = (int)Math.Clamp(Math.Ceiling(_plotRect.Width), 4.0, pointCount);

        var topX = new double[bucketCount];
        var topY = new double[bucketCount];
        var bottomX = new double[bucketCount];
        var bottomY = new double[bucketCount];
        var valid = new bool[bucketCount];

        for (int index = 0; index < pointCount; index++)
        {
            int bucket = (int)Math.Clamp(Math.Round(render.Px[index] - _plotRect.Left), 0.0, bucketCount - 1);
            double x = render.Px[index];
            double y = render.Py[index];
            if (!valid[bucket])
            {
                valid[bucket] = true;
                topX[bucket] = x;
                topY[bucket] = y;
                bottomX[bucket] = x;
                bottomY[bucket] = y;
            }
            else
            {
                if (y < topY[bucket])
                {
                    topY[bucket] = y;
                    topX[bucket] = x;
                }

                if (y > bottomY[bucket])
                {
                    bottomY[bucket] = y;
                    bottomX[bucket] = x;
                }
            }
        }

        var line = new List<Point>(bucketCount * 2);
        var envelope = render.Source.ShowArea ? new List<Point>(bucketCount) : null;
        for (int bucket = 0; bucket < bucketCount; bucket++)
        {
            if (!valid[bucket])
            {
                continue;
            }

            line.Add(new Point(topX[bucket], topY[bucket]));
            line.Add(new Point(bottomX[bucket], bottomY[bucket]));
            envelope?.Add(new Point(topX[bucket], topY[bucket]));
        }

        return (line.ToArray(), envelope?.ToArray());
    }

    private static Point[] ToPointArray(double[] px, double[] py)
    {
        int count = px.Length;
        var result = new Point[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = new Point(px[index], py[index]);
        }

        return result;
    }

    private static StreamGeometry BuildLineGeometry(Point[] points)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(points[0], isFilled: false);
            for (int index = 1; index < points.Length; index++)
            {
                context.LineTo(points[index]);
            }
        }

        return geometry;
    }

    private static StreamGeometry BuildAreaGeometry(Point[] points, double bottom)
    {
        // Non-zero: an area whose curve doubles back on itself would otherwise punch holes.
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.SetFillRule(FillRule.NonZero);
            context.BeginFigure(points[0], isFilled: true);
            for (int index = 1; index < points.Length; index++)
            {
                context.LineTo(points[index]);
            }

            context.LineTo(new Point(points[^1].X, bottom));
            context.LineTo(new Point(points[0].X, bottom));
            context.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static StreamGeometry BuildMarkerGeometry(Point[] points, double radius)
    {
        // Every marker is a sub-path of one geometry. With the default even-odd rule, overlapping
        // dots cancel each other out - two coincident points vanish completely and a partial
        // overlap leaves a lens-shaped bite. Non-zero unions them instead.
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.SetFillRule(FillRule.NonZero);
            foreach (Point point in points)
            {
                context.BeginFigure(new Point(point.X, point.Y - radius), isFilled: true);
                context.ArcTo(new Point(point.X, point.Y + radius), new Size(radius, radius), 0.0, false, SweepDirection.Clockwise);
                context.ArcTo(new Point(point.X, point.Y - radius), new Size(radius, radius), 0.0, false, SweepDirection.Clockwise);
                context.EndFigure(isClosed: true);
            }
        }

        return geometry;
    }

    private static StreamGeometry? BuildInterpolatedLine(Point[]? prev, Point[]? next, double t)
    {
        Point[]? points = MorphPoints(prev, next, t);
        return points is { Length: > 0 } ? BuildLineGeometry(points) : null;
    }

    private static StreamGeometry? BuildInterpolatedArea(Point[]? prev, Point[]? next, double t, double bottom)
    {
        Point[]? points = MorphPoints(prev, next, t);
        return points is { Length: >= 2 } ? BuildAreaGeometry(points, bottom) : null;
    }

    private static Point Lerp(Point from, Point to, double t)
    {
        return new Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
    }

    private List<LuminaChartSeries> GetVisibleSeries()
    {
        var result = new List<LuminaChartSeries>();
        IList<LuminaChartSeries>? series = Series;
        if (series == null)
        {
            return result;
        }

        foreach (LuminaChartSeries item in series)
        {
            if (item == null || item.Points == null)
            {
                continue;
            }

            foreach (LuminaChartPoint point in item.Points)
            {
                if (!point.IsEmpty)
                {
                    result.Add(item);
                    break;
                }
            }
        }

        return result;
    }

    private (AxisScale X, AxisScale Y) ComputeScales(List<LuminaChartSeries> series, int maxXTicks, int maxYTicks)
    {
        double xMin = double.PositiveInfinity;
        double xMax = double.NegativeInfinity;
        double yMin = double.PositiveInfinity;
        double yMax = double.NegativeInfinity;
        int maxCategories = _xAxis.Categories?.Count ?? 0;
        bool hasBarSeries = false;

        foreach (LuminaChartSeries item in series)
        {
            if (item is LuminaBarSeries)
            {
                hasBarSeries = true;
            }

            int categoryCount = 0;
            if (item.Points != null)
            {
                foreach (LuminaChartPoint point in item.Points)
                {
                    categoryCount++;
                    if (point.IsEmpty)
                    {
                        continue;
                    }

                    double x = point.X;
                    double y = point.Y;
                    if (double.IsFinite(x))
                    {
                        if (x < xMin)
                        {
                            xMin = x;
                        }

                        if (x > xMax)
                        {
                            xMax = x;
                        }
                    }

                    if (double.IsFinite(y))
                    {
                        if (y < yMin)
                        {
                            yMin = y;
                        }

                        if (y > yMax)
                        {
                            yMax = y;
                        }
                    }
                }
            }

            maxCategories = Math.Max(maxCategories, categoryCount);
        }

        if (_xAxis.Kind == LuminaChartAxisKind.Category)
        {
            xMin = -0.5;
            xMax = Math.Max(0.5, maxCategories - 0.5);
        }
        else if (hasBarSeries && !_xAxis.Minimum.HasValue && !_xAxis.Maximum.HasValue)
        {
            double minGap = ComputeMinDataGap(series);
            if (double.IsFinite(minGap) && minGap > 0.0)
            {
                xMin -= minGap * 0.5;
                xMax += minGap * 0.5;
            }
        }

        if (!double.IsFinite(xMin) || !double.IsFinite(xMax))
        {
            (xMin, xMax) = (0.0, 1.0);
        }

        if (!double.IsFinite(yMin) || !double.IsFinite(yMax))
        {
            (yMin, yMax) = (0.0, 1.0);
        }

        ApplyAxisRange(_xAxis, ref xMin, ref xMax);
        ApplyAxisRange(_yAxis, ref yMin, ref yMax);

        PadEqualRange(ref xMin, ref xMax);
        PadEqualRange(ref yMin, ref yMax);

        double xStep = _xAxis.TickStep is > 0.0 && double.IsFinite(_xAxis.TickStep.Value)
            ? _xAxis.TickStep.Value
            : (_xAxis.Kind == LuminaChartAxisKind.Category
                ? ComputeCategoryStep(xMin, xMax)
                : GetNiceStep(xMin, xMax, maxXTicks));

        double yStep = _yAxis.TickStep is > 0.0 && double.IsFinite(_yAxis.TickStep.Value)
            ? _yAxis.TickStep.Value
            : GetNiceStep(yMin, yMax, maxYTicks);

        if (xStep <= 0.0 || !double.IsFinite(xStep))
        {
            xStep = 1.0;
        }

        if (yStep <= 0.0 || !double.IsFinite(yStep))
        {
            yStep = 1.0;
        }

        return (new AxisScale(xMin, xMax, xStep), new AxisScale(yMin, yMax, yStep));
    }

    /// <summary>
    /// Picks an integer tick step for a category axis. Every category gets a label while the
    /// labels still fit, and the step widens as the plot narrows or the category set grows so
    /// labels and grid lines never pile up.
    /// </summary>
    private double ComputeCategoryStep(double min, double max)
    {
        int categoryCount = (int)Math.Round(max - min);
        if (categoryCount <= 1)
        {
            return 1.0;
        }

        double widestLabel = EstimateCategoryLabelWidth(categoryCount);
        // Horizontal budget: control width minus the y-axis label gutter and a little padding.
        double availableWidth = Math.Max(48.0, Bounds.Width - PlotLeftPad - PlotRight - 44.0);
        int fittingLabels = widestLabel > 0.0
            ? Math.Max(1, (int)Math.Floor(availableWidth / (widestLabel + 10.0)))
            : categoryCount;

        return Math.Max(1.0, Math.Ceiling(categoryCount / (double)Math.Max(1, fittingLabels)));
    }

    private double EstimateCategoryLabelWidth(int categoryCount)
    {
        double fontSize = AxisLabelFontSize;
        IReadOnlyList<string>? categories = _xAxis.Categories;
        if (categories is not { Count: > 0 })
        {
            return EstimateTextWidth((categoryCount - 1).ToString(CultureInfo.CurrentCulture), fontSize);
        }

        double widest = 0.0;
        foreach (string category in categories)
        {
            widest = Math.Max(widest, EstimateTextWidth(category, fontSize));
        }

        return widest;
    }

    private static double EstimateTextWidth(string? text, double fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0.0;
        }

        double width = 0.0;
        foreach (char character in text)
        {
            // CJK glyphs occupy roughly one em, latin ones noticeably less.
            width += character >= '\u2E80' ? fontSize : fontSize * 0.58;
        }

        return width;
    }

    private static double ComputeMinDataGap(List<LuminaChartSeries> series)
    {
        double minGap = double.PositiveInfinity;
        foreach (LuminaChartSeries s in series)
        {
            if (s is not LuminaBarSeries || s.Points == null)
            {
                continue;
            }

            double prev = double.NaN;
            foreach (LuminaChartPoint pt in s.Points)
            {
                if (pt.IsEmpty || !double.IsFinite(pt.X))
                {
                    continue;
                }

                if (!double.IsNaN(prev))
                {
                    double diff = Math.Abs(pt.X - prev);
                    if (diff > 1e-6 && diff < minGap)
                    {
                        minGap = diff;
                    }
                }

                prev = pt.X;
            }
        }

        return double.IsFinite(minGap) ? minGap : 1.0;
    }

    private static void ApplyAxisRange(LuminaChartAxis axis, ref double rangeMin, ref double rangeMax)
    {
        if (axis.Minimum.HasValue)
        {
            rangeMin = axis.Minimum.Value;
        }

        if (axis.Maximum.HasValue)
        {
            rangeMax = axis.Maximum.Value;
        }
    }

    private static void PadEqualRange(ref double min, ref double max)
    {
        if (min < max)
        {
            return;
        }

        double pad = Math.Abs(min) * 0.1;
        if (pad <= 0.0)
        {
            pad = 1.0;
        }

        min -= pad;
        max += pad;
    }

    private static double GetNiceStep(double min, double max, int maxTicks)
    {
        double range = max - min;
        if (range <= 0.0)
        {
            return 1.0;
        }

        double step = NiceNum(range / Math.Max(1, maxTicks), true);
        return step <= 0.0 ? 1.0 : step;
    }

    private static double TickStart(double min, double step)
    {
        return step > 0.0 ? Math.Ceiling(min / step) * step : min;
    }

    private static double NiceNum(double range, bool round)
    {
        double exponent = Math.Floor(Math.Log10(range));
        double fraction = range / Math.Pow(10.0, exponent);
        double niceFraction = round
            ? fraction switch
            {
                < 1.5 => 1.0,
                < 3.0 => 2.0,
                < 7.0 => 5.0,
                _ => 10.0
            }
            : fraction switch
            {
                <= 1.0 => 1.0,
                <= 2.0 => 2.0,
                <= 5.0 => 5.0,
                _ => 10.0
            };
        return niceFraction * Math.Pow(10.0, exponent);
    }

    private void DrawStaticLayer(DrawingContext context)
    {
        if (ShowGridLines)
        {
            DrawGrid(context);
        }

        // Markers sit centred on their data point, so a point on the axis edge would be sliced in
        // half by a clip that stops exactly at the plot rect. Give the series layer enough slack to
        // draw a whole marker.
        double markerSlack = 0.0;
        foreach (RenderSeries render in _renderSeries)
        {
            if (render.Markers != null)
            {
                markerSlack = Math.Max(markerSlack, render.MarkerRadius + 1.0);
            }
        }

        using (context.PushClip(_plotRect.Inflate(markerSlack)))
        {
            DrawSeries(context);
        }
    }

    private void DrawGrid(DrawingContext context)
    {
        IBrush gridBrush = GridLineBrush ?? Brushes.Gray;
        var gridPen = new Pen(gridBrush, Math.Max(0.5, GridLineThickness));

        for (int index = 0; index < _yTickValues.Count; index++)
        {
            double pixelY = MapY(_yTickValues[index], _plotRect, _yScale);
            context.DrawLine(gridPen, new Point(_plotRect.Left, pixelY), new Point(_plotRect.Right, pixelY));
            if (index < _yTickLabels.Count)
            {
                DrawAxisLabel(context, _yTickLabels[index], _plotRect.Left - PlotLeftPad, pixelY, HorizontalAlignment.Right);
            }
        }

        for (int index = 0; index < _xTickValues.Count; index++)
        {
            double pixelX = MapX(_xTickValues[index], _plotRect, _xScale);
            context.DrawLine(gridPen, new Point(pixelX, _plotRect.Top), new Point(pixelX, _plotRect.Bottom));
            if (index < _xTickLabels.Count)
            {
                DrawAxisLabel(context, _xTickLabels[index], pixelX, _plotRect.Bottom + PlotBottomPad, HorizontalAlignment.Center);
            }
        }
    }

    private static void DrawAxisLabel(DrawingContext context, FormattedText label, double x, double y, HorizontalAlignment horizontalBasis)
    {
        double originX = horizontalBasis switch
        {
            HorizontalAlignment.Right => x - label.Width,
            HorizontalAlignment.Center => x - label.Width / 2.0,
            _ => x
        };
        context.DrawText(label, new Point(originX, y));
    }

    private void DrawSeries(DrawingContext context)
    {
        IBrush markerFill = ChartDrawing.ResolveSurfaceBrush(this);
        for (int index = 0; index < _renderSeries.Count; index++)
        {
            RenderSeries render = _renderSeries[index];

            if (render.IsBar)
            {
                DrawBars(context, render);
                continue;
            }

            if (render.IsScatter)
            {
                if (render.Markers != null)
                {
                    context.DrawGeometry(render.Stroke, null, render.Markers);
                }

                continue;
            }

            StreamGeometry? line = render.Line;
            StreamGeometry? area = render.Area;

            if (_inTransition)
            {
                var (prevLine, prevArea, _) = _transitionPrev != null && index < _transitionPrev.Count
                    ? _transitionPrev[index]
                    : ((Point[]?)null, (Point[]?)null, (Point[]?)null);
                line = BuildInterpolatedLine(prevLine, render.LinePoints, _transitionT) ?? render.Line;
                area = BuildInterpolatedArea(prevArea, render.AreaPoints, _transitionT, _plotRect.Bottom) ?? render.Area;
            }

            if (area != null && render.Fill != null)
            {
                context.DrawGeometry(render.Fill, null, area);
            }

            if (line != null)
            {
                var pen = render.LinePen ?? new Pen(render.Stroke, render.Thickness, null, PenLineCap.Round, PenLineJoin.Round);
                context.DrawGeometry(null, pen, line);
            }

            if (render.Markers != null)
            {
                var markerPen = render.MarkerPen ?? new Pen(render.Stroke, Math.Max(1.0, render.Thickness));
                if (_inTransition)
                {
                    var (_, _, prevRaw) = _transitionPrev != null && index < _transitionPrev.Count
                        ? _transitionPrev[index]
                        : ((Point[]?)null, (Point[]?)null, (Point[]?)null);
                    Point[]? morphed = MorphPoints(prevRaw, render.RawPoints, _transitionT);
                    if (morphed != null)
                    {
                        foreach (Point point in morphed)
                        {
                            context.DrawEllipse(markerFill, markerPen, point, 2.5, 2.5);
                        }
                    }
                }
                else
                {
                    context.DrawGeometry(markerFill, markerPen, render.Markers);
                }
            }
        }
    }

    private void DrawBars(DrawingContext context, RenderSeries render)
    {
        IBrush fill = render.Fill ?? render.Stroke;
        var strokePen = render.BarPen ?? new Pen(render.Stroke, Math.Min(1.5, Math.Max(0.5, render.Thickness)));
        double radius = render.BarCornerRadius;
        foreach (Rect rect in render.BarRects)
        {
            if (radius > 0.0)
            {
                context.DrawRectangle(fill, strokePen, rect, radius, radius);
            }
            else
            {
                context.DrawRectangle(fill, strokePen, rect);
            }
        }
    }

    private void DrawLegend(DrawingContext context)
    {
        ChartDrawing.DrawLegend(context, _cachedLegendItems, _cachedLegendContentWidth, LegendPosition, Bounds.Size);
    }

    private void DrawOverlay(DrawingContext context, Point hover)
    {
        if (!EnableCrosshair)
        {
            return;
        }

        _overlayMatches.Clear();
        _barHighlights.Clear();
        _scatterHighlights.Clear();
        double threshold = Math.Max(1.0, HoverThreshold);

        // Pass 1: a scatter dot under the cursor is the most precise anchor. It decides the x
        // position that the crosshair, the markers and the tooltip all have to share.
        RenderSeries? anchorSeries = null;
        int anchorIndex = -1;
        double bestScatterDistance = double.PositiveInfinity;
        foreach (RenderSeries render in _renderSeries)
        {
            if (!render.IsScatter)
            {
                continue;
            }

            int nearest = FindNearestScatter(render, hover);
            if (nearest < 0)
            {
                continue;
            }

            double distance = Math.Sqrt(Squared(hover.X - render.Px[nearest], hover.Y - render.Py[nearest]));
            if (distance <= threshold && distance < bestScatterDistance)
            {
                bestScatterDistance = distance;
                anchorSeries = render;
                anchorIndex = nearest;
            }
        }

        bool anchored = anchorSeries != null;
        double anchorX = anchored ? anchorSeries!.Px[anchorIndex] : 0.0;
        double anchorValueX = anchored ? anchorSeries!.Values[anchorIndex].X : 0.0;

        double bestLineDistance = double.PositiveInfinity;
        Point? lineTrackPixel = null;

        foreach (RenderSeries render in _renderSeries)
        {
            if (render.IsBar)
            {
                int barIndex = FindBarAt(render, hover);
                if (barIndex >= 0)
                {
                    var pixel = new Point(render.Px[barIndex], Math.Min(render.Py[barIndex], _plotRect.Bottom));
                    _overlayMatches.Add(new OverlayMatch(render, render.Values[barIndex], pixel));
                    _barHighlights.Add(new BarHighlight(render, barIndex));
                }

                continue;
            }

            if (render.IsScatter)
            {
                if (!ReferenceEquals(render, anchorSeries))
                {
                    continue;
                }

                var pixel = new Point(anchorX, render.Py[anchorIndex]);
                _overlayMatches.Add(new OverlayMatch(render, render.Values[anchorIndex], pixel));
                _scatterHighlights.Add(new OverlayMatch(render, render.Values[anchorIndex], pixel));
                continue;
            }

            if (anchored)
            {
                // The dot fixed the x position, so sample the polyline that was actually drawn.
                // Snapping to the nearest vertex here would put the marker half a category away
                // from the crosshair - the very mismatch this replaces.
                if (TrySampleAt(render, anchorX, anchorValueX, out LuminaChartPoint? sample, out Point samplePixel) &&
                    sample != null)
                {
                    _overlayMatches.Add(new OverlayMatch(render, sample, samplePixel));
                }

                continue;
            }

            int lineIndex = FindNearestIndex(render, hover.X);
            if (lineIndex < 0)
            {
                continue;
            }

            double lineDistance = Math.Abs(render.Px[lineIndex] - hover.X);
            if (lineDistance > threshold)
            {
                continue;
            }

            var linePixel = new Point(render.Px[lineIndex], render.Py[lineIndex]);
            _overlayMatches.Add(new OverlayMatch(render, render.Values[lineIndex], linePixel));
            if (lineDistance < bestLineDistance)
            {
                bestLineDistance = lineDistance;
                lineTrackPixel = linePixel;
            }
        }

        double? crosshairX = anchored ? anchorX : lineTrackPixel?.X;
        if (crosshairX is { } crosshair)
        {
            IBrush brush = CrosshairBrush ?? Brushes.Gray;
            if (_cachedCrosshairPen == null || !Equals(_cachedCrosshairPen.Brush, brush))
            {
                _cachedCrosshairPen = new Pen(brush, 1.0)
                {
                    DashStyle = CrosshairDashStyle
                };
            }

            context.DrawLine(_cachedCrosshairPen, new Point(crosshair, _plotRect.Top), new Point(crosshair, _plotRect.Bottom));
        }

        foreach (OverlayMatch match in _scatterHighlights)
        {
            double radius = match.Series.MarkerRadius + 2.5;
            var ringPen = match.Series.HighlightRingPen ?? new Pen(match.Series.Stroke, 1.5);
            context.DrawEllipse(null, ringPen, match.Pixel, radius, radius);
        }

        foreach (BarHighlight highlight in _barHighlights)
        {
            Rect rect = highlight.Series.BarRects[highlight.Index];
            var outlinePen = new Pen(ChartDrawing.ContrastFor(highlight.Series.Fill ?? highlight.Series.Stroke), 1.5);
            context.DrawRectangle(null, outlinePen, rect);
        }

        IBrush hoverFill = ChartDrawing.ResolveSurfaceBrush(this);
        foreach (OverlayMatch match in _overlayMatches)
        {
            if (match.Series.IsBar || match.Series.IsScatter)
            {
                continue;
            }

            var markerPen = match.Series.HighlightMarkerPen ?? new Pen(match.Series.Stroke, 2.0);
            context.DrawEllipse(hoverFill, markerPen, match.Pixel, 3.5, 3.5);
        }

        DrawTooltip(context, hover);
    }

    private int FindBarAt(RenderSeries render, Point hover)
    {
        Rect[] rects = render.BarRects;
        for (int index = 0; index < rects.Length; index++)
        {
            Rect rect = rects[index];
            if (rect.Contains(hover))
            {
                return index;
            }
        }

        return -1;
    }

    private int FindNearestScatter(RenderSeries render, Point hover)
    {
        if (render.Px.Length == 0)
        {
            return -1;
        }

        int best = -1;
        double bestSquared = double.PositiveInfinity;
        for (int index = 0; index < render.Px.Length; index++)
        {
            double dx = hover.X - render.Px[index];
            double dy = hover.Y - render.Py[index];
            double squared = dx * dx + dy * dy;
            if (squared < bestSquared)
            {
                bestSquared = squared;
                best = index;
            }
        }

        return best;
    }

    private static double Squared(double a, double b)
    {
        return a * a + b * b;
    }

    private static int FindNearestIndex(RenderSeries render, double target)
    {
        double[] px = render.Px;
        if (px.Length == 0)
        {
            return -1;
        }

        int best = -1;
        double bestDistance = double.PositiveInfinity;
        if (render.Sorted)
        {
            int location = BinarySearchInsertion(px, 0, px.Length - 1, target);
            for (int i = Math.Max(0, location - 1); i <= Math.Min(px.Length - 1, location + 1); i++)
            {
                double distance = Math.Abs(px[i] - target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
        }
        else
        {
            int location = BinarySearchInsertion(render.Order!, render.Px, target);
            for (int i = Math.Max(0, location - 1); i <= Math.Min(render.Order!.Length - 1, location + 1); i++)
            {
                int direct = render.Order[i];
                double distance = Math.Abs(px[direct] - target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = direct;
                }
            }
        }

        return best;
    }

    private static int BinarySearchInsertion(double[] px, int lo, int hi, double target)
    {
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (px[mid] == target)
            {
                return mid;
            }

            if (px[mid] < target)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    private static int BinarySearchInsertion(int[] order, double[] px, double target)
    {
        int lo = 0;
        int hi = order.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            double value = px[order[mid]];
            if (value == target)
            {
                return mid;
            }

            if (value < target)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    private void DrawTooltip(DrawingContext context, Point hover)
    {
        if (_overlayMatches.Count == 0)
        {
            _cachedTooltipMatches.Clear();
            _cachedTooltipLines.Clear();
            return;
        }

        IBrush background = TooltipBackgroundBrush ?? Brushes.SlateGray;
        IBrush textBrush = TooltipTextBrush ?? Brushes.White;
        double fontSize = AxisLabelFontSize;

        bool matchesChanged = _cachedTooltipMatches.Count != _overlayMatches.Count;
        if (!matchesChanged)
        {
            for (int i = 0; i < _overlayMatches.Count; i++)
            {
                var cached = _cachedTooltipMatches[i];
                var current = _overlayMatches[i];
                if (!ReferenceEquals(cached.Series, current.Series.Source) ||
                    cached.X != current.Value.X ||
                    cached.Y != current.Value.Y)
                {
                    matchesChanged = true;
                    break;
                }
            }
        }

        if (matchesChanged)
        {
            _cachedTooltipMatches.Clear();
            _cachedTooltipLines.Clear();
            foreach (OverlayMatch match in _overlayMatches)
            {
                _cachedTooltipMatches.Add((match.Series.Source, match.Value.X, match.Value.Y));
                string title = string.IsNullOrEmpty(match.Series.Source.Title) ? "Values" : match.Series.Source.Title;
                string text = $"{title}: ({FormatValue(match.Value.X)}, {FormatValue(match.Value.Y)})";
                _cachedTooltipLines.Add(ChartDrawing.CreateText(text, _labelTypeface, textBrush, fontSize));
            }

            _cachedTooltipSize = ChartDrawing.MeasureTooltip(_cachedTooltipLines, TooltipPadding);
        }

        Rect? legendRect = ShowLegend && _cachedLegendItems.Count > 0
            ? ChartDrawing.ComputeLegendRect(_cachedLegendItems, _cachedLegendContentWidth, LegendPosition, Bounds.Size)
            : null;

        Point origin = ChartDrawing.PlaceTooltip(
            _cachedTooltipSize,
            new Point(hover.X + 14.0, hover.Y - _cachedTooltipSize.Height - 8.0),
            Bounds.Size,
            legendRect);

        ChartDrawing.DrawTooltip(
            context,
            _cachedTooltipLines,
            background,
            ChartDrawing.ResolveTooltipBorderPen(this),
            TooltipPadding,
            TooltipCornerRadius,
            origin,
            _cachedTooltipSize);
    }

    private static string FormatTick(double value, double step)
    {
        int decimals = step > 0.0 && step < 1.0
            ? Math.Min(3, (int)Math.Ceiling(-Math.Log10(step)))
            : 0;
        string format = decimals > 0 ? "0." + new string('0', decimals) : "0";
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    private static string FormatValue(double value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static double MapX(double value, Rect plot, AxisScale scale)
    {
        double ratio = (value - scale.Min) / (scale.Max - scale.Min);
        return plot.Left + ratio * plot.Width;
    }

    private static double MapY(double value, Rect plot, AxisScale scale)
    {
        double ratio = (value - scale.Min) / (scale.Max - scale.Min);
        return plot.Bottom - ratio * plot.Height;
    }

    private static double UnmapY(double pixel, Rect plot, AxisScale scale)
    {
        double ratio = (plot.Bottom - pixel) / plot.Height;
        return scale.Min + ratio * (scale.Max - scale.Min);
    }

    /// <summary>
    /// Samples a line-like series at pixel <paramref name="x"/> using the polyline that was actually
    /// drawn. Interpolating the raw data points instead would drift off spline segments, because a
    /// spline bulges between its samples.
    /// </summary>
    private bool TrySampleAt(RenderSeries render, double x, double valueX, out LuminaChartPoint? value, out Point pixel)
    {
        value = default;
        pixel = default;
        Point[]? line = render.LinePoints;
        if (line == null || line.Length < 2)
        {
            return false;
        }

        if (render.Sorted)
        {
            if (x < line[0].X || x > line[^1].X)
            {
                return false;
            }

            int low = 0;
            int high = line.Length - 1;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (line[middle].X <= x)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return Interpolate(line[low], line[high], x, valueX, out value, out pixel);
        }

        for (int index = 1; index < line.Length; index++)
        {
            if (Interpolate(line[index - 1], line[index], x, valueX, out value, out pixel))
            {
                return true;
            }
        }

        return false;
    }

    private bool Interpolate(Point from, Point to, double x, double valueX, out LuminaChartPoint? value, out Point pixel)
    {
        value = default;
        pixel = default;
        if (x < Math.Min(from.X, to.X) || x > Math.Max(from.X, to.X))
        {
            return false;
        }

        double span = to.X - from.X;
        double ratio = span == 0.0 ? 0.0 : (x - from.X) / span;
        double y = from.Y + ((to.Y - from.Y) * ratio);
        pixel = new Point(x, y);
        value = new LuminaChartPoint(valueX, UnmapY(y, _plotRect, _yScale));
        return true;
    }

    private sealed class RenderSeries
    {
        public required LuminaChartSeries Source;
        public IBrush Stroke = Brushes.Gray;
        public IBrush? Fill;
        public double Thickness = 2.0;
        public bool Decimated;
        public double[] Px = [];
        public double[] Py = [];
        public LuminaChartPoint[] Values = [];
        public int[]? Order;
        public bool Sorted = true;
        public Point[]? RawPoints;
        public Point[]? LinePoints;
        public Point[]? AreaPoints;
        public StreamGeometry? Line;
        public StreamGeometry? Area;
        public StreamGeometry? Markers;
        public bool IsBar;
        public Rect[] BarRects = [];
        public double BarCornerRadius;
        public bool IsScatter;
        public double MarkerRadius = 2.5;
        public Pen? LinePen;
        public Pen? MarkerPen;
        public Pen? BarPen;
        public Pen? HighlightRingPen;
        public Pen? HighlightMarkerPen;
    }

    private readonly record struct OverlayMatch(RenderSeries Series, LuminaChartPoint Value, Point Pixel);

    private readonly record struct BarHighlight(RenderSeries Series, int Index);

    private readonly record struct AxisScale(double Min, double Max, double Step);
}