using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace LuminaUI.Controls;

public sealed class LuminaButton
{
    public static readonly AttachedProperty<bool> IsLoadingProperty;

    public static readonly AttachedProperty<LuminaLoadingKind> LoadingKindProperty;

    public static readonly AttachedProperty<object?> IconProperty;

    public static readonly AttachedProperty<IDataTemplate?> IconTemplateProperty;

    public static readonly AttachedProperty<IBrush?> IconForegroundProperty;

    public static readonly AttachedProperty<bool> SyncIconForegroundProperty;

    public static readonly AttachedProperty<bool> SyncContentForegroundProperty;

    public static readonly AttachedProperty<LuminaIconPlacement> IconPlacementProperty;

    public static readonly AttachedProperty<double> IconSizeProperty;

    public static readonly AttachedProperty<double> IconSpacingProperty;

    public static readonly AttachedProperty<bool> HasIconProperty;

    public static readonly AttachedProperty<bool> HasContentProperty;

    private LuminaButton()
    {
    }

    static LuminaButton()
    {
        IsLoadingProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, bool>("IsLoading", defaultValue: false);
        LoadingKindProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, LuminaLoadingKind>("LoadingKind", LuminaLoadingKind.Ring);
        IconProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, object?>("Icon");
        IconTemplateProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, IDataTemplate?>("IconTemplate");
        IconForegroundProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, IBrush?>("IconForeground");
        SyncIconForegroundProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, bool>("SyncIconForeground", defaultValue: true);
        SyncContentForegroundProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, bool>("SyncContentForeground", defaultValue: true);
        IconPlacementProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, LuminaIconPlacement>("IconPlacement", LuminaIconPlacement.Left);
        IconSizeProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, double>("IconSize", 16.0);
        IconSpacingProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, double>("IconSpacing", 8.0);
        HasIconProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, bool>("HasIcon", defaultValue: false);
        HasContentProperty = AvaloniaProperty.RegisterAttached<LuminaButton, Button, bool>("HasContent", defaultValue: false);
        IconProperty.Changed.AddClassHandler((Button button, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateState(button);
        });
        ContentControl.ContentProperty.Changed.AddClassHandler((Button button, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateState(button);
        });
    }

    public static bool GetIsLoading(Button button)
    {
        return button.GetValue(IsLoadingProperty);
    }

    public static void SetIsLoading(Button button, bool value)
    {
        button.SetValue(IsLoadingProperty, value);
    }

    public static LuminaLoadingKind GetLoadingKind(Button button)
    {
        return button.GetValue(LoadingKindProperty);
    }

    public static void SetLoadingKind(Button button, LuminaLoadingKind value)
    {
        button.SetValue(LoadingKindProperty, value);
    }

    public static object? GetIcon(Button button)
    {
        return button.GetValue(IconProperty);
    }

    public static void SetIcon(Button button, object? value)
    {
        button.SetValue(IconProperty, value);
    }

    public static IDataTemplate? GetIconTemplate(Button button)
    {
        return button.GetValue(IconTemplateProperty);
    }

    public static void SetIconTemplate(Button button, IDataTemplate? value)
    {
        button.SetValue(IconTemplateProperty, value);
    }

    public static IBrush? GetIconForeground(Button button)
    {
        return button.GetValue(IconForegroundProperty);
    }

    public static void SetIconForeground(Button button, IBrush? value)
    {
        button.SetValue(IconForegroundProperty, value);
    }

    public static bool GetSyncIconForeground(Button button)
    {
        return button.GetValue(SyncIconForegroundProperty);
    }

    public static void SetSyncIconForeground(Button button, bool value)
    {
        button.SetValue(SyncIconForegroundProperty, value);
    }

    public static bool GetSyncContentForeground(Button button)
    {
        return button.GetValue(SyncContentForegroundProperty);
    }

    public static void SetSyncContentForeground(Button button, bool value)
    {
        button.SetValue(SyncContentForegroundProperty, value);
    }

    public static LuminaIconPlacement GetIconPlacement(Button button)
    {
        return button.GetValue(IconPlacementProperty);
    }

    public static void SetIconPlacement(Button button, LuminaIconPlacement value)
    {
        button.SetValue(IconPlacementProperty, value);
    }

    public static double GetIconSize(Button button)
    {
        return button.GetValue(IconSizeProperty);
    }

    public static void SetIconSize(Button button, double value)
    {
        button.SetValue(IconSizeProperty, value);
    }

    public static double GetIconSpacing(Button button)
    {
        return button.GetValue(IconSpacingProperty);
    }

    public static void SetIconSpacing(Button button, double value)
    {
        button.SetValue(IconSpacingProperty, value);
    }

    public static bool GetHasIcon(Button button)
    {
        return button.GetValue(HasIconProperty);
    }

    private static void SetHasIcon(Button button, bool value)
    {
        button.SetValue(HasIconProperty, value);
    }

    public static bool GetHasContent(Button button)
    {
        return button.GetValue(HasContentProperty);
    }

    private static void SetHasContent(Button button, bool value)
    {
        button.SetValue(HasContentProperty, value);
    }

    private static void UpdateState(Button button)
    {
        bool hasIcon = GetIcon(button) != null;
        bool hasContent = HasDisplayContent(button.Content);
        SetHasIcon(button, hasIcon);
        SetHasContent(button, hasContent);
        SetClass(button, "LuminaIconOnly", hasIcon && !hasContent);
    }

    private static bool HasDisplayContent(object? content)
    {
        return content != null && (content is not string text || !string.IsNullOrWhiteSpace(text));
    }

    private static void SetClass(Button button, string className, bool enabled)
    {
        if (enabled)
        {
            if (!button.Classes.Contains(className))
            {
                button.Classes.Add(className);
            }
        }
        else
        {
            button.Classes.Remove(className);
        }
    }
}
