using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Input;

namespace LuminaUI.Controls;

public class LuminaShortcutBinding : StyledElement
{
    public static readonly StyledProperty<KeyGesture?> GestureProperty =
        AvaloniaProperty.Register<LuminaShortcutBinding, KeyGesture?>(nameof(Gesture));

    public static readonly StyledProperty<IEnumerable<KeyGesture>?> GesturesProperty =
        AvaloniaProperty.Register<LuminaShortcutBinding, IEnumerable<KeyGesture>?>(nameof(Gestures));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<LuminaShortcutBinding, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<LuminaShortcutBinding, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<LuminaShortcutBinding, bool>(nameof(IsEnabled), defaultValue: true);

    public static readonly StyledProperty<bool> PassEventArgsToCommandProperty =
        AvaloniaProperty.Register<LuminaShortcutBinding, bool>(nameof(PassEventArgsToCommand), defaultValue: false);

    public static readonly StyledProperty<bool> MarkHandledProperty =
        AvaloniaProperty.Register<LuminaShortcutBinding, bool>(nameof(MarkHandled), defaultValue: true);

    public KeyGesture? Gesture
    {
        get => GetValue(GestureProperty);
        set => SetValue(GestureProperty, value);
    }

    public IEnumerable<KeyGesture>? Gestures
    {
        get => GetValue(GesturesProperty);
        set => SetValue(GesturesProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public bool PassEventArgsToCommand
    {
        get => GetValue(PassEventArgsToCommandProperty);
        set => SetValue(PassEventArgsToCommandProperty, value);
    }

    public bool MarkHandled
    {
        get => GetValue(MarkHandledProperty);
        set => SetValue(MarkHandledProperty, value);
    }

    internal bool Matches(KeyEventArgs e)
    {
        return Matches(Gesture, Gestures, e);
    }

    internal object? ResolveParameter(KeyEventArgs e)
    {
        if (IsSet(CommandParameterProperty))
        {
            return CommandParameter;
        }

        return PassEventArgsToCommand ? e : null;
    }

    internal static bool HasGesture(KeyGesture? gesture, IEnumerable<KeyGesture>? gestures)
    {
        return gesture != null || gestures != null;
    }

    internal static bool Matches(KeyGesture? gesture, IEnumerable<KeyGesture>? gestures, KeyEventArgs e)
    {
        if (LuminaKeyGestureDetector.Matches(gesture, e))
        {
            return true;
        }

        if (gestures == null)
        {
            return false;
        }

        foreach (KeyGesture shortcutGesture in gestures)
        {
            if (LuminaKeyGestureDetector.Matches(shortcutGesture, e))
            {
                return true;
            }
        }

        return false;
    }
}

public class LuminaShortcutCollection : AvaloniaList<LuminaShortcutBinding>
{
}
