using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

/// <summary>
/// Registers ordinary controls in the system back stack without requiring a navigation page.
/// </summary>
public sealed class LuminaBack : AvaloniaObject
{
    private static readonly ConditionalWeakTable<Control, RegistrationState> RegistrationStates = new ConditionalWeakTable<Control, RegistrationState>();

    public static readonly AttachedProperty<bool> IsEnabledProperty = AvaloniaProperty.RegisterAttached<LuminaBack, Control, bool>("IsEnabled", defaultValue: false);

    public static readonly AttachedProperty<ICommand?> CommandProperty = AvaloniaProperty.RegisterAttached<LuminaBack, Control, ICommand?>("Command");

    public static readonly AttachedProperty<object?> CommandParameterProperty = AvaloniaProperty.RegisterAttached<LuminaBack, Control, object?>("CommandParameter");

    static LuminaBack()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnRegistrationPropertyChanged);
        CommandProperty.Changed.AddClassHandler<Control>(OnRegistrationPropertyChanged);
    }

    private LuminaBack()
    {
    }

    public static bool GetIsEnabled(Control control)
    {
        return control.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(Control control, bool value)
    {
        control.SetValue(IsEnabledProperty, value);
    }

    public static ICommand? GetCommand(Control control)
    {
        return control.GetValue(CommandProperty);
    }

    public static void SetCommand(Control control, ICommand? value)
    {
        control.SetValue(CommandProperty, value);
    }

    public static object? GetCommandParameter(Control control)
    {
        return control.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(Control control, object? value)
    {
        control.SetValue(CommandParameterProperty, value);
    }

    private static void OnRegistrationPropertyChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        RegistrationStates.GetValue(control, static owner => new RegistrationState(owner)).Update();
    }

    private sealed class RegistrationState
    {
        private readonly Control _owner;

        private IDisposable? _registration;

        public RegistrationState(Control owner)
        {
            _owner = owner;
            _owner.AttachedToVisualTree += OnAttachedToVisualTree;
            _owner.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        public void Update()
        {
            _registration?.Dispose();
            _registration = null;

            if (!GetIsEnabled(_owner) || GetCommand(_owner) == null)
            {
                return;
            }

            LuminaBackDispatcher? dispatcher = LuminaBackDispatcher.FindFor(_owner);
            if (dispatcher != null)
            {
                _registration = dispatcher.Register(_owner, TryHandleBack);
            }
        }

        private bool TryHandleBack()
        {
            if (!GetIsEnabled(_owner))
            {
                return false;
            }

            ICommand? command = GetCommand(_owner);
            object? parameter = GetCommandParameter(_owner);
            if (command?.CanExecute(parameter) != true)
            {
                return false;
            }

            command.Execute(parameter);
            return true;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Update();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _registration?.Dispose();
            _registration = null;
        }
    }
}
