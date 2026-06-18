using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace LuminaUI.Controls;

public sealed class LuminaShortcut
{
    private sealed class ShortcutState
    {
        private readonly HashSet<LuminaShortcutBinding> _attachedShortcuts = new HashSet<LuminaShortcutBinding>();

        private InputElement? _element;

        private LuminaShortcutCollection? _shortcuts;

        public bool HasShortcuts => _shortcuts?.Count > 0;

        public LuminaShortcutCollection? Shortcuts => _shortcuts;

        public void SetShortcuts(InputElement element, LuminaShortcutCollection? shortcuts)
        {
            if (ReferenceEquals(_shortcuts, shortcuts))
            {
                return;
            }

            if (_shortcuts != null)
            {
                _shortcuts.CollectionChanged -= OnShortcutsCollectionChanged;
            }

            _element = element;
            _shortcuts = shortcuts;

            if (_shortcuts != null)
            {
                _shortcuts.CollectionChanged += OnShortcutsCollectionChanged;
            }

            SyncShortcutLogicalParents();
        }

        private void OnShortcutsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SyncShortcutLogicalParents();
            if (_element != null)
            {
                UpdateSubscription(_element);
            }
        }

        private void SyncShortcutLogicalParents()
        {
            LuminaShortcutBinding[] activeShortcuts = _shortcuts?.ToArray() ?? Array.Empty<LuminaShortcutBinding>();
            foreach (LuminaShortcutBinding shortcut in _attachedShortcuts.ToArray())
            {
                if (!activeShortcuts.Contains(shortcut))
                {
                    DetachShortcut(shortcut);
                }
            }

            foreach (LuminaShortcutBinding shortcut in activeShortcuts)
            {
                AttachShortcut(shortcut);
            }
        }

        private void AttachShortcut(LuminaShortcutBinding shortcut)
        {
            if (_element is ILogical logicalParent && _attachedShortcuts.Add(shortcut))
            {
                ((ISetLogicalParent)shortcut).SetParent(logicalParent);
            }
        }

