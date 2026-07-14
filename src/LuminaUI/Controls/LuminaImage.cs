using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Localization;
using LuminaUI.Services;

namespace LuminaUI.Controls;

public class LuminaImage : TemplatedControl
{
    private CancellationTokenSource? _loadCancellation;

    private IImage? _imageSource;

    private LuminaImageStatus _status = LuminaImageStatus.Idle;

    private string? _errorMessage;

    private bool _hasImage;

    private bool _isLoading;

    private bool _hasError;

    private bool _hasPlaceholderContent;

    private bool _hasErrorContent;

    private bool _showDefaultLoading;

    private bool _showDefaultError;

    private bool _showPlaceholderContent;

    private bool _showErrorContent;

    private string _displayLoadingText = string.Empty;

    private string _displayErrorText = string.Empty;

    private bool _isLocalizationSubscribed;

    private int _loadVersion;

    private bool _pendingAutoSizeLoad;

    public static readonly StyledProperty<ILuminaImageLoader?> LoaderProperty = AvaloniaProperty.Register<LuminaImage, ILuminaImageLoader?>(nameof(Loader));

    public static readonly StyledProperty<object?> SourceProperty = AvaloniaProperty.Register<LuminaImage, object?>(nameof(Source));

    public static readonly StyledProperty<int> DecodeWidthProperty = AvaloniaProperty.Register<LuminaImage, int>(nameof(DecodeWidth));

    public static readonly StyledProperty<int> DecodeHeightProperty = AvaloniaProperty.Register<LuminaImage, int>(nameof(DecodeHeight));

    public static readonly StyledProperty<bool> AutoDecodeToDisplaySizeProperty = AvaloniaProperty.Register<LuminaImage, bool>(nameof(AutoDecodeToDisplaySize), defaultValue: true);

    public static readonly StyledProperty<LuminaImageCacheMode> ImageCacheModeProperty = AvaloniaProperty.Register<LuminaImage, LuminaImageCacheMode>(nameof(ImageCacheMode), LuminaImageCacheMode.MemoryAndDisk);

    public static readonly StyledProperty<TimeSpan> CacheDurationProperty = AvaloniaProperty.Register<LuminaImage, TimeSpan>(nameof(CacheDuration), TimeSpan.FromDays(7));

    public static readonly StyledProperty<string?> CacheDirectoryProperty = AvaloniaProperty.Register<LuminaImage, string?>(nameof(CacheDirectory));

    public static readonly StyledProperty<Stretch> StretchProperty = AvaloniaProperty.Register<LuminaImage, Stretch>(nameof(Stretch), Stretch.UniformToFill);

    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty = AvaloniaProperty.Register<LuminaImage, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

    public static readonly StyledProperty<object?> PlaceholderContentProperty = AvaloniaProperty.Register<LuminaImage, object?>(nameof(PlaceholderContent));

    public static readonly StyledProperty<IDataTemplate?> PlaceholderContentTemplateProperty = AvaloniaProperty.Register<LuminaImage, IDataTemplate?>(nameof(PlaceholderContentTemplate));

    public static readonly StyledProperty<object?> ErrorContentProperty = AvaloniaProperty.Register<LuminaImage, object?>(nameof(ErrorContent));

    public static readonly StyledProperty<IDataTemplate?> ErrorContentTemplateProperty = AvaloniaProperty.Register<LuminaImage, IDataTemplate?>(nameof(ErrorContentTemplate));

    public static readonly StyledProperty<string?> LoadingTextProperty = AvaloniaProperty.Register<LuminaImage, string?>(nameof(LoadingText));

    public static readonly StyledProperty<string?> ErrorTextProperty = AvaloniaProperty.Register<LuminaImage, string?>(nameof(ErrorText));

    public static readonly StyledProperty<IBrush?> LoadingBackgroundProperty = AvaloniaProperty.Register<LuminaImage, IBrush?>(nameof(LoadingBackground));

    public static readonly StyledProperty<IBrush?> LoadingForegroundProperty = AvaloniaProperty.Register<LuminaImage, IBrush?>(nameof(LoadingForeground));

    public static readonly StyledProperty<IBrush?> ErrorBackgroundProperty = AvaloniaProperty.Register<LuminaImage, IBrush?>(nameof(ErrorBackground));

    public static readonly StyledProperty<IBrush?> ErrorForegroundProperty = AvaloniaProperty.Register<LuminaImage, IBrush?>(nameof(ErrorForeground));

    public static readonly DirectProperty<LuminaImage, IImage?> ImageSourceProperty = AvaloniaProperty.RegisterDirect("ImageSource", (LuminaImage image) => image.ImageSource);

    public static readonly DirectProperty<LuminaImage, LuminaImageStatus> StatusProperty = AvaloniaProperty.RegisterDirect<LuminaImage, LuminaImageStatus>(nameof(Status), (LuminaImage image) => image.Status, null, LuminaImageStatus.Idle);

    public static readonly DirectProperty<LuminaImage, string?> ErrorMessageProperty = AvaloniaProperty.RegisterDirect<LuminaImage, string?>(nameof(ErrorMessage), (LuminaImage image) => image.ErrorMessage);

    public static readonly DirectProperty<LuminaImage, bool> HasImageProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(HasImage), (LuminaImage image) => image.HasImage, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> IsLoadingProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(IsLoading), (LuminaImage image) => image.IsLoading, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> HasErrorProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(HasError), (LuminaImage image) => image.HasError, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> HasPlaceholderContentProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(HasPlaceholderContent), (LuminaImage image) => image.HasPlaceholderContent, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> HasErrorContentProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(HasErrorContent), (LuminaImage image) => image.HasErrorContent, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> ShowDefaultLoadingProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(ShowDefaultLoading), (LuminaImage image) => image.ShowDefaultLoading, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> ShowDefaultErrorProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(ShowDefaultError), (LuminaImage image) => image.ShowDefaultError, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> ShowPlaceholderContentProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(ShowPlaceholderContent), (LuminaImage image) => image.ShowPlaceholderContent, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, bool> ShowErrorContentProperty = AvaloniaProperty.RegisterDirect<LuminaImage, bool>(nameof(ShowErrorContent), (LuminaImage image) => image.ShowErrorContent, null, unsetValue: false);

    public static readonly DirectProperty<LuminaImage, string> DisplayLoadingTextProperty = AvaloniaProperty.RegisterDirect<LuminaImage, string>(nameof(DisplayLoadingText), (LuminaImage image) => image.DisplayLoadingText);

    public static readonly DirectProperty<LuminaImage, string> DisplayErrorTextProperty = AvaloniaProperty.RegisterDirect<LuminaImage, string>(nameof(DisplayErrorText), (LuminaImage image) => image.DisplayErrorText);

    public static ILuminaImageLoader ImageLoader { get; set; } = LuminaImageLoader.Shared;

    public ILuminaImageLoader? Loader
    {
        get => GetValue(LoaderProperty);
        set => SetValue(LoaderProperty, value);
    }

    public object? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// 目标解码像素宽度。&gt;0 时按该宽度降采样解码（保持宽高比），显著降低大图内存与解码开销。
    /// 为 0 且 <see cref="AutoDecodeToDisplaySize"/> 为 true 时，将按控件实际显示尺寸自动推断。
    /// </summary>
    public int DecodeWidth
    {
        get => GetValue(DecodeWidthProperty);
        set => SetValue(DecodeWidthProperty, value);
    }

    /// <summary>
    /// 目标解码像素高度。&gt;0 时按该高度降采样解码（保持宽高比）。仅在 <see cref="DecodeWidth"/> 未设置时生效。
    /// </summary>
    public int DecodeHeight
    {
        get => GetValue(DecodeHeightProperty);
        set => SetValue(DecodeHeightProperty, value);
    }

    /// <summary>
    /// 是否在未显式指定 <see cref="DecodeWidth"/>/<see cref="DecodeHeight"/> 时，
    /// 自动按控件的显示尺寸降采样解码。默认 true，建议在固定尺寸的列表封面场景保持开启。
    /// </summary>
    public bool AutoDecodeToDisplaySize
    {
        get => GetValue(AutoDecodeToDisplaySizeProperty);
        set => SetValue(AutoDecodeToDisplaySizeProperty, value);
    }

    public LuminaImageCacheMode ImageCacheMode
    {
        get => GetValue(ImageCacheModeProperty);
        set => SetValue(ImageCacheModeProperty, value);
    }

    public TimeSpan CacheDuration
    {
        get => GetValue(CacheDurationProperty);
        set => SetValue(CacheDurationProperty, value);
    }

    public string? CacheDirectory
    {
        get => GetValue(CacheDirectoryProperty);
        set => SetValue(CacheDirectoryProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public StretchDirection StretchDirection
    {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    public object? PlaceholderContent
    {
        get => GetValue(PlaceholderContentProperty);
        set => SetValue(PlaceholderContentProperty, value);
    }

    public IDataTemplate? PlaceholderContentTemplate
    {
        get => GetValue(PlaceholderContentTemplateProperty);
        set => SetValue(PlaceholderContentTemplateProperty, value);
    }

    public object? ErrorContent
    {
        get => GetValue(ErrorContentProperty);
        set => SetValue(ErrorContentProperty, value);
    }

    public IDataTemplate? ErrorContentTemplate
    {
        get => GetValue(ErrorContentTemplateProperty);
        set => SetValue(ErrorContentTemplateProperty, value);
    }

    public string? LoadingText
    {
        get => GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public IBrush? LoadingBackground
    {
        get => GetValue(LoadingBackgroundProperty);
        set => SetValue(LoadingBackgroundProperty, value);
    }

    public IBrush? LoadingForeground
    {
        get => GetValue(LoadingForegroundProperty);
        set => SetValue(LoadingForegroundProperty, value);
    }

    public IBrush? ErrorBackground
    {
        get => GetValue(ErrorBackgroundProperty);
        set => SetValue(ErrorBackgroundProperty, value);
    }

    public IBrush? ErrorForeground
    {
        get => GetValue(ErrorForegroundProperty);
        set => SetValue(ErrorForegroundProperty, value);
    }

    public IImage? ImageSource
    {
        get
        {
            return _imageSource;
        }
        private set
        {
            SetAndRaise(ImageSourceProperty, ref _imageSource, value);
        }
    }

    public LuminaImageStatus Status
    {
        get
        {
            return _status;
        }
        private set
        {
            SetAndRaise(StatusProperty, ref _status, value);
        }
    }

    public string? ErrorMessage
    {
        get
        {
            return _errorMessage;
        }
        private set
        {
            SetAndRaise(ErrorMessageProperty, ref _errorMessage, value);
        }
    }

    public bool HasImage
    {
        get
        {
            return _hasImage;
        }
        private set
        {
            SetAndRaise(HasImageProperty, ref _hasImage, value);
        }
    }

    public bool IsLoading
    {
        get
        {
            return _isLoading;
        }
        private set
        {
            SetAndRaise(IsLoadingProperty, ref _isLoading, value);
        }
    }

    public bool HasError
    {
        get
        {
            return _hasError;
        }
        private set
        {
            SetAndRaise(HasErrorProperty, ref _hasError, value);
        }
    }

    public bool HasPlaceholderContent
    {
        get
        {
            return _hasPlaceholderContent;
        }
        private set
        {
            SetAndRaise(HasPlaceholderContentProperty, ref _hasPlaceholderContent, value);
        }
    }

    public bool HasErrorContent
    {
        get
        {
            return _hasErrorContent;
        }
        private set
        {
            SetAndRaise(HasErrorContentProperty, ref _hasErrorContent, value);
        }
    }

    public bool ShowDefaultLoading
    {
        get
        {
            return _showDefaultLoading;
        }
        private set
        {
            SetAndRaise(ShowDefaultLoadingProperty, ref _showDefaultLoading, value);
        }
    }

    public bool ShowDefaultError
    {
        get
        {
            return _showDefaultError;
        }
        private set
        {
            SetAndRaise(ShowDefaultErrorProperty, ref _showDefaultError, value);
        }
    }

    public bool ShowPlaceholderContent
    {
        get
        {
            return _showPlaceholderContent;
        }
        private set
        {
            SetAndRaise(ShowPlaceholderContentProperty, ref _showPlaceholderContent, value);
        }
    }

    public bool ShowErrorContent
    {
        get
        {
            return _showErrorContent;
        }
        private set
        {
            SetAndRaise(ShowErrorContentProperty, ref _showErrorContent, value);
        }
    }

    public string DisplayLoadingText
    {
        get
        {
            return _displayLoadingText;
        }
        private set
        {
            SetAndRaise(DisplayLoadingTextProperty, ref _displayLoadingText, value);
        }
    }

    public string DisplayErrorText
    {
        get
        {
            return _displayErrorText;
        }
        private set
        {
            SetAndRaise(DisplayErrorTextProperty, ref _displayErrorText, value);
        }
    }

    public LuminaImage()
    {
        UpdateDisplayText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty || change.Property == LoaderProperty || change.Property == ImageCacheModeProperty || change.Property == CacheDurationProperty || change.Property == CacheDirectoryProperty || change.Property == DecodeWidthProperty || change.Property == DecodeHeightProperty || change.Property == AutoDecodeToDisplaySizeProperty)
        {
            BeginLoad();
        }
        else if (change.Property == PlaceholderContentProperty)
        {
            UpdateContentState();
        }
        else if (change.Property == ErrorContentProperty)
        {
            UpdateContentState();
        }
        else if (change.Property == LoadingTextProperty || change.Property == ErrorTextProperty)
        {
            UpdateDisplayText();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeLocalization();
        UpdateDisplayText();
        BeginLoad();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelLoad();
        UnsubscribeLocalization();
        base.OnDetachedFromVisualTree(e);
    }

    private void BeginLoad(Size? arrangedSize = null)
    {
        int version = ++_loadVersion;
        CancelLoad();
        _pendingAutoSizeLoad = false;
        if (Source == null)
        {
            SetIdle(version);
            return;
        }

        // 自动按显示尺寸降采样时，若布局尚未完成（Bounds 为 0）则推迟加载，
        // 待 ArrangeOverride 拿到真实尺寸后再解码，避免退化为全分辨率解码。
        bool wantsAutoSize = DecodeWidth <= 0 && DecodeHeight <= 0 && AutoDecodeToDisplaySize;
        LuminaImageLoadOptions options = ResolveLoadOptions(arrangedSize);
        if (wantsAutoSize && options.DecodePixelWidth <= 0 && options.DecodePixelHeight <= 0)
        {
            _pendingAutoSizeLoad = true;
            SetLoading();
            return;
        }

        ILuminaImageLoader loader = Loader ?? ImageLoader;

        // 虚拟化关键路径：列表项被回收复用时会反复 detach/attach。
        // 若已解码位图缓存命中，则同步直接复用，避免每次都走异步加载 + 重新解码，
        // 既消除滚动闪烁（先 Loading 再出图），也消除重复解码开销。
        IImage? cached = loader.TryGetCachedImage(Source, options);
        if (cached != null)
        {
            ImageSource = cached;
            Status = LuminaImageStatus.Loaded;
            ErrorMessage = null;
            UpdateContentState();
            return;
        }

        _loadCancellation = new CancellationTokenSource();
        CancellationToken token = _loadCancellation.Token;
        SetLoading();
        _ = LoadAsync(Source, options, version, token);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Size result = base.ArrangeOverride(finalSize);

        // ArrangeOverride 执行时 Bounds 尚未提交，直接使用当前布局分配的尺寸触发降采样解码。
        if (_pendingAutoSizeLoad && (finalSize.Width > 0 || finalSize.Height > 0))
        {
            _pendingAutoSizeLoad = false;
            BeginLoad(finalSize);
        }

        return result;
    }

    private async Task LoadAsync(object? source, LuminaImageLoadOptions options, int version, CancellationToken cancellationToken)
    {
        try
        {
            ILuminaImageLoader loader = Loader ?? ImageLoader;
            IImage? image = await loader.LoadAsync(source, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                if (!cancellationToken.IsCancellationRequested && version == _loadVersion)
                {
                    if (image == null)
                    {
                        SetFailed("Image source could not be decoded.");
                    }
                    else
                    {
                        ImageSource = image;
                        Status = LuminaImageStatus.Loaded;
                        ErrorMessage = null;
                        UpdateContentState();
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                if (version == _loadVersion)
                {
                    SetFailed(ex.Message);
                }
            });
        }
    }

    private LuminaImageLoadOptions ResolveLoadOptions(Size? arrangedSize = null)
    {
        int decodeWidth = DecodeWidth;
        int decodeHeight = DecodeHeight;

        if (decodeWidth <= 0 && decodeHeight <= 0 && AutoDecodeToDisplaySize)
        {
            (decodeWidth, decodeHeight) = ResolveAutoDecodeSize(arrangedSize);
        }

        return new LuminaImageLoadOptions(ImageCacheMode, CacheDuration, CacheDirectory, decodeWidth, decodeHeight);
    }

    private (int width, int height) ResolveAutoDecodeSize(Size? arrangedSize = null)
    {
        // 优先用显式宽/高；首次布局使用当前分配尺寸，其他场景回退到 Bounds；
        // 再乘以渲染缩放(DPI)得到物理像素，避免高分屏下解码过小而模糊。
        double scaling = TryGetRenderScaling();

        double width = ResolveDimension(Width, arrangedSize?.Width ?? Bounds.Width);
        double height = ResolveDimension(Height, arrangedSize?.Height ?? Bounds.Height);

        int pixelWidth = ToDecodePixels(width, scaling);
        int pixelHeight = ToDecodePixels(height, scaling);

        // DecodeToWidth 优先；只有在没有有效宽度时才用高度。
        if (pixelWidth > 0)
        {
            return (pixelWidth, 0);
        }
        if (pixelHeight > 0)
        {
            return (0, pixelHeight);
        }
        return (0, 0);
    }

    private static double ResolveDimension(double explicitValue, double boundsValue)
    {
        if (!double.IsNaN(explicitValue) && explicitValue > 0 && !double.IsInfinity(explicitValue))
        {
            return explicitValue;
        }
        if (boundsValue > 0 && !double.IsInfinity(boundsValue))
        {
            return boundsValue;
        }
        return 0;
    }

    private static int ToDecodePixels(double logicalSize, double scaling)
    {
        if (logicalSize <= 0)
        {
            return 0;
        }
        double pixels = logicalSize * scaling;
        if (pixels <= 0 || double.IsInfinity(pixels))
        {
            return 0;
        }
        return (int)Math.Ceiling(pixels);
    }

    private double TryGetRenderScaling()
    {
        try
        {
            double scaling = Avalonia.Controls.TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            return scaling > 0 ? scaling : 1.0;
        }
        catch
        {
            return 1.0;
        }
    }

    private void CancelLoad()
    {
        if (_loadCancellation != null)
        {
            _loadCancellation.Cancel();
            _loadCancellation.Dispose();
            _loadCancellation = null;
        }
    }

    private void SetIdle(int version)
    {
        if (version == _loadVersion)
        {
            ImageSource = null;
            Status = LuminaImageStatus.Idle;
            ErrorMessage = null;
            UpdateContentState();
        }
    }

    private void SetLoading()
    {
        ImageSource = null;
        Status = LuminaImageStatus.Loading;
        ErrorMessage = null;
        UpdateContentState();
    }

    private void SetFailed(string message)
    {
        ImageSource = null;
        Status = LuminaImageStatus.Failed;
        ErrorMessage = message;
        UpdateContentState();
    }

    private void UpdateContentState()
    {
        HasImage = ImageSource != null && Status == LuminaImageStatus.Loaded;
        IsLoading = Status == LuminaImageStatus.Loading;
        HasError = Status == LuminaImageStatus.Failed;
        HasPlaceholderContent = PlaceholderContent != null;
        HasErrorContent = ErrorContent != null;
        ShowPlaceholderContent = IsLoading && HasPlaceholderContent;
        ShowErrorContent = HasError && HasErrorContent;
        ShowDefaultLoading = IsLoading && !HasPlaceholderContent;
        ShowDefaultError = HasError && !HasErrorContent;
    }

    private void UpdateDisplayText()
    {
        DisplayLoadingText = (!string.IsNullOrWhiteSpace(LoadingText)) ? LoadingText : LuminaLocalization.Get("Lumina.Page.Loading");
        DisplayErrorText = (!string.IsNullOrWhiteSpace(ErrorText)) ? ErrorText : LuminaLocalization.Get("Lumina.Image.Unavailable");
    }

    private void SubscribeLocalization()
    {
        if (!_isLocalizationSubscribed)
        {
            LuminaLocalization.LanguageChanged += OnLanguageChanged;
            _isLocalizationSubscribed = true;
        }
    }

    private void UnsubscribeLocalization()
    {
        if (_isLocalizationSubscribed)
        {
            LuminaLocalization.LanguageChanged -= OnLanguageChanged;
            _isLocalizationSubscribed = false;
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateDisplayText();
    }
}
