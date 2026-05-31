using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaBanner : HeaderedContentControl
{
    private Button? _closeButton;

    public static readonly StyledProperty<LuminaBannerType> TypeProperty;

    public static readonly StyledProperty<bool> CanCloseProperty;

    public static readonly StyledProperty<bool> ShowIconProperty;

    public static readonly StyledProperty<object?> IconProperty;

    public static readonly StyledProperty<ICommand?> CloseCommandProperty;

    public static readonly DirectProperty<LuminaBanner, bool> HasHeaderProperty;

    private bool _hasHeader;

    public static readonly DirectProperty<LuminaBanner, object?> EffectiveIconProperty;

    private object? _effectiveIcon;

    public LuminaBannerType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public bool CanClose
    {
        get => GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public bool HasHeader
    {
        get
        {
            return _hasHeader;
        }
        private set
        {
            SetAndRaise(HasHeaderProperty, ref _hasHeader, value);
        }
    }

    public object? EffectiveIcon
    {
        get
        {
            return _effectiveIcon;
        }
        private set
        {
            SetAndRaise(EffectiveIconProperty, ref _effectiveIcon, value);
        }
    }

    static LuminaBanner()
    {
        TypeProperty = AvaloniaProperty.Register<LuminaBanner, LuminaBannerType>(nameof(Type), LuminaBannerType.Info);
        CanCloseProperty = AvaloniaProperty.Register<LuminaBanner, bool>(nameof(CanClose), defaultValue: false);
        ShowIconProperty = AvaloniaProperty.Register<LuminaBanner, bool>(nameof(ShowIcon), defaultValue: true);
        IconProperty = AvaloniaProperty.Register<LuminaBanner, object?>(nameof(Icon));
        CloseCommandProperty = AvaloniaProperty.Register<LuminaBanner, ICommand?>(nameof(CloseCommand));
        HasHeaderProperty = AvaloniaProperty.RegisterDirect<LuminaBanner, bool>(nameof(HasHeader), (LuminaBanner banner) => banner.HasHeader, null, unsetValue: false);
        EffectiveIconProperty = AvaloniaProperty.RegisterDirect("EffectiveIcon", (LuminaBanner banner) => banner.EffectiveIcon);
        HeaderedContentControl.HeaderProperty.Changed.AddClassHandler((LuminaBanner banner, AvaloniaPropertyChangedEventArgs _) =>
        {
            banner.UpdateState();
        });
        IconProperty.Changed.AddClassHandler((LuminaBanner banner, AvaloniaPropertyChangedEventArgs _) =>
        {
            banner.UpdateState();
        });
        TypeProperty.Changed.AddClassHandler((LuminaBanner banner, AvaloniaPropertyChangedEventArgs _) =>
        {
            banner.UpdateState();
        });
    }

    public LuminaBanner()
    {
        UpdateState();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_closeButton != null)
        {
            _closeButton.Click -= OnCloseButtonClick;
        }
        _closeButton = e.NameScope.FindRequired<Button>("PART_CloseButton");
        if (_closeButton != null)
        {
            _closeButton.Click += OnCloseButtonClick;
        }
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        IsVisible = false;
        ICommand? command = CloseCommand;
        if (command != null && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private void UpdateState()
    {
        HasHeader = Header != null;
        EffectiveIcon = Icon ?? Type switch
        {
            LuminaBannerType.Success => "OK", 
            LuminaBannerType.Warning => "!", 
            LuminaBannerType.Danger => "!", 
            _ => "i", 
        };
    }
}
