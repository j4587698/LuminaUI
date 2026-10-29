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
    private const int MaxTickCount = 200;
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
    private double _cachedLegendHeight;
    private double _areaBaseline;
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
    private IPen? _cachedTooltipBorderPen;
    private IBrush? _cachedSurfaceBrush;
    private readonly List<FormattedText> _cachedTooltipLines = new();
    private readonly List<(LuminaChartSeries Series, double X, double Y)> _cachedTooltipMatches = new();
    private Size _cachedTooltipSize;

    private LuminaChartAxis _xAxis = new();
    private LuminaChartAxis _yAxis = new();

    private INotifyCollectionChanged? _seriesSubscription;
    private INotifyCollectionChanged? _categoriesSubscription;
    private HashSet<INotifyCollectionChanged> _pointsSubscriptions = new();
    private HashSet<LuminaChartSeries> _seriesItemSubscriptions = new();
    private HashSet<INotifyCollectionChanged> _spareDesiredPoints = new();
    private HashSet<LuminaChartSeries> _spareDesiredSeries = new();
    private DispatcherTimer? _transitionTimer;
    private readonly Stopwatch _transitionSw = new();
    private List<(Point[]? Line, Point[]? Area, Point[]? Raw)>? _transitionPrev;

    private Typeface _labelTypeface;
    private AxisScale _xScale;
    private AxisScale _yScale;
    private Rect _plotRect;
    private Point? _hoverPosition;
    private bool _isAttached;
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
        // Axis labels and legend entries are pre-built FormattedText instances, so anything that
        // feeds them (visibility, brush, font) needs a rebuild rather than a plain repaint.
        ShowAxisLabelsProperty,
        LegendTextBrushProperty,
        TextElement.FontFamilyProperty,
        DecimationThresholdProperty,
        ShowLegendProperty,
        LegendPositionProperty,
        Visual.BoundsProperty
    };

    private static readonly HashSet<AvaloniaProperty> InvalidateOnlyProperties = new()
    {
        ShowGridLinesProperty,
        GridLineThicknessProperty,
        GridLineBrushProperty,
        EnableCrosshairProperty,
        CrosshairBrushProperty,
        TooltipBackgroundBrushProperty,
        TooltipTextBrushProperty,
        HoverThresholdProperty,
        TransitionDurationProperty
    };

    public LuminaLineChart()
    {
        ClipToBounds = true;
        SetValue(SeriesProperty, new List<LuminaChartSeries>());
        SetValue(XAxisProperty, _xAxis);
        SetValue(YAxisProperty, _yAxis);
        // Axis, series and point subscriptions are only held while the chart is attached to a
        // visual tree (see OnAttachedToVisualTree); a detached chart must not be kept alive by
        // long-lived view models.
        ResourcesChanged += OnResourcesChanged;
        // A theme variant switch does not raise ResourcesChanged, but palette colors, translucent
        // area fills and label brushes are all resolved from theme resources.
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
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
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnAxisChanged(sender, e));
            return;
        }

        if (e.PropertyName is null or nameof(LuminaChartAxis.Categories))
        {
            SyncCategorySubscription();
        }

        _isDirty = true;
        InvalidateTooltipCache();
        InvalidateVisual();
    }

    /// <summary>
    /// Follows an observable <see cref="LuminaChartAxis.Categories"/> list on the X axis, so
    /// adding or renaming categories in place relabels the axis.
    /// </summary>
    private void SyncCategorySubscription()
    {
        INotifyCollectionChanged? desired = _isAttached ? _xAxis.Categories as INotifyCollectionChanged : null;
        if (ReferenceEquals(desired, _categoriesSubscription))
        {
            return;
        }

        if (_categoriesSubscription != null)
        {
            _categoriesSubscription.CollectionChanged -= OnCategoriesChanged;
        }

        _categoriesSubscription = desired;
        if (_categoriesSubscription != null)
        {
            _categoriesSubscription.CollectionChanged += OnCategoriesChanged;
        }
    }

    private void OnCategoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnCategoriesChanged(sender, e));
            return;
        }

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

    /// <summary>
    /// Gets or sets the series to draw. Observable lists (and observable <c>Points</c>
    /// collections) are tracked while the chart is in the visual tree.
    /// </summary>
    /// <remarks>
    /// Mutate the series, their points and the axes on the UI thread. Change notifications raised
    /// on another thread are marshalled to the UI thread, but the collections themselves are read
    /// during rendering, so modifying them concurrently from a worker thread can still tear a
    /// frame. Batch updates off-thread and apply them through <c>Dispatcher.UIThread.Post</c>.
    /// </remarks>
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

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) =>
        ChartDrawing.MeasureChart(availableSize, 320.0, 200.0);

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
            // An invisible full-bounds background keeps the whole chart hit-testable, so the
            // crosshair tracks the pointer over empty plot space and not just over painted pixels.
            context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

            bool didRebuild = false;
            if (_isDirty)
            {
                didRebuild = true;
                bool wantTransition = _rebuildDueToDataChange && TransitionDuration > 0.0;
                _transitionPrev = wantTransition ? CaptureTransitionPoints() : null;

                Rebuild();
                if (_renderSeries.Count == 0)
                {
                    // Nothing to draw. Clear the dirty flag anyway so pointer moves over an empty
                    // chart do not re-scan every series on each frame; data/size changes set it again.
                    _isDirty = false;
                    _rebuildDueToDataChange = false;
                    _inTransition = false;
                    _transitionPrev = null;
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
        UpdateHover(e.GetPosition(this));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // Touch and pen produce no move events before the press, so a tap has to place the
        // crosshair by itself.
        if (e.Pointer.Type != PointerType.Mouse)
        {
            UpdateHover(e.GetPosition(this));
        }
    }

    private void UpdateHover(Point position)
    {
        if (!EnableCrosshair)
        {
            return;
        }

        double threshold = Math.Max(1.0, ChartDrawing.Finite(HoverThreshold, 28.0));
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
        // A lifted finger "exits" right after the tap; keep the tooltip until the next tap.
        if (e.Pointer.Type == PointerType.Touch)
        {
            return;
        }

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
        _isAttached = true;
        _xAxis.PropertyChanged -= OnAxisChanged;
        _xAxis.PropertyChanged += OnAxisChanged;
        _yAxis.PropertyChanged -= OnAxisChanged;
        _yAxis.PropertyChanged += OnAxisChanged;

        UpdateSeriesSubscription(Series);
        SyncCategorySubscription();
        _isDirty = true;
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttached = false;
        StopTransitionTimer();
        _inTransition = false;
        _transitionPrev = null;
        _hoverPosition = null;
        _cachedStaticDrawing = null;
        InvalidateTooltipCache();
        _cachedCrosshairPen = null;

        // Release every subscription so the series/axis objects (usually owned by a long-lived
        // view model) no longer reference this control. They are rebuilt on re-attach.
        UpdateSeriesSubscription(null);
        SyncCategorySubscription();
        _xAxis.PropertyChanged -= OnAxisChanged;
        _yAxis.PropertyChanged -= OnAxisChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeriesProperty)
        {
            UpdateSeriesSubscription(change.GetNewValue<IList<LuminaChartSeries>?>());
            _rebuildDueToDataChange = true;
            _cachedStaticDrawing = null;
        }
        else if (change.Property == XAxisProperty)
        {
            _xAxis = SwapAxis(change.GetOldValue<LuminaChartAxis>(), change.GetNewValue<LuminaChartAxis>());
            SyncCategorySubscription();
            _cachedStaticDrawing = null;
            InvalidateTooltipCache();
            _isDirty = true;
            InvalidateVisual();
        }
        else if (change.Property == YAxisProperty)
        {
            _yAxis = SwapAxis(change.GetOldValue<LuminaChartAxis>(), change.GetNewValue<LuminaChartAxis>());
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
                // Everything is drawn in local coordinates, so a pure move (same size) keeps the
                // cached static layer; only a resize has to rebuild it.
                _lastLayoutSize = newSize;
                _isDirty = true;
                _cachedStaticDrawing = null;
                InvalidateVisual();
            }
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
        InvalidateThemeDependentState();
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        InvalidateThemeDependentState();
    }

    private void InvalidateThemeDependentState()
    {
        _cachedStaticDrawing = null;
        InvalidateTooltipCache();
        _cachedCrosshairPen = null;
        _cachedTooltipBorderPen = null;
        _cachedSurfaceBrush = null;
        _isDirty = true;
        InvalidateVisual();
    }

    /// <summary>Theme surface brush, resolved once per theme instead of on every hover frame.</summary>
    private IBrush SurfaceBrush => _cachedSurfaceBrush ??= ChartDrawing.ResolveSurfaceBrush(this);

    private IPen TooltipBorderPen => _cachedTooltipBorderPen ??= ChartDrawing.ResolveTooltipBorderPen(this);

    private LuminaChartAxis SwapAxis(LuminaChartAxis? oldAxis, LuminaChartAxis? newAxis)
    {
        newAxis ??= new LuminaChartAxis();
        if (oldAxis != null)
        {
            oldAxis.PropertyChanged -= OnAxisChanged;
        }

        newAxis.PropertyChanged -= OnAxisChanged;
        if (_isAttached)
        {
            newAxis.PropertyChanged += OnAxisChanged;
        }

        return newAxis;
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnSeriesCollectionChanged(sender, e));
            return;
        }

        _rebuildDueToDataChange = true;
        _isDirty = true;
        SyncDataSubscriptions();
        InvalidateVisual();
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnPointsCollectionChanged(sender, e));
            return;
        }

        _rebuildDueToDataChange = true;
        _isDirty = true;
        InvalidateVisual();
    }

    private void OnSeriesItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnSeriesItemPropertyChanged(sender, e));
            return;
        }

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

    private void UpdateSeriesSubscription(IList<LuminaChartSeries>? next)
    {
        if (_seriesSubscription != null)
        {
            _seriesSubscription.CollectionChanged -= OnSeriesCollectionChanged;
            _seriesSubscription = null;
        }

        if (_isAttached && next is INotifyCollectionChanged nextObservable)
        {
            _seriesSubscription = nextObservable;
            _seriesSubscription.CollectionChanged += OnSeriesCollectionChanged;
        }

        SyncDataSubscriptions();
    }

    /// <summary>
    /// Keeps change subscriptions in sync with the current series list: one subscription per
    /// observable <c>Points</c> collection and one per series object, so both in-place point
    /// edits and series property edits (color, thickness, title) repaint the chart. While the
    /// chart is detached the desired set is empty, which releases every subscription.
    /// </summary>
    private void SyncDataSubscriptions()
    {
        // Double-buffered: the previous "current" sets become the next scratch sets, so a
        // rebuild per live-data tick does not allocate.
        HashSet<INotifyCollectionChanged> desiredPoints = _spareDesiredPoints;
        HashSet<LuminaChartSeries> desiredSeries = _spareDesiredSeries;
        HashSet<INotifyCollectionChanged> stalePoints = _pointsSubscriptions;
        HashSet<LuminaChartSeries> staleSeries = _seriesItemSubscriptions;
        desiredPoints.Clear();
        desiredSeries.Clear();
        IList<LuminaChartSeries>? series = _isAttached ? Series : null;
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
        stalePoints.Clear();
        staleSeries.Clear();
        _spareDesiredPoints = stalePoints;
        _spareDesiredSeries = staleSeries;
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
        // The legend wraps onto as many rows as it needs, so it is built before the plot rect,
        // which reserves its height.
        BuildLegendCache(visible);

        Rect plotRect = ComputePlotRect();
        if (plotRect.Width <= 0.0 || plotRect.Height <= 0.0)
        {
            return;
        }

        _plotRect = plotRect;
        // Areas fill towards y = 0 when zero is in range (and towards the nearer plot edge
        // otherwise), so negative values hang below the axis instead of filling to the bottom.
        _areaBaseline = Math.Clamp(
            MapY(Math.Clamp(0.0, _yScale.Min, _yScale.Max), _plotRect, _yScale),
            _plotRect.Top,
            _plotRect.Bottom);
        int barGroupCount = 0;
        foreach (LuminaChartSeries item in visible)
        {
            if (item is LuminaBarSeries)
            {
                barGroupCount++;
            }
        }

        int barGroupIndex = 0;
        double barSlot = barGroupCount > 0 ? ComputeSharedBarSlot(visible) : 0.0;
        for (int index = 0; index < visible.Count; index++)
        {
            RenderSeries? render = BuildRenderSeries(visible[index], index, barGroupIndex, barGroupCount, barSlot);
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

    private void BuildLegendCache(List<LuminaChartSeries> visible)
    {
        _cachedLegendItems.Clear();
        _cachedLegendHeight = 0.0;
        if (!ShowLegend || visible.Count == 0)
        {
            return;
        }

        IBrush textBrush = LegendTextBrush ?? AxisLabelBrush ?? Brushes.Gray;
        double fontSize = ChartDrawing.LegendFontSize;

        for (int index = 0; index < visible.Count; index++)
        {
            LuminaChartSeries source = visible[index];
            string title = string.IsNullOrEmpty(source.Title)
                ? $"S{index + 1}"
                : source.Title!;
            FormattedText text = ChartDrawing.CreateText(title, _labelTypeface, textBrush, fontSize);
            // Same color resolution as BuildRenderSeries, which uses the visible index too.
            _cachedLegendItems.Add((text, source.Stroke ?? ChartDrawing.ResolvePaletteBrush(this, index)));
        }

        _cachedLegendHeight = ChartDrawing.MeasureLegendHeight(_cachedLegendItems, Bounds.Size);
    }

    private void BuildTickLabels()
    {
        _labelTypeface = ChartDrawing.CreateTypeface(this);
        IBrush labelBrush = AxisLabelBrush ?? Brushes.Gray;
        double fontSize = AxisLabelFontSize;
        bool showLabels = ShowAxisLabels;

        GenerateNumericTicks(_yScale, _yTickValues);
        if (showLabels)
        {
            foreach (double y in _yTickValues)
            {
                string text = _yAxis.LabelFormatter?.Invoke(y) ?? FormatTick(y, _yScale.Step);
                _yTickLabels.Add(ChartDrawing.CreateText(text, _labelTypeface, labelBrush, fontSize));
            }
        }

        if (_xAxis.Kind == LuminaChartAxisKind.Category)
        {
            GenerateCategoryTicks(_xScale, _xTickValues);
            if (showLabels)
            {
                foreach (double x in _xTickValues)
                {
                    string text = FormatCategoryLabel((long)x);
                    _xTickLabels.Add(ChartDrawing.CreateText(text, _labelTypeface, labelBrush, fontSize));
                }
            }
        }
        else
        {
            GenerateNumericTicks(_xScale, _xTickValues);
            if (showLabels)
            {
                foreach (double x in _xTickValues)
                {
                    string text = _xAxis.LabelFormatter?.Invoke(x) ?? FormatTick(x, _xScale.Step);
                    _xTickLabels.Add(ChartDrawing.CreateText(text, _labelTypeface, labelBrush, fontSize));
                }
            }
        }
    }

    /// <summary>
    /// Emits <c>start + i * step</c> for every tick inside the scale. Ticks are derived from an
    /// integer index rather than by accumulating <c>step</c>, so the loop always terminates (an
    /// accumulating loop never advances once <c>step</c> drops below the precision of the
    /// values) and float error does not build up. The count is capped at
    /// <see cref="MaxTickCount"/> as a last line of defence against degenerate steps.
    /// </summary>
    private static void GenerateNumericTicks(AxisScale scale, List<double> output)
    {
        double step = scale.Step;
        if (!(step > 0.0) || !double.IsFinite(step) || !double.IsFinite(scale.Min) || !double.IsFinite(scale.Max) || scale.Max < scale.Min)
        {
            return;
        }

        double start = TickStart(scale.Min, step);
        double span = (scale.Max - start) / step;
        if (!double.IsFinite(start) || !double.IsFinite(span) || span < -1e-9)
        {
            return;
        }

        long count = Math.Min(MaxTickCount, (long)Math.Floor(span + 1e-9) + 1);
        double tolerance = step * 1e-9;
        for (long index = 0; index < count; index++)
        {
            double value = start + index * step;
            if (Math.Abs(value) < tolerance)
            {
                // Avoid "-0" labels from (-0.5 / step) rounding or tiny accumulated error.
                value = 0.0;
            }

            if (output.Count > 0 && value <= output[^1])
            {
                // The step is below the precision of the values; neighbouring ticks collapse.
                continue;
            }

            output.Add(value);
        }
    }

    private static void GenerateCategoryTicks(AxisScale scale, List<double> output)
    {
        if (!double.IsFinite(scale.Step) || !double.IsFinite(scale.Min) || !double.IsFinite(scale.Max))
        {
            return;
        }

        // Clamp to a range where long arithmetic is exact; category counts never get near it.
        const double Limit = 1e15;
        double first = Math.Max(0.0, Math.Ceiling(scale.Min));
        double last = Math.Min(Limit, Math.Floor(scale.Max));
        if (last < first)
        {
            return;
        }

        long start = (long)first;
        long end = (long)last;
        long step = Math.Max(1L, (long)Math.Min(Limit, Math.Round(scale.Step)));
        long span = end - start;
        if (span / step + 1 > MaxTickCount)
        {
            step = span / (MaxTickCount - 1) + 1;
        }

        for (long index = start; index <= end; index += step)
        {
            output.Add(index);
        }
    }

    private string FormatCategoryLabel(long index)
    {
        if (_xAxis.Categories != null && index >= 0 && index < _xAxis.Categories.Count)
        {
            return _xAxis.Categories[(int)index];
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

        if (ShowLegend && _cachedLegendHeight > 0.0)
        {
            if (LegendPosition is LuminaChartLegendPosition.TopLeft or LuminaChartLegendPosition.TopCenter or LuminaChartLegendPosition.TopRight)
            {
                top += _cachedLegendHeight + LegendBandGap;
            }
            else
            {
                bottomMargin += _cachedLegendHeight + LegendBandGap;
            }
        }

        double width = Math.Max(0.0, Bounds.Width - left - PlotRight);
        double height = Math.Max(0.0, Bounds.Height - top - bottomMargin);
        return new Rect(left, top, width, height);
    }

    private RenderSeries? BuildRenderSeries(LuminaChartSeries source, int paletteIndex, int barGroupIndex, int barGroupCount, double barSlot)
    {
        var render = new RenderSeries { Source = source };
        render.Stroke = source.Stroke ?? ChartDrawing.ResolvePaletteBrush(this, paletteIndex);
        render.Fill = source.Fill ?? (source.ShowArea ? ChartDrawing.CreateTranslucent(render.Stroke, 0.22) : null);
        render.Thickness = Math.Max(1.0, ChartDrawing.Finite(source.StrokeThickness, 2.0));
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
                BuildBars(render, bar, barGroupIndex, barGroupCount, barSlot);
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
            render.Area = BuildAreaGeometry(render.AreaPoints, _areaBaseline);
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

    private void BuildBars(RenderSeries render, LuminaBarSeries series, int barGroupIndex, int barGroupCount, double slot)
    {
        render.IsBar = true;
        render.BarCornerRadius = Math.Max(0.0, ChartDrawing.Finite(series.CornerRadius, 0.0));
        render.Fill ??= render.Stroke;
        render.HighlightOutlinePen = new Pen(ChartDrawing.ContrastFor(render.Fill), 1.5);
        render.BarRects = BuildBarRects(render, Math.Clamp(ChartDrawing.Finite(series.BarWidthRatio, 0.7), 0.01, 1.0), barGroupIndex, barGroupCount, slot);
        render.Markers = null;
    }

    private void BuildScatter(RenderSeries render, LuminaScatterSeries series)
    {
        render.IsScatter = true;
        render.MarkerRadius = Math.Max(0.5, ChartDrawing.Finite(series.MarkerSize, 3.0));
        render.Markers = BuildMarkerGeometry(ToPointArray(render.Px, render.Py), render.MarkerRadius);
    }

    /// <summary>Upper bound for <see cref="LuminaSplineSeries.SamplesPerSegment"/>.</summary>
    private const int MaxSplineSamplesPerSegment = 32;

    private void BuildSpline(RenderSeries render, LuminaSplineSeries series)
    {
        // Smoothing needs points ordered by x, and on large series it would only multiply the
        // geometry by SamplesPerSegment without visible benefit - those render as a regular
        // (decimated) line instead.
        if (!render.Sorted || render.Px.Length > Math.Max(4, DecimationThreshold))
        {
            BuildLineAndArea(render, series);
            return;
        }

        int samples = Math.Clamp(series.SamplesPerSegment, 2, MaxSplineSamplesPerSegment);
        Point[] splinePoints = BuildSplinePoints(render.Px, render.Py, samples);
        render.LinePoints = splinePoints;
        render.RawPoints = ToPointArray(render.Px, render.Py);
        render.Line = BuildLineGeometry(splinePoints);
        if (series.ShowArea)
        {
            render.AreaPoints = splinePoints;
            render.Area = BuildAreaGeometry(splinePoints, _areaBaseline);
        }

        if (series.ShowMarkers && render.Px.Length <= SubsetMarkersLimit)
        {
            render.Markers = BuildMarkerGeometry(render.RawPoints, 2.5);
        }
    }

    private Rect[] BuildBarRects(RenderSeries render, double widthRatio, int barGroupIndex, int barGroupCount, double slot)
    {
        if (render.Px.Length == 0)
        {
            return [];
        }

        int groupCount = Math.Max(1, barGroupCount);
        double band = slot / groupCount;
        double width = Math.Max(1.0, band * widthRatio);
        double baseline = Math.Clamp(MapY(Math.Clamp(0.0, _yScale.Min, _yScale.Max), _plotRect, _yScale), _plotRect.Top, _plotRect.Bottom);
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

    /// <summary>
    /// Computes one slot width (in pixels) shared by every bar series, so grouped bars line up
    /// and a series with a single point or with gaps still gets a sensible width. A category
    /// axis uses the width of one category; a numeric axis uses the smallest x distance between
    /// neighbouring bars across all bar series.
    /// </summary>
    private double ComputeSharedBarSlot(List<LuminaChartSeries> visible)
    {
        double slot;
        if (_xAxis.Kind == LuminaChartAxisKind.Category)
        {
            slot = Math.Abs(MapX(1.0, _plotRect, _xScale) - MapX(0.0, _plotRect, _xScale));
        }
        else
        {
            double gap = ComputeMinDataGap(visible);
            slot = double.IsFinite(gap)
                ? Math.Abs(MapX(_xScale.Min + gap, _plotRect, _xScale) - MapX(_xScale.Min, _plotRect, _xScale))
                : double.NaN;
        }

        if (!double.IsFinite(slot) || slot <= 0.0)
        {
            int maxPoints = 1;
            foreach (LuminaChartSeries item in visible)
            {
                if (item is LuminaBarSeries && item.Points != null)
                {
                    maxPoints = Math.Max(maxPoints, item.Points.Count);
                }
            }

            slot = _plotRect.Width / maxPoints;
        }

        return Math.Min(slot, _plotRect.Width);
    }

    /// <summary>
    /// Monotone cubic Hermite interpolation (Fritsch–Carlson) over points sorted by x. x advances
    /// linearly inside each segment, so the curve is a function of x (no loops, no backtracking),
    /// and the tangents are limited so y never overshoots the neighbouring data values.
    /// </summary>
    internal static Point[] BuildSplinePoints(double[] px, double[] py, int samplesPerSegment)
    {
        int count = px.Length;
        if (count < 3)
        {
            return ToPointArray(px, py);
        }

        var slopes = new double[count - 1];
        for (int index = 0; index < count - 1; index++)
        {
            double h = px[index + 1] - px[index];
            slopes[index] = h > 0.0 ? (py[index + 1] - py[index]) / h : 0.0;
        }

        var tangents = new double[count];
        tangents[0] = slopes[0];
        tangents[count - 1] = slopes[count - 2];
        for (int index = 1; index < count - 1; index++)
        {
            // Zero at local extrema and wherever the curve flattens, so it cannot overshoot.
            tangents[index] = slopes[index - 1] * slopes[index] <= 0.0
                ? 0.0
                : (slopes[index - 1] + slopes[index]) / 2.0;
        }

        for (int index = 0; index < count - 1; index++)
        {
            if (slopes[index] == 0.0)
            {
                tangents[index] = 0.0;
                tangents[index + 1] = 0.0;
                continue;
            }

            double alpha = tangents[index] / slopes[index];
            double beta = tangents[index + 1] / slopes[index];
            double magnitude = (alpha * alpha) + (beta * beta);
            if (magnitude > 9.0)
            {
                double tau = 3.0 / Math.Sqrt(magnitude);
                tangents[index] = tau * alpha * slopes[index];
                tangents[index + 1] = tau * beta * slopes[index];
            }
        }

        var result = new List<Point>(((count - 1) * samplesPerSegment) + 1) { new(px[0], py[0]) };
        for (int index = 0; index < count - 1; index++)
        {
            double x0 = px[index];
            double h = px[index + 1] - x0;
            if (h <= 0.0)
            {
                // Duplicate x: nothing to interpolate, just connect.
                result.Add(new Point(px[index + 1], py[index + 1]));
                continue;
            }

            for (int sample = 1; sample <= samplesPerSegment; sample++)
            {
                double t = sample / (double)samplesPerSegment;
                double t2 = t * t;
                double t3 = t2 * t;
                double h00 = (2.0 * t3) - (3.0 * t2) + 1.0;
                double h10 = t3 - (2.0 * t2) + t;
                double h01 = (-2.0 * t3) + (3.0 * t2);
                double h11 = t3 - t2;
                double y = (h00 * py[index]) + (h10 * h * tangents[index]) + (h01 * py[index + 1]) + (h11 * h * tangents[index + 1]);
                result.Add(new Point(x0 + (t * h), y));
            }
        }

        return result.ToArray();
    }

    private int MapFinitePoints(RenderSeries render, LuminaChartSeries source)
    {
        if (source.Points == null || source.Points.Count == 0)
        {
            render.Px = [];
            render.Py = [];
            render.Values = [];
            render.DataX = [];
            return 0;
        }

        bool categoryX = _xAxis.Kind == LuminaChartAxisKind.Category;
        var px = new List<double>(Math.Min(source.Points.Count, 4096));
        var py = new List<double>(px.Capacity);
        var values = new List<LuminaChartPoint>(px.Capacity);
        var dataX = new List<double>(px.Capacity);
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
            dataX.Add(xValue);
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
        render.DataX = dataX.ToArray();
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

        // Points outside a fixed viewport are not bucketed: clamping them into the edge buckets
        // used to put far-away extremes on the edge and draw the entry segment from the wrong
        // point. Only the nearest point on each side is kept, so the line still runs off the
        // plot edge in the right direction (the clip cuts it there).
        Point? leftOuter = null;
        Point? rightOuter = null;
        for (int index = 0; index < pointCount; index++)
        {
            double x = render.Px[index];
            double y = render.Py[index];
            if (x < _plotRect.Left - 0.5)
            {
                if (leftOuter is not { } left || x > left.X)
                {
                    leftOuter = new Point(x, y);
                }

                continue;
            }

            if (x > _plotRect.Right + 0.5)
            {
                if (rightOuter is not { } right || x < right.X)
                {
                    rightOuter = new Point(x, y);
                }

                continue;
            }

            int bucket = (int)Math.Clamp(Math.Round(x - _plotRect.Left), 0.0, bucketCount - 1);
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

        var line = new List<Point>((bucketCount * 2) + 2);
        var envelope = render.Source.ShowArea ? new List<Point>(bucketCount + 2) : null;
        if (leftOuter is { } leftPoint)
        {
            line.Add(leftPoint);
            envelope?.Add(leftPoint);
        }

        for (int bucket = 0; bucket < bucketCount; bucket++)
        {
            if (!valid[bucket])
            {
                continue;
            }

            // Emit the bucket's extremes in x order so the polyline does not double back.
            var top = new Point(topX[bucket], topY[bucket]);
            var bottom = new Point(bottomX[bucket], bottomY[bucket]);
            if (top.X <= bottom.X)
            {
                line.Add(top);
                line.Add(bottom);
            }
            else
            {
                line.Add(bottom);
                line.Add(top);
            }

            envelope?.Add(top);
        }

        if (rightOuter is { } rightPoint)
        {
            line.Add(rightPoint);
            envelope?.Add(rightPoint);
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
            if (!double.IsFinite(minGap))
            {
                minGap = 1.0;
            }

            xMin -= minGap * 0.5;
            xMax += minGap * 0.5;
        }

        if (!double.IsFinite(xMin) || !double.IsFinite(xMax))
        {
            (xMin, xMax) = (0.0, 1.0);
        }

        if (!double.IsFinite(yMin) || !double.IsFinite(yMax))
        {
            (yMin, yMax) = (0.0, 1.0);
        }

        if (hasBarSeries)
        {
            // Bars encode magnitude by their length from zero; a baseline at the data minimum
            // would make the smallest bar vanish. Fixed axis bounds below still win.
            yMin = Math.Min(yMin, 0.0);
            yMax = Math.Max(yMax, 0.0);
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

        xStep = EnforceTickBudget(xMin, xMax, xStep);
        yStep = EnforceTickBudget(yMin, yMax, yStep);

        return (new AxisScale(xMin, xMax, xStep), new AxisScale(yMin, yMax, yStep));
    }

    /// <summary>
    /// Widens a step (typically a user-supplied <see cref="LuminaChartAxis.TickStep"/>) that would
    /// produce more than <see cref="MaxTickCount"/> ticks, so labels stay readable and the tick
    /// build stays bounded.
    /// </summary>
    private static double EnforceTickBudget(double min, double max, double step)
    {
        double count = (max - min) / step;
        if (!double.IsFinite(count) || count <= MaxTickCount)
        {
            return step;
        }

        double widened = NiceNum((max - min) / MaxTickCount, round: false);
        return double.IsFinite(widened) && widened > step ? widened : step;
    }

    /// <summary>
    /// Picks an integer tick step for a category axis. Every category gets a label while the
    /// labels still fit, and the step widens as the plot narrows or the category set grows so
    /// labels and grid lines never pile up.
    /// </summary>
    private double ComputeCategoryStep(double min, double max)
    {
        double span = Math.Round(max - min);
        if (!(span > 1.0))
        {
            return 1.0;
        }

        int categoryCount = span >= int.MaxValue ? int.MaxValue : (int)span;

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

        return minGap;
    }

    private static void ApplyAxisRange(LuminaChartAxis axis, ref double rangeMin, ref double rangeMax)
    {
        // Non-finite bounds cannot be mapped to pixels (and used to stall tick generation), so
        // they are treated as "auto".
        if (axis.Minimum is { } minimum && double.IsFinite(minimum))
        {
            rangeMin = minimum;
        }

        if (axis.Maximum is { } maximum && double.IsFinite(maximum))
        {
            rangeMax = maximum;
        }

        if (rangeMin > rangeMax)
        {
            (rangeMin, rangeMax) = (rangeMax, rangeMin);
        }

        if (!double.IsFinite(rangeMax - rangeMin))
        {
            // Finite bounds whose span overflows (e.g. ±1e308) - shrink so the scale stays finite.
            const double Half = double.MaxValue / 4.0;
            rangeMin = Math.Max(rangeMin, -Half);
            rangeMax = Math.Min(rangeMax, Half);
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
            DrawGridLines(context);
        }

        // Axis labels are independent of the grid: hiding grid lines must not hide the labels
        // that the plot rect still reserves room for. The label lists are only populated when
        // ShowAxisLabels is on.
        DrawAxisLabels(context);

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

    private void DrawGridLines(DrawingContext context)
    {
        IBrush gridBrush = GridLineBrush ?? Brushes.Gray;
        var gridPen = new Pen(gridBrush, Math.Max(0.5, GridLineThickness));

        foreach (double tick in _yTickValues)
        {
            double pixelY = MapY(tick, _plotRect, _yScale);
            context.DrawLine(gridPen, new Point(_plotRect.Left, pixelY), new Point(_plotRect.Right, pixelY));
        }

        foreach (double tick in _xTickValues)
        {
            double pixelX = MapX(tick, _plotRect, _xScale);
            context.DrawLine(gridPen, new Point(pixelX, _plotRect.Top), new Point(pixelX, _plotRect.Bottom));
        }
    }

    private void DrawAxisLabels(DrawingContext context)
    {
        for (int index = 0; index < _yTickLabels.Count && index < _yTickValues.Count; index++)
        {
            double pixelY = MapY(_yTickValues[index], _plotRect, _yScale);
            DrawAxisLabel(context, _yTickLabels[index], _plotRect.Left - PlotLeftPad, pixelY, HorizontalAlignment.Right);
        }

        for (int index = 0; index < _xTickLabels.Count && index < _xTickValues.Count; index++)
        {
            double pixelX = MapX(_xTickValues[index], _plotRect, _xScale);
            DrawAxisLabel(context, _xTickLabels[index], pixelX, _plotRect.Bottom + PlotBottomPad, HorizontalAlignment.Center);
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
        IBrush markerFill = SurfaceBrush;
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
                area = BuildInterpolatedArea(prevArea, render.AreaPoints, _transitionT, _areaBaseline) ?? render.Area;
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
        ChartDrawing.DrawLegend(context, _cachedLegendItems, LegendPosition, Bounds.Size);
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
        double threshold = Math.Max(1.0, ChartDrawing.Finite(HoverThreshold, 28.0));

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
        double anchorValueX = anchored ? anchorSeries!.DataX[anchorIndex] : 0.0;

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
                    _overlayMatches.Add(new OverlayMatch(render, render.Values[barIndex], pixel, render.DataX[barIndex]));
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
                _overlayMatches.Add(new OverlayMatch(render, render.Values[anchorIndex], pixel, render.DataX[anchorIndex]));
                _scatterHighlights.Add(new OverlayMatch(render, render.Values[anchorIndex], pixel, render.DataX[anchorIndex]));
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
                    _overlayMatches.Add(new OverlayMatch(render, sample, samplePixel, anchorValueX));
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
            _overlayMatches.Add(new OverlayMatch(render, render.Values[lineIndex], linePixel, render.DataX[lineIndex]));
            if (lineDistance < bestLineDistance)
            {
                bestLineDistance = lineDistance;
                lineTrackPixel = linePixel;
            }
        }

        // Markers and outlines get the same slack as the series layer (a dot on the axis edge
        // stays whole) but no more, so with a fixed viewport they do not spill over the axis
        // labels. The tooltip is drawn afterwards, unclipped.
        double overlaySlack = 6.0;
        foreach (OverlayMatch match in _scatterHighlights)
        {
            overlaySlack = Math.Max(overlaySlack, match.Series.MarkerRadius + 4.0);
        }

        using (context.PushClip(_plotRect.Inflate(overlaySlack)))
        {
            DrawOverlayMarks(context, anchored ? anchorX : lineTrackPixel?.X);
        }

        DrawTooltip(context, hover);
    }

    private void DrawOverlayMarks(DrawingContext context, double? crosshairX)
    {
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
            var outlinePen = highlight.Series.HighlightOutlinePen ?? new Pen(ChartDrawing.ContrastFor(highlight.Series.Fill ?? highlight.Series.Stroke), 1.5);
            context.DrawRectangle(null, outlinePen, rect);
        }

        IBrush hoverFill = SurfaceBrush;
        foreach (OverlayMatch match in _overlayMatches)
        {
            if (match.Series.IsBar || match.Series.IsScatter)
            {
                continue;
            }

            var markerPen = match.Series.HighlightMarkerPen ?? new Pen(match.Series.Stroke, 2.0);
            context.DrawEllipse(hoverFill, markerPen, match.Pixel, 3.5, 3.5);
        }
    }

    private int FindBarAt(RenderSeries render, Point hover)
    {
        Rect[] rects = render.BarRects;
        if (render.Sorted)
        {
            // Every bar of a series has the same width and x offset, so with sorted points both
            // edges grow with the index: skip to the first bar whose right edge is past the
            // pointer, then only bars that start left of it can contain it.
            int low = 0;
            int high = rects.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (rects[middle].Right < hover.X)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            for (int index = low; index < rects.Length && rects[index].Left <= hover.X; index++)
            {
                if (rects[index].Contains(hover))
                {
                    return index;
                }
            }

            return -1;
        }

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
                    cached.X != current.DataX ||
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
                _cachedTooltipMatches.Add((match.Series.Source, match.DataX, match.Value.Y));
                string title = string.IsNullOrEmpty(match.Series.Source.Title) ? "Values" : match.Series.Source.Title;
                string text = $"{title}: ({FormatTooltipX(match.DataX)}, {FormatTooltipY(match.Value.Y)})";
                _cachedTooltipLines.Add(ChartDrawing.CreateText(text, _labelTypeface, textBrush, fontSize));
            }

            _cachedTooltipSize = ChartDrawing.MeasureTooltip(_cachedTooltipLines, TooltipPadding);
        }

        Rect? legendRect = ShowLegend && _cachedLegendItems.Count > 0
            ? ChartDrawing.ComputeLegendRect(_cachedLegendItems, LegendPosition, Bounds.Size)
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
            TooltipBorderPen,
            TooltipPadding,
            TooltipCornerRadius,
            origin,
            _cachedTooltipSize);
    }

    private static string FormatTick(double value, double step)
    {
        // Use as many decimals as the step needs to be represented exactly (0.25 -> 2,
        // 0.0002 -> 4), so neighbouring labels never round to the same text.
        int decimals = 0;
        if (step > 0.0 && double.IsFinite(step))
        {
            while (decimals < 10 && Math.Abs(Math.Round(step, decimals) - step) > step * 1e-6)
            {
                decimals++;
            }
        }

        string format = decimals > 0 ? "0." + new string('0', decimals) : "0";
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    private static string FormatValue(double value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Tooltip X text in the same vocabulary as the axis: the category label on a category axis
    /// (the point's own X is meaningless there), otherwise the axis label formatter if any.
    /// </summary>
    private string FormatTooltipX(double dataX)
    {
        if (_xAxis.Kind == LuminaChartAxisKind.Category)
        {
            return FormatCategoryLabel((long)Math.Round(dataX));
        }

        return _xAxis.LabelFormatter?.Invoke(dataX) ?? FormatValue(dataX);
    }

    private string FormatTooltipY(double value) => _yAxis.LabelFormatter?.Invoke(value) ?? FormatValue(value);

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
        public double[] DataX = [];
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
        public Pen? HighlightOutlinePen;
    }

    /// <param name="DataX">X in axis units: the point's X on a numeric axis, its index on a category axis.</param>
    private readonly record struct OverlayMatch(RenderSeries Series, LuminaChartPoint Value, Point Pixel, double DataX);

    private readonly record struct BarHighlight(RenderSeries Series, int Index);

    private readonly record struct AxisScale(double Min, double Max, double Step);
}