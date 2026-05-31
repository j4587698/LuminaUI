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

    public static readonly StyledProperty<ILuminaImageLoader?> LoaderProperty = AvaloniaProperty.Register<LuminaImage, ILuminaImageLoader?>(nameof(Loader));

    public static readonly StyledProperty<object?> SourceProperty = AvaloniaProperty.Register<LuminaImage, object?>(nameof(Source));

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
        if (change.Property == SourceProperty || change.Property == LoaderProperty || change.Property == ImageCacheModeProperty || change.Property == CacheDurationProperty || change.Property == CacheDirectoryProperty)
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

    private void BeginLoad()
    {
        int version = ++_loadVersion;
        CancelLoad();
        if (Source == null)
        {
            SetIdle(version);
            return;
        }
        _loadCancellation = new CancellationTokenSource();
        CancellationToken token = _loadCancellation.Token;
        SetLoading();
        _ = LoadAsync(Source, version, token);
    }

    private async Task LoadAsync(object? source, int version, CancellationToken cancellationToken)
    {
        try
        {
            LuminaImageLoadOptions options = new LuminaImageLoadOptions(ImageCacheMode, CacheDuration, CacheDirectory);
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
