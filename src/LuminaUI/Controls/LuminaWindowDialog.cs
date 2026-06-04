using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using LuminaUI.Localization;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace LuminaUI.Controls;

public class LuminaWindowDialog : LuminaWindow
{
    public static readonly StyledProperty<bool> ShowFooterProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, bool>(nameof(ShowFooter), defaultValue: true);

    public static readonly StyledProperty<string> ConfirmButtonTextProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, string>(nameof(ConfirmButtonText), defaultValue: string.Empty);

    public static readonly StyledProperty<string> CancelButtonTextProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, string>(nameof(CancelButtonText), defaultValue: string.Empty);

    public static readonly StyledProperty<string> ConfirmButtonThemeProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, string>(nameof(ConfirmButtonTheme), defaultValue: "Primary");

    public static readonly StyledProperty<bool> IsConfirmButtonEnabledProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, bool>(nameof(IsConfirmButtonEnabled), defaultValue: true);

    public static readonly StyledProperty<LuminaDialogButtons> ButtonsProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, LuminaDialogButtons>(nameof(Buttons), defaultValue: LuminaDialogButtons.OkCancel);

    private readonly List<(Button Button, LuminaDialogResult Result)> _footerButtons = new();

    public bool ShowFooter
    {
        get => GetValue(ShowFooterProperty);
        set => SetValue(ShowFooterProperty, value);
    }

    public string ConfirmButtonText
    {
        get => GetValue(ConfirmButtonTextProperty);
        set => SetValue(ConfirmButtonTextProperty, value);
    }

    public string CancelButtonText
    {
        get => GetValue(CancelButtonTextProperty);
        set => SetValue(CancelButtonTextProperty, value);
    }

    public string ConfirmButtonTheme
    {
        get => GetValue(ConfirmButtonThemeProperty);
        set => SetValue(ConfirmButtonThemeProperty, value);
    }

    public bool IsConfirmButtonEnabled
    {
        get => GetValue(IsConfirmButtonEnabledProperty);
        set => SetValue(IsConfirmButtonEnabledProperty, value);
    }

    public LuminaDialogButtons Buttons
    {
        get => GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
    }

    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, object?>(nameof(Footer));

    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> FooterTemplateProperty =
        AvaloniaProperty.Register<LuminaWindowDialog, Avalonia.Controls.Templates.IDataTemplate?>(nameof(FooterTemplate));

    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public Avalonia.Controls.Templates.IDataTemplate? FooterTemplate
    {
        get => GetValue(FooterTemplateProperty);
        set => SetValue(FooterTemplateProperty, value);
    }

    public ICommand ConfirmCommand { get; }
    
    public ICommand CancelCommand { get; }

    public ICommand DialogResultCommand { get; }

    protected override Type StyleKeyOverride => typeof(LuminaWindowDialog);

    public LuminaWindowDialog()
    {
        ShowMinimizeButton = false;
        ShowMaximizeButton = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        WindowChromeMode = LuminaWindowChromeMode.Extended;

        this.Bind(ContentBackgroundProperty, new DynamicResourceExtension("LuminaSurfaceElevatedBrush"));
        this.Bind(GlassContentBackgroundProperty, new DynamicResourceExtension("LuminaSurfaceElevatedBrush"));

        var titleText = new Avalonia.Controls.TextBlock
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontSize = 12,
            Margin = new Avalonia.Thickness(0, 0, 0, 0)
        };
        titleText.Bind(Avalonia.Controls.TextBlock.TextProperty, this.GetObservable(TitleProperty));
        TitleBarLeftContent = titleText;

