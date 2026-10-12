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
    private const double LegendBandHeight = 20.0;
    private const double LegendBandGap = 6.0;

    private static readonly IReadOnlyList<Color> DefaultPalette =
    [
        Color.FromRgb(0x25, 0x63, 0xEB),
        Color.FromRgb(0x14, 0xB8, 0xA6),
        Color.FromRgb(0xF5, 0x9E, 0x0B),
        Color.FromRgb(0xEC, 0x48, 0x99),
        Color.FromRgb(0x8B, 0x5C, 0xF6),
        Color.FromRgb(0x22, 0xC5, 0x5E),
        Color.FromRgb(0xF9, 0x73, 0x16),
        Color.FromRgb(0x06, 0xB6, 0xD4)
    ];

    private readonly List<RenderSeries> _renderSeries = new();
    private readonly List<double> _xTickValues = new();
    private readonly List<double> _yTickValues = new();
    private readonly List<FormattedText> _xTickLabels = new();
    private readonly List<FormattedText> _yTickLabels = new();
    private readonly List<OverlayMatch> _overlayMatches = new();
    private readonly List<BarHighlight> _barHighlights = new();
    private readonly List<OverlayMatch> _scatterHighlights = new();
    private readonly Stopwatch _stopwatch = new();

    private LuminaChartAxis _xAxis = new();
    private LuminaChartAxis _yAxis = new();

    private INotifyCollectionChanged? _seriesSubscription;
    private HashSet<INotifyCollectionChanged> _pointsSubscriptions = new();
    private DispatcherTimer? _transitionTimer;
    private readonly Stopwatch _transitionSw = new();
    private List<(Point[]? Line, Point[]? Area)>? _transitionPrev;

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
        ResourcesChanged += OnResourcesChanged;
        _labelTypeface = new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Normal);
        _xAxis.PropertyChanged += OnAxisChanged;
        _yAxis.PropertyChanged += OnAxisChanged;
    }

    private void OnAxisChanged(object? sender, PropertyChangedEventArgs e)
    {
        _isDirty = true;
        InvalidateVisual();
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
        get => _xAxis;
        set
        {
            if (value == null || ReferenceEquals(_xAxis, value))
            {
                return;
            }

            _xAxis.PropertyChanged -= OnAxisChanged;
            _xAxis = value;
            _xAxis.PropertyChanged += OnAxisChanged;
            _isDirty = true;
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets the Y axis.</summary>
    public LuminaChartAxis YAxis
    {
        get => _yAxis;
        set
        {
            if (value == null || ReferenceEquals(_yAxis, value))
            {
                return;
            }

            _yAxis.PropertyChanged -= OnAxisChanged;
            _yAxis = value;
            _yAxis.PropertyChanged += OnAxisChanged;
            _isDirty = true;
            InvalidateVisual();
        }
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
        _stopwatch.Restart();

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
                _lastRenderMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
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

        DrawStaticLayer(context);

        if (_hoverPosition is { } hover)
        {
            DrawOverlay(context, hover);
        }

        if (ShowLegend && _renderSeries.Count > 0)
        {
            DrawLegend(context);
        }

        _stopwatch.Stop();
        _lastRenderMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
        if (!didRebuild)
        {
            _lastMoveRenderMilliseconds = _lastRenderMilliseconds;
        }
        }
        catch (Exception exception)
        {
            _isDirty = true;
            System.Diagnostics.Trace.TraceError("LuminaUI.Charts render failed: {0}", exception);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _hoverPosition = e.GetPosition(this);
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoverPosition != null)
        {
            _hoverPosition = null;
            InvalidateVisual();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeriesProperty)
        {
            UpdateSeriesSubscription(change.GetOldValue<IList<LuminaChartSeries>?>(), change.GetNewValue<IList<LuminaChartSeries>?>());
            _rebuildDueToDataChange = true;
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

            InvalidateVisual();
        }
        else if (RebuildProperties.Contains(change.Property))
        {
            _isDirty = true;
            InvalidateVisual();
        }
        else if (InvalidateOnlyProperties.Contains(change.Property))
        {
            InvalidateVisual();
        }
    }

    private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e)
    {
        _isDirty = true;
        InvalidateVisual();
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _rebuildDueToDataChange = true;
        _isDirty = true;
        InvalidateVisual();
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _rebuildDueToDataChange = true;
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

        SyncPointsSubscriptions();
    }

    private void SyncPointsSubscriptions()
    {
        var desired = new HashSet<INotifyCollectionChanged>();
        IList<LuminaChartSeries>? series = Series;
        if (series != null)
        {
            foreach (LuminaChartSeries item in series)
            {
                if (item?.Points is INotifyCollectionChanged points)
                {
                    desired.Add(points);
                }
            }
        }

        foreach (INotifyCollectionChanged stale in _pointsSubscriptions)
        {
            if (!desired.Contains(stale))
            {
                stale.CollectionChanged -= OnPointsCollectionChanged;
            }
        }

        foreach (INotifyCollectionChanged add in desired)
        {
            if (!_pointsSubscriptions.Contains(add))
            {
                add.CollectionChanged += OnPointsCollectionChanged;
            }
        }

        _pointsSubscriptions = desired;
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

    private List<(Point[]? Line, Point[]? Area)> CaptureTransitionPoints()
    {
        var snapshot = new List<(Point[]? Line, Point[]? Area)>(_renderSeries.Count);
        bool morphing = _inTransition && _transitionPrev != null && _transitionT > 0.0 && _transitionT < 1.0;
        for (int index = 0; index < _renderSeries.Count; index++)
        {
            RenderSeries render = _renderSeries[index];
            if (morphing && index < _transitionPrev!.Count)
            {
                var (prevLine, prevArea) = _transitionPrev[index];
                snapshot.Add((
                    MorphPoints(prevLine, render.LinePoints, _transitionT),
                    MorphPoints(prevArea, render.AreaPoints, _transitionT)));
            }
            else
            {
                snapshot.Add((render.LinePoints, render.AreaPoints));
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
        _stopwatch.Reset();
        _stopwatch.Start();
        try
        {
            RebuildCore();
        }
        finally
        {
            _stopwatch.Stop();
            _lastBuildMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
        }
    }

    private void RebuildCore()
    {
        _renderSeries.Clear();
        _xTickValues.Clear();
        _yTickValues.Clear();
        _xTickLabels.Clear();
        _yTickLabels.Clear();

        SyncPointsSubscriptions();

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
    }

    private void BuildTickLabels()
    {
        _labelTypeface = new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Normal);
        IBrush labelBrush = AxisLabelBrush ?? Brushes.Gray;
        double fontSize = AxisLabelFontSize;

        for (double y = TickStart(_yScale.Min, _yScale.Step); y <= _yScale.Max + 1e-9; y += _yScale.Step)
        {
            _yTickValues.Add(y);
            if (ShowAxisLabels)
            {
                string text = _yAxis.LabelFormatter?.Invoke(y) ?? FormatTick(y, _yScale.Step);
                _yTickLabels.Add(CreateFormattedText(text, _labelTypeface, labelBrush, fontSize));
            }
        }

        for (double x = TickStart(_xScale.Min, _xScale.Step); x <= _xScale.Max + 1e-9; x += _xScale.Step)
        {
            _xTickValues.Add(x);
            if (ShowAxisLabels)
            {
                string text = _xAxis.Kind == LuminaChartAxisKind.Category
                    ? FormatCategoryLabel((int)Math.Round(x))
                    : _xAxis.LabelFormatter?.Invoke(x) ?? FormatTick(x, _xScale.Step);
                _xTickLabels.Add(CreateFormattedText(text, _labelTypeface, labelBrush, fontSize));
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
        render.Stroke = source.Stroke ?? ResolvePaletteBrush(paletteIndex);
        render.Fill = source.Fill ?? (source.ShowArea ? CreateTranslucent(render.Stroke) : null);
        render.Thickness = Math.Max(1.0, source.StrokeThickness);

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
        }
        else
        {
            render.LinePoints = ToPointArray(render.Px, render.Py);
            render.AreaPoints = source.ShowArea ? render.LinePoints : null;
        }

        Point[]? markerPoints = render.LinePoints;
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
        render.Line = BuildLineGeometry(splinePoints);
        if (series.ShowArea)
        {
            render.Area = BuildAreaGeometry(splinePoints, _plotRect.Bottom);
        }

        if (series.ShowMarkers && render.Px.Length <= SubsetMarkersLimit)
        {
            render.Markers = BuildMarkerGeometry(ToPointArray(render.Px, render.Py), 2.5);
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
        bool categoryX = _xAxis.Kind == LuminaChartAxisKind.Category;
        var px = new List<double>(Math.Min(source.Points!.Count, 4096));
        var py = new List<double>(px.Capacity);
        var values = new List<LuminaChartPoint>(px.Capacity);
        bool sorted = true;
        double previous = double.NegativeInfinity;
        int pointIndex = 0;

        foreach (LuminaChartPoint point in source.Points)
        {
            if (point.IsEmpty)
            {
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
            int bucket = (int)Math.Clamp(Math.Round(render.Px[index]), 0.0, bucketCount - 1);
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
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
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
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
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
        int maxCategories = 0;

        foreach (LuminaChartSeries item in series)
        {
            int categoryCount = 0;
            foreach (LuminaChartPoint point in item.Points!)
            {
                if (point.IsEmpty)
                {
                    continue;
                }

                categoryCount++;
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

            maxCategories = Math.Max(maxCategories, categoryCount);
        }

        if (_xAxis.Kind == LuminaChartAxisKind.Category)
        {
            xMin = 0.0;
            xMax = Math.Max(xMax, maxCategories - 1);
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

        double xStep = _xAxis.TickStep ?? GetNiceStep(xMin, xMax, maxXTicks);
        double yStep = _yAxis.TickStep ?? GetNiceStep(yMin, yMax, maxYTicks);
        return (new AxisScale(xMin, xMax, xStep), new AxisScale(yMin, yMax, yStep));
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

        using (context.PushClip(_plotRect))
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
                var (prevLine, prevArea) = _transitionPrev != null && index < _transitionPrev.Count
                    ? _transitionPrev[index]
                    : ((Point[]?)null, (Point[]?)null);
                line = BuildInterpolatedLine(prevLine, render.LinePoints, _transitionT) ?? render.Line;
                area = BuildInterpolatedArea(prevArea, render.AreaPoints, _transitionT, _plotRect.Bottom) ?? render.Area;
            }

            if (area != null && render.Fill != null)
            {
                context.DrawGeometry(render.Fill, null, area);
            }

            if (line != null)
            {
                var pen = new Pen(render.Stroke, render.Thickness, null, PenLineCap.Round, PenLineJoin.Round);
                context.DrawGeometry(null, pen, line);
            }

            if (render.Markers != null)
            {
                var markerPen = new Pen(render.Stroke, Math.Max(1.0, render.Thickness));
                if (_inTransition)
                {
                    var (prevLine, _) = _transitionPrev != null && index < _transitionPrev.Count
                        ? _transitionPrev[index]
                        : ((Point[]?)null, (Point[]?)null);
                    Point[]? morphed = MorphPoints(prevLine, render.LinePoints, _transitionT);
                    if (morphed != null)
                    {
                        foreach (Point point in morphed)
                        {
                            context.DrawEllipse(Brushes.White, markerPen, point, 2.5, 2.5);
                        }
                    }
                }
                else
                {
                    context.DrawGeometry(Brushes.White, markerPen, render.Markers);
                }
            }
        }
    }

    private void DrawBars(DrawingContext context, RenderSeries render)
    {
        IBrush fill = render.Fill ?? render.Stroke;
        var strokePen = new Pen(render.Stroke, Math.Min(1.5, Math.Max(0.5, render.Thickness)));
        foreach (Rect rect in render.BarRects)
        {
            context.DrawRectangle(fill, strokePen, rect);
        }
    }

    private void DrawLegend(DrawingContext context)
    {
        IBrush textBrush = LegendTextBrush ?? AxisLabelBrush ?? Brushes.Gray;
        const double swatchWidth = 12.0;
        const double swatchHeight = 3.0;
        const double swatchGap = 6.0;
        const double itemGap = 14.0;
        const double fontSize = 12.0;
        var typeface = _labelTypeface;

        var items = new List<(FormattedText Text, IBrush Color)>(_renderSeries.Count);
        double contentWidth = 0.0;
        for (int index = 0; index < _renderSeries.Count; index++)
        {
            RenderSeries render = _renderSeries[index];
            string title = string.IsNullOrEmpty(render.Source.Title)
                ? $"S{index + 1}"
                : render.Source.Title!;
            var text = CreateFormattedText(title, typeface, textBrush, fontSize);
            items.Add((text, render.Stroke));
            if (index > 0)
            {
                contentWidth += itemGap;
            }

            contentWidth += swatchWidth + swatchGap + text.Width;
        }

        if (contentWidth > Bounds.Width - 8.0)
        {
            contentWidth = Bounds.Width - 8.0;
        }

        double x;
        switch (LegendPosition)
        {
            case LuminaChartLegendPosition.TopLeft:
            case LuminaChartLegendPosition.BottomLeft:
                x = 4.0;
                break;
            case LuminaChartLegendPosition.TopCenter:
            case LuminaChartLegendPosition.BottomCenter:
                x = Math.Max(0.0, (Bounds.Width - contentWidth) / 2.0);
                break;
            default:
                x = Math.Max(0.0, Bounds.Width - contentWidth - 4.0);
                break;
        }

        bool isTop = LegendPosition is LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.TopRight;
        double cursorY = isTop
            ? 2.0 + (LegendBandHeight - fontSize) / 2.0
            : Bounds.Height - LegendBandHeight - 2.0 + (LegendBandHeight - fontSize) / 2.0;

        double cursorX = x;
        foreach ((FormattedText Text, IBrush Color) item in items)
        {
            var swatchRect = new Rect(cursorX, cursorY + (fontSize - swatchHeight) / 2.0, swatchWidth, swatchHeight);
            context.DrawRectangle(item.Color, null, swatchRect, 1.5, 1.5);
            cursorX += swatchWidth + swatchGap;
            context.DrawText(item.Text, new Point(cursorX, cursorY));
            cursorX += item.Text.Width + itemGap;
        }
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
        double bestLineDistance = double.PositiveInfinity;
        Point? trackPixel = null;

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
                int nearest = FindNearestScatter(render, hover);
                if (nearest >= 0)
                {
                    double distance = Math.Sqrt(Squared(hover.X - render.Px[nearest], hover.Y - render.Py[nearest]));
                    if (distance <= threshold)
                    {
                        var pixel = new Point(render.Px[nearest], render.Py[nearest]);
                        _overlayMatches.Add(new OverlayMatch(render, render.Values[nearest], pixel));
                        _scatterHighlights.Add(new OverlayMatch(render, render.Values[nearest], pixel));
                    }
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
                trackPixel = linePixel;
            }
        }

        if (trackPixel != null)
        {
            var crosshairPen = new Pen(CrosshairBrush ?? Brushes.Gray, 1.0)
            {
                DashStyle = new DashStyle([4.0, 3.0], 0.0)
            };
            context.DrawLine(crosshairPen, new Point(trackPixel.Value.X, _plotRect.Top), new Point(trackPixel.Value.X, _plotRect.Bottom));
        }

        foreach (OverlayMatch match in _scatterHighlights)
        {
            double radius = match.Series.MarkerRadius + 2.5;
            var ringPen = new Pen(match.Series.Stroke, 1.5);
            context.DrawEllipse(null, ringPen, match.Pixel, radius, radius);
        }

        foreach (BarHighlight highlight in _barHighlights)
        {
            Rect rect = highlight.Series.BarRects[highlight.Index];
            var outlinePen = new Pen(Brushes.White, 2.0);
            context.DrawRectangle(null, outlinePen, rect);
        }

        foreach (OverlayMatch match in _overlayMatches)
        {
            if (match.Series.IsBar || match.Series.IsScatter)
            {
                continue;
            }

            var markerPen = new Pen(match.Series.Stroke, 2.0);
            context.DrawEllipse(Brushes.White, markerPen, match.Pixel, 3.5, 3.5);
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
            return;
        }

        IBrush background = TooltipBackgroundBrush ?? Brushes.SlateGray;
        IBrush textBrush = TooltipTextBrush ?? Brushes.White;
        double fontSize = AxisLabelFontSize;

        var lines = new List<FormattedText>(_overlayMatches.Count);
        double width = 0.0;
        foreach (OverlayMatch match in _overlayMatches)
        {
            string title = string.IsNullOrEmpty(match.Series.Source.Title) ? "Values" : match.Series.Source.Title;
            string text = $"{title}: ({FormatValue(match.Value.X)}, {FormatValue(match.Value.Y)})";
            FormattedText line = CreateFormattedText(text, _labelTypeface, textBrush, fontSize);
            width = Math.Max(width, line.Width);
            lines.Add(line);
        }

        double height = 0.0;
        foreach (FormattedText line in lines)
        {
            height += line.Height;
        }

        double boxWidth = width + TooltipPadding * 2.0;
        double boxHeight = height + TooltipPadding * 2.0;
        double x = hover.X + 14.0;
        double y = hover.Y - boxHeight - 8.0;
        x = Math.Clamp(x, 0.0, Math.Max(0.0, Bounds.Width - boxWidth));
        y = Math.Clamp(y, 0.0, Math.Max(0.0, Bounds.Height - boxHeight));

        var boxRect = new Rect(x, y, boxWidth, boxHeight);
        context.DrawRectangle(background, null, boxRect, TooltipCornerRadius, TooltipCornerRadius);

        double lineY = y + TooltipPadding;
        foreach (FormattedText line in lines)
        {
            context.DrawText(line, new Point(x + TooltipPadding, lineY));
            lineY += line.Height;
        }
    }

    private IBrush ResolvePaletteBrush(int index)
    {
        string key = "LuminaChartColor" + ((index % DefaultPalette.Count) + 1);
        if (this.TryFindResource(key, out object? value) && value is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(DefaultPalette[index % DefaultPalette.Count]);
    }

    private static IBrush CreateTranslucent(IBrush source)
    {
        if (source is ISolidColorBrush solid && solid.Color.A > 0)
        {
            return new SolidColorBrush(Color.FromArgb((byte)(solid.Color.A * 0.22), solid.Color.R, solid.Color.G, solid.Color.B));
        }

        if (source is ISolidColorBrush fallback)
        {
            return fallback;
        }

        return Brushes.Gray;
    }

    private static FormattedText CreateFormattedText(string text, Typeface typeface, IBrush brush, double fontSize)
    {
        return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, brush);
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
        public Point[]? LinePoints;
        public Point[]? AreaPoints;
        public StreamGeometry? Line;
        public StreamGeometry? Area;
        public StreamGeometry? Markers;
        public bool IsBar;
        public Rect[] BarRects = [];
        public bool IsScatter;
        public double MarkerRadius = 2.5;
    }

    private readonly record struct OverlayMatch(RenderSeries Series, LuminaChartPoint Value, Point Pixel);

    private readonly record struct BarHighlight(RenderSeries Series, int Index);

    private readonly record struct AxisScale(double Min, double Max, double Step);
}