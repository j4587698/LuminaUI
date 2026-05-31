#define DEBUG
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace LuminaUI.Controls;

public sealed class LuminaCommand : AvaloniaObject
{
    private static readonly AttachedProperty<bool> IsClickSubscribedProperty;

    private static readonly AttachedProperty<bool> IsTextChangedSubscribedProperty;

    private static readonly AttachedProperty<bool> IsSelectionChangedSubscribedProperty;

    private static readonly AttachedProperty<bool> IsTappedSubscribedProperty;

    private static readonly AttachedProperty<bool> IsLoadedSubscribedProperty;

    public static readonly AttachedProperty<ICommand?> ClickCommandProperty;

    public static readonly AttachedProperty<object?> ClickCommandParameterProperty;

    public static readonly AttachedProperty<ICommand?> TextChangedCommandProperty;

    public static readonly AttachedProperty<object?> TextChangedCommandParameterProperty;

    public static readonly AttachedProperty<ICommand?> SelectionChangedCommandProperty;

    public static readonly AttachedProperty<object?> SelectionChangedCommandParameterProperty;

    public static readonly AttachedProperty<ICommand?> TappedCommandProperty;

    public static readonly AttachedProperty<object?> TappedCommandParameterProperty;

    public static readonly AttachedProperty<ICommand?> LoadedCommandProperty;

    public static readonly AttachedProperty<object?> LoadedCommandParameterProperty;

    public static readonly AttachedProperty<bool> PassEventArgsToCommandProperty;

    public static readonly AttachedProperty<bool> MarkHandledProperty;

    static LuminaCommand()
    {
        IsClickSubscribedProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, bool>("IsClickSubscribed", defaultValue: false);
        IsTextChangedSubscribedProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, bool>("IsTextChangedSubscribed", defaultValue: false);
        IsSelectionChangedSubscribedProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, bool>("IsSelectionChangedSubscribed", defaultValue: false);
        IsTappedSubscribedProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, bool>("IsTappedSubscribed", defaultValue: false);
        IsLoadedSubscribedProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, bool>("IsLoadedSubscribed", defaultValue: false);
        ClickCommandProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, ICommand?>("ClickCommand");
        ClickCommandParameterProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, object?>("ClickCommandParameter");
        TextChangedCommandProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, ICommand?>("TextChangedCommand");
        TextChangedCommandParameterProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, object?>("TextChangedCommandParameter");
        SelectionChangedCommandProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, ICommand?>("SelectionChangedCommand");
        SelectionChangedCommandParameterProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, object?>("SelectionChangedCommandParameter");
        TappedCommandProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, ICommand?>("TappedCommand");
        TappedCommandParameterProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, object?>("TappedCommandParameter");
        LoadedCommandProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, ICommand?>("LoadedCommand");
        LoadedCommandParameterProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, object?>("LoadedCommandParameter");
        PassEventArgsToCommandProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, bool>("PassEventArgsToCommand", defaultValue: false);
        MarkHandledProperty = AvaloniaProperty.RegisterAttached<LuminaCommand, AvaloniaObject, bool>("MarkHandled", defaultValue: false);
        ClickCommandProperty.Changed.AddClassHandler((AvaloniaObject target, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateClickSubscription(target);
        });
        TextChangedCommandProperty.Changed.AddClassHandler((AvaloniaObject target, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateTextChangedSubscription(target);
        });
        SelectionChangedCommandProperty.Changed.AddClassHandler((AvaloniaObject target, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateSelectionChangedSubscription(target);
        });
        TappedCommandProperty.Changed.AddClassHandler((AvaloniaObject target, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateTappedSubscription(target);
        });
        LoadedCommandProperty.Changed.AddClassHandler((AvaloniaObject target, AvaloniaPropertyChangedEventArgs _) =>
        {
            UpdateLoadedSubscription(target);
        });
    }

    private LuminaCommand()
    {
    }

    public static ICommand? GetClickCommand(AvaloniaObject target)
    {
        return target.GetValue(ClickCommandProperty);
    }

    public static void SetClickCommand(AvaloniaObject target, ICommand? value)
    {
        target.SetValue(ClickCommandProperty, value);
    }

    public static object? GetClickCommandParameter(AvaloniaObject target)
    {
        return target.GetValue(ClickCommandParameterProperty);
    }

    public static void SetClickCommandParameter(AvaloniaObject target, object? value)
    {
        target.SetValue(ClickCommandParameterProperty, value);
    }

    public static ICommand? GetTextChangedCommand(AvaloniaObject target)
    {
        return target.GetValue(TextChangedCommandProperty);
    }

    public static void SetTextChangedCommand(AvaloniaObject target, ICommand? value)
    {
        target.SetValue(TextChangedCommandProperty, value);
    }

    public static object? GetTextChangedCommandParameter(AvaloniaObject target)
    {
        return target.GetValue(TextChangedCommandParameterProperty);
    }

    public static void SetTextChangedCommandParameter(AvaloniaObject target, object? value)
    {
        target.SetValue(TextChangedCommandParameterProperty, value);
    }

    public static ICommand? GetSelectionChangedCommand(AvaloniaObject target)
    {
        return target.GetValue(SelectionChangedCommandProperty);
    }

    public static void SetSelectionChangedCommand(AvaloniaObject target, ICommand? value)
    {
        target.SetValue(SelectionChangedCommandProperty, value);
    }

    public static object? GetSelectionChangedCommandParameter(AvaloniaObject target)
    {
        return target.GetValue(SelectionChangedCommandParameterProperty);
    }

    public static void SetSelectionChangedCommandParameter(AvaloniaObject target, object? value)
    {
        target.SetValue(SelectionChangedCommandParameterProperty, value);
    }

    public static ICommand? GetTappedCommand(AvaloniaObject target)
    {
        return target.GetValue(TappedCommandProperty);
    }

    public static void SetTappedCommand(AvaloniaObject target, ICommand? value)
    {
        target.SetValue(TappedCommandProperty, value);
    }

    public static object? GetTappedCommandParameter(AvaloniaObject target)
    {
        return target.GetValue(TappedCommandParameterProperty);
    }

    public static void SetTappedCommandParameter(AvaloniaObject target, object? value)
    {
        target.SetValue(TappedCommandParameterProperty, value);
    }

    public static ICommand? GetLoadedCommand(AvaloniaObject target)
    {
        return target.GetValue(LoadedCommandProperty);
    }

    public static void SetLoadedCommand(AvaloniaObject target, ICommand? value)
    {
        target.SetValue(LoadedCommandProperty, value);
    }

    public static object? GetLoadedCommandParameter(AvaloniaObject target)
    {
        return target.GetValue(LoadedCommandParameterProperty);
    }

    public static void SetLoadedCommandParameter(AvaloniaObject target, object? value)
    {
        target.SetValue(LoadedCommandParameterProperty, value);
    }

    public static bool GetPassEventArgsToCommand(AvaloniaObject target)
    {
        return target.GetValue(PassEventArgsToCommandProperty);
    }

    public static void SetPassEventArgsToCommand(AvaloniaObject target, bool value)
    {
        target.SetValue(PassEventArgsToCommandProperty, value);
    }

    public static bool GetMarkHandled(AvaloniaObject target)
    {
        return target.GetValue(MarkHandledProperty);
    }

    public static void SetMarkHandled(AvaloniaObject target, bool value)
    {
        target.SetValue(MarkHandledProperty, value);
    }

    private static void UpdateClickSubscription(AvaloniaObject target)
    {
        bool shouldSubscribe = GetClickCommand(target) != null;
        if (target.GetValue(IsClickSubscribedProperty) == shouldSubscribe)
        {
            return;
        }
        if (target is Button button)
        {
            if (shouldSubscribe)
            {
                button.Click += OnClick;
            }
            else
            {
                button.Click -= OnClick;
            }
            target.SetValue(IsClickSubscribedProperty, shouldSubscribe);
        }
        else if (target is MenuItem menuItem)
        {
            if (shouldSubscribe)
            {
                menuItem.Click += OnClick;
            }
            else
            {
                menuItem.Click -= OnClick;
            }
            target.SetValue(IsClickSubscribedProperty, shouldSubscribe);
        }
        else if (shouldSubscribe)
        {
            Debug.WriteLine("LuminaCommand: ClickCommand is not supported on '" + target.GetType().Name + "'.");
        }
    }

    private static void UpdateTextChangedSubscription(AvaloniaObject target)
    {
        bool shouldSubscribe = GetTextChangedCommand(target) != null;
        if (target.GetValue(IsTextChangedSubscribedProperty) == shouldSubscribe)
        {
            return;
        }
        if (target is TextBox textBox)
        {
            if (shouldSubscribe)
            {
                textBox.TextChanged += OnTextChanged;
            }
            else
            {
                textBox.TextChanged -= OnTextChanged;
            }
            target.SetValue(IsTextChangedSubscribedProperty, shouldSubscribe);
        }
        else if (target is AutoCompleteBox autoCompleteBox)
        {
            if (shouldSubscribe)
            {
                autoCompleteBox.TextChanged += OnTextChanged;
            }
            else
            {
                autoCompleteBox.TextChanged -= OnTextChanged;
            }
            target.SetValue(IsTextChangedSubscribedProperty, shouldSubscribe);
        }
        else if (shouldSubscribe)
        {
            Debug.WriteLine("LuminaCommand: TextChangedCommand is not supported on '" + target.GetType().Name + "'.");
        }
    }

    private static void UpdateSelectionChangedSubscription(AvaloniaObject target)
    {
        bool shouldSubscribe = GetSelectionChangedCommand(target) != null;
        if (target.GetValue(IsSelectionChangedSubscribedProperty) == shouldSubscribe)
        {
            return;
        }
        if (target is SelectingItemsControl selectingItemsControl)
        {
            if (shouldSubscribe)
            {
                selectingItemsControl.SelectionChanged += OnSelectionChanged;
            }
            else
            {
                selectingItemsControl.SelectionChanged -= OnSelectionChanged;
            }
            target.SetValue(IsSelectionChangedSubscribedProperty, shouldSubscribe);
        }
        else if (target is LuminaNavigationView navigationView)
        {
            if (shouldSubscribe)
            {
                navigationView.SelectionChanged += OnNavigationSelectionChanged;
            }
            else
            {
                navigationView.SelectionChanged -= OnNavigationSelectionChanged;
            }
            target.SetValue(IsSelectionChangedSubscribedProperty, shouldSubscribe);
        }
        else if (shouldSubscribe)
        {
            Debug.WriteLine("LuminaCommand: SelectionChangedCommand is not supported on '" + target.GetType().Name + "'.");
        }
    }

    private static void UpdateTappedSubscription(AvaloniaObject target)
    {
        bool shouldSubscribe = GetTappedCommand(target) != null;
        if (target.GetValue(IsTappedSubscribedProperty) == shouldSubscribe)
        {
            return;
        }
        if (target is InputElement inputElement)
        {
            if (shouldSubscribe)
            {
                inputElement.Tapped += OnTapped;
            }
            else
            {
                inputElement.Tapped -= OnTapped;
            }
            target.SetValue(IsTappedSubscribedProperty, shouldSubscribe);
        }
        else if (shouldSubscribe)
        {
            Debug.WriteLine("LuminaCommand: TappedCommand is not supported on '" + target.GetType().Name + "'.");
        }
    }

    private static void UpdateLoadedSubscription(AvaloniaObject target)
    {
        bool shouldSubscribe = GetLoadedCommand(target) != null;
        if (target.GetValue(IsLoadedSubscribedProperty) == shouldSubscribe)
        {
            return;
        }
        if (target is Control control)
        {
            if (shouldSubscribe)
            {
                control.Loaded += OnLoaded;
            }
            else
            {
                control.Loaded -= OnLoaded;
            }
            target.SetValue(IsLoadedSubscribedProperty, shouldSubscribe);
        }
        else if (shouldSubscribe)
        {
            Debug.WriteLine("LuminaCommand: LoadedCommand is not supported on '" + target.GetType().Name + "'.");
        }
    }

    private static void OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is AvaloniaObject target)
        {
            Execute(target, ClickCommandProperty, ClickCommandParameterProperty, e);
        }
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is AvaloniaObject target)
        {
            Execute(target, TextChangedCommandProperty, TextChangedCommandParameterProperty, e);
        }
    }

    private static void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is AvaloniaObject target)
        {
            Execute(target, SelectionChangedCommandProperty, SelectionChangedCommandParameterProperty, e);
        }
    }

    private static void OnNavigationSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is AvaloniaObject target)
        {
            Execute(target, SelectionChangedCommandProperty, SelectionChangedCommandParameterProperty, e);
        }
    }

    private static void OnTapped(object? sender, TappedEventArgs e)
    {
        if (sender is AvaloniaObject target)
        {
            Execute(target, TappedCommandProperty, TappedCommandParameterProperty, e);
        }
    }

    private static void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is AvaloniaObject target)
        {
            Execute(target, LoadedCommandProperty, LoadedCommandParameterProperty, e);
        }
    }

    private static void Execute(AvaloniaObject target, AttachedProperty<ICommand?> commandProperty, AttachedProperty<object?> parameterProperty, RoutedEventArgs eventArgs)
    {
        ICommand? command = target.GetValue(commandProperty);
        if (command == null)
        {
            return;
        }
        object? parameter = ResolveParameter(target, parameterProperty, eventArgs);
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
            if (GetMarkHandled(target))
            {
                eventArgs.Handled = true;
            }
        }
    }

    private static object? ResolveParameter(AvaloniaObject target, AttachedProperty<object?> parameterProperty, RoutedEventArgs eventArgs)
    {
        if (target.IsSet(parameterProperty))
        {
            return target.GetValue(parameterProperty);
        }
        return GetPassEventArgsToCommand(target) ? eventArgs : null;
    }
}