        ConfirmCommand = new LuminaRelayCommand(_ => Confirm(), _ => IsConfirmButtonEnabled);
        CancelCommand = new LuminaRelayCommand(_ => Cancel());
        DialogResultCommand = new LuminaRelayCommand(param => 
        {
            if (param is LuminaDialogResult result)
                Close(result);
            else
                Close(param);
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LuminaLocalization.LanguageChanged -= OnLanguageChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshFooterTexts();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        if (Content is Grid rootSurface && rootSurface.Name == "PART_RootSurface")
        {
            if (rootSurface.RowDefinitions.Count < 3)
            {
                rootSurface.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                
                var footerContainer = new ContentControl();
                footerContainer.Bind(ContentControl.ContentProperty, this.GetObservable(FooterProperty));
                footerContainer.Bind(ContentControl.ContentTemplateProperty, this.GetObservable(FooterTemplateProperty));
                footerContainer.Bind(IsVisibleProperty, this.GetObservable(ShowFooterProperty));

                if (Footer == null && FooterTemplate == null && Buttons != LuminaDialogButtons.Custom)
                {
                    Footer = CreateDefaultFooter();
                }

                Grid.SetRow(footerContainer, 2);
                rootSurface.Children.Add(footerContainer);
            }
        }
    }

    private object CreateDefaultFooter()
    {
        var border = new Border
        {
            Padding = new Thickness(24, 8),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Avalonia.Media.Brushes.Transparent
        };
        border.Bind(Border.BorderBrushProperty, new DynamicResourceExtension("LuminaBorderDefaultBrush"));

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        _footerButtons.Clear();

        string confirmTheme = string.IsNullOrEmpty(ConfirmButtonTheme) ? "Primary" : ConfirmButtonTheme;

        void AddButton(LuminaDialogResult result, string themeClass, ICommand command)
        {
            var btn = new Button
            {
                Content = GetButtonText(result),
                Command = command,
                CommandParameter = result
            };
            if (!string.IsNullOrEmpty(themeClass))
            {
                btn.Classes.Add(themeClass);
            }
            btn.Classes.Add("Small");
            _footerButtons.Add((btn, result));
            stack.Children.Add(btn);
        }

        switch (Buttons)
        {
            case LuminaDialogButtons.Ok:
                AddButton(LuminaDialogResult.Ok, confirmTheme, ConfirmCommand);
                break;
            case LuminaDialogButtons.OkCancel:
                AddButton(LuminaDialogResult.Cancel, "Outline", CancelCommand);
                AddButton(LuminaDialogResult.Ok, confirmTheme, ConfirmCommand);
                break;
            case LuminaDialogButtons.YesNo:
                AddButton(LuminaDialogResult.No, "Outline", DialogResultCommand);
                AddButton(LuminaDialogResult.Yes, confirmTheme, ConfirmCommand);
                break;
            case LuminaDialogButtons.YesNoCancel:
                AddButton(LuminaDialogResult.Cancel, "Outline", CancelCommand);
                AddButton(LuminaDialogResult.No, "Outline", DialogResultCommand);
                AddButton(LuminaDialogResult.Yes, confirmTheme, ConfirmCommand);
                break;
        }

        border.Child = stack;
        return border;
    }

    private void RefreshFooterTexts()
    {
        foreach ((Button button, LuminaDialogResult result) in _footerButtons)
        {
            button.Content = GetButtonText(result);
        }
    }

    private string GetButtonText(LuminaDialogResult result)
    {
        return result switch
        {
            LuminaDialogResult.Ok => string.IsNullOrEmpty(ConfirmButtonText)
                ? LuminaLocalization.Get(LuminaLocalizationKeys.CommonConfirm)
                : ConfirmButtonText,
            LuminaDialogResult.Cancel => string.IsNullOrEmpty(CancelButtonText)
                ? LuminaLocalization.Get(LuminaLocalizationKeys.CommonCancel)
                : CancelButtonText,
            LuminaDialogResult.Yes => LuminaLocalization.Get(LuminaLocalizationKeys.CommonYes),
            LuminaDialogResult.No => LuminaLocalization.Get(LuminaLocalizationKeys.CommonNo),
            _ => string.Empty
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsConfirmButtonEnabledProperty)
        {
            (ConfirmCommand as LuminaRelayCommand)?.RaiseCanExecuteChanged();
        }
        else if (change.Property == ConfirmButtonTextProperty || change.Property == CancelButtonTextProperty)
        {
            RefreshFooterTexts();
        }
    }

    protected virtual void Confirm()
    {
        Close(Buttons is LuminaDialogButtons.YesNo or LuminaDialogButtons.YesNoCancel
            ? LuminaDialogResult.Yes
            : LuminaDialogResult.Ok);
    }

    protected virtual void Cancel()
    {
        Close(LuminaDialogResult.Cancel);
    }
}