        private void DetachShortcut(LuminaShortcutBinding shortcut)
        {
            if (_attachedShortcuts.Remove(shortcut))
            {
                ((ISetLogicalParent)shortcut).SetParent(null);
            }
        }
    }

    private static readonly AttachedProperty<bool> IsSubscribedProperty;

    private static readonly AttachedProperty<ShortcutState?> StateProperty;

    public static readonly AttachedProperty<KeyGesture?> GestureProperty;

    public static readonly AttachedProperty<IEnumerable<KeyGesture>?> GesturesProperty;

    public static readonly AttachedProperty<LuminaShortcutCollection?> ShortcutsProperty;

    public static readonly AttachedProperty<ICommand?> CommandProperty;

    public static readonly AttachedProperty<object?> CommandParameterProperty;

    public static readonly AttachedProperty<bool> IsEnabledProperty;

    public static readonly AttachedProperty<bool> PassEventArgsToCommandProperty;

    public static readonly AttachedProperty<bool> MarkHandledProperty;

    static LuminaShortcut()
    {
        IsSubscribedProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, bool>("IsSubscribed", defaultValue: false);
        StateProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, ShortcutState?>("State");
        GestureProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, KeyGesture?>("Gesture");
        GesturesProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, IEnumerable<KeyGesture>?>("Gestures");
        ShortcutsProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, LuminaShortcutCollection?>("Shortcuts");
        CommandProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, ICommand?>("Command");
        CommandParameterProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, object?>("CommandParameter");
        IsEnabledProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, bool>("IsEnabled", defaultValue: true);
        PassEventArgsToCommandProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, bool>("PassEventArgsToCommand", defaultValue: false);
        MarkHandledProperty = AvaloniaProperty.RegisterAttached<LuminaShortcut, InputElement, bool>("MarkHandled", defaultValue: true);

        GestureProperty.Changed.AddClassHandler<InputElement>((element, _) => UpdateSubscription(element));
        GesturesProperty.Changed.AddClassHandler<InputElement>((element, _) => UpdateSubscription(element));
        ShortcutsProperty.Changed.AddClassHandler<InputElement>((element, args) =>
        {
            GetOrCreateState(element).SetShortcuts(element, args.GetNewValue<LuminaShortcutCollection>());
            UpdateSubscription(element);
        });
        CommandProperty.Changed.AddClassHandler<InputElement>((element, _) => UpdateSubscription(element));
        IsEnabledProperty.Changed.AddClassHandler<InputElement>((element, _) => UpdateSubscription(element));
    }

    private LuminaShortcut()
    {
    }

    public static KeyGesture? GetGesture(InputElement element)
    {
        return element.GetValue(GestureProperty);
    }

    public static void SetGesture(InputElement element, KeyGesture? value)
    {
        element.SetValue(GestureProperty, value);
    }

    public static IEnumerable<KeyGesture>? GetGestures(InputElement element)
    {
        return element.GetValue(GesturesProperty);
    }

    public static void SetGestures(InputElement element, IEnumerable<KeyGesture>? value)
    {
        element.SetValue(GesturesProperty, value);
    }

    public static LuminaShortcutCollection GetShortcuts(InputElement element)
    {
        LuminaShortcutCollection? shortcuts = element.GetValue(ShortcutsProperty);
        if (shortcuts == null)
        {
            shortcuts = new LuminaShortcutCollection();
            element.SetValue(ShortcutsProperty, shortcuts);
        }

        return shortcuts;
    }

    public static void SetShortcuts(InputElement element, LuminaShortcutCollection? value)
    {
        element.SetValue(ShortcutsProperty, value);
    }

    public static ICommand? GetCommand(InputElement element)
    {
        return element.GetValue(CommandProperty);
    }

    public static void SetCommand(InputElement element, ICommand? value)
    {
        element.SetValue(CommandProperty, value);
    }

    public static object? GetCommandParameter(InputElement element)
    {
        return element.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(InputElement element, object? value)
    {
        element.SetValue(CommandParameterProperty, value);
    }

    public static bool GetIsEnabled(InputElement element)
    {
        return element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(InputElement element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    public static bool GetPassEventArgsToCommand(InputElement element)
    {
        return element.GetValue(PassEventArgsToCommandProperty);
    }

    public static void SetPassEventArgsToCommand(InputElement element, bool value)
    {
        element.SetValue(PassEventArgsToCommandProperty, value);
    }

    public static bool GetMarkHandled(InputElement element)
    {
        return element.GetValue(MarkHandledProperty);
    }

    public static void SetMarkHandled(InputElement element, bool value)
    {
        element.SetValue(MarkHandledProperty, value);
    }

    private static ShortcutState GetOrCreateState(InputElement element)
    {
        ShortcutState? state = element.GetValue(StateProperty);
        if (state == null)
        {
            state = new ShortcutState();
            element.SetValue(StateProperty, state);
        }

        return state;
    }

    private static ShortcutState? GetState(InputElement element)
    {
        return element.GetValue(StateProperty);
    }

    private static void UpdateSubscription(InputElement element)
    {
        bool shouldSubscribe = GetIsEnabled(element) && (HasDirectShortcut(element) || GetState(element)?.HasShortcuts == true);
        bool isSubscribed = element.GetValue(IsSubscribedProperty);

        if (shouldSubscribe == isSubscribed)
        {
            return;
        }

        if (shouldSubscribe)
        {
            element.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        }
        else
        {
            element.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        element.SetValue(IsSubscribedProperty, shouldSubscribe);
    }

    private static bool HasDirectShortcut(InputElement element)
    {
        return GetCommand(element) != null && LuminaShortcutBinding.HasGesture(GetGesture(element), GetGestures(element));
    }

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || sender is not InputElement element || !GetIsEnabled(element))
        {
            return;
        }

        if (TryExecuteDirectShortcut(element, e))
        {
            return;
        }

        LuminaShortcutCollection? shortcuts = GetState(element)?.Shortcuts;
        if (shortcuts == null)
        {
            return;
        }

        foreach (LuminaShortcutBinding shortcut in shortcuts.ToArray())
        {
            if (TryExecuteBinding(shortcut, e))
            {
                break;
            }
        }
    }

    private static bool TryExecuteDirectShortcut(InputElement element, KeyEventArgs e)
    {
        ICommand? command = GetCommand(element);
        if (command == null || !LuminaShortcutBinding.Matches(GetGesture(element), GetGestures(element), e))
        {
            return false;
        }

        object? parameter = ResolveParameter(element, e);
        if (!command.CanExecute(parameter))
        {
            return false;
        }

        command.Execute(parameter);
        if (GetMarkHandled(element))
        {
            e.Handled = true;
        }

        return true;
    }

    internal static bool TryExecuteBinding(LuminaShortcutBinding shortcut, KeyEventArgs e)
    {
        if (e.Handled || !shortcut.IsEnabled || shortcut.Command == null || !shortcut.Matches(e))
        {
            return false;
        }

        object? parameter = shortcut.ResolveParameter(e);
        if (!shortcut.Command.CanExecute(parameter))
        {
            return false;
        }

        shortcut.Command.Execute(parameter);
        if (shortcut.MarkHandled)
        {
            e.Handled = true;
        }

        return true;
    }

    private static object? ResolveParameter(InputElement element, KeyEventArgs e)
    {
        if (element.IsSet(CommandParameterProperty))
        {
            return element.GetValue(CommandParameterProperty);
        }

        return GetPassEventArgsToCommand(element) ? e : null;
    }
}
