using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Inspection;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Interaction;

public sealed class InteractionHandler : IDiagnosticToolHandler
{
    private readonly InteractionKind _kind;
    private readonly IUiThreadInvoker _invoker;
    private readonly IControlResolver _controlResolver;
    private readonly PropertyValueConverter _valueConverter;
    private readonly Func<IReadOnlyList<Control>> _getRoots;

    private InteractionHandler(
        InteractionKind kind,
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        PropertyValueConverter? valueConverter = null,
        Func<IReadOnlyList<Control>>? getRoots = null)
    {
        _kind = kind;
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        _controlResolver = controlResolver ?? new AvaloniaControlResolver();
        _valueConverter = valueConverter ?? new PropertyValueConverter();
        _getRoots = getRoots ?? InspectionRequestHelpers.GetCurrentWindowRoots;
    }

    public string Method =>
        _kind switch
        {
            InteractionKind.Click => LuminaDiagnosticsToolNames.ClickControl,
            InteractionKind.SetProperty => LuminaDiagnosticsToolNames.SetProperty,
            InteractionKind.InputText => LuminaDiagnosticsToolNames.InputText,
            InteractionKind.InvokeCommand => LuminaDiagnosticsToolNames.InvokeCommand,
            InteractionKind.WaitForProperty => LuminaDiagnosticsToolNames.WaitForProperty,
            _ => throw new InvalidOperationException("Unknown interaction kind.")
        };

    public static InteractionHandler ClickControl(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(InteractionKind.Click, invoker, controlResolver, getRoots: getRoots);

    public static InteractionHandler SetProperty(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        PropertyValueConverter? valueConverter = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(InteractionKind.SetProperty, invoker, controlResolver, valueConverter, getRoots);

    public static InteractionHandler InputText(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(InteractionKind.InputText, invoker, controlResolver, getRoots: getRoots);

    public static InteractionHandler InvokeCommand(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(InteractionKind.InvokeCommand, invoker, controlResolver, getRoots: getRoots);

    public static InteractionHandler WaitForProperty(
        IUiThreadInvoker invoker,
        IControlResolver? controlResolver = null,
        Func<IReadOnlyList<Control>>? getRoots = null) =>
        new(InteractionKind.WaitForProperty, invoker, controlResolver, getRoots: getRoots);

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default) =>
        _invoker.InvokeAsync(
            request,
            token => HandleOnUiThreadAsync(request, token),
            cancellationToken);

    private Task<DiagnosticResponse> HandleOnUiThreadAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken) =>
        _kind switch
        {
            InteractionKind.Click => Task.FromResult(Click(request)),
            InteractionKind.SetProperty => Task.FromResult(SetProperty(request)),
            InteractionKind.InputText => InputTextAsync(request, cancellationToken),
            InteractionKind.InvokeCommand => Task.FromResult(InvokeCommand(request)),
            InteractionKind.WaitForProperty => WaitForPropertyAsync(request, cancellationToken),
            _ => Task.FromResult(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.UnsupportedOperation,
                    "Unsupported interaction kind."))
        };

    private DiagnosticResponse Click(DiagnosticRequest request)
    {
        var lookup = ResolveRequiredControl(request);
        if (!lookup.Success)
            return lookup.Response!;

        var control = lookup.Control!;
        control.Focus();

        if (control is Button button)
        {
            if (button.Command is null)
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return Ok(request, "clickRaised");
            }

            var command = button.Command;
            var parameter = button.CommandParameter;
            if (!command.CanExecute(parameter))
            {
                return DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.UnsupportedOperation,
                    "Button command cannot execute.");
            }

            command.Execute(parameter);
            return Ok(request, "commandExecuted");
        }

        if (TryToggleExpandableControl(control, out var expanded))
        {
            return Ok(request, expanded ? "expanded" : "collapsed");
        }

        var commandResponse = TryExecuteCommand(request, control);
        if (commandResponse is not null)
        {
            return commandResponse;
        }

        if (TryRaiseStaticRoutedEvent(control, "InvokedEvent"))
        {
            return Ok(request, "invokedRaised");
        }

        return Ok(request, "focused");
    }

    private DiagnosticResponse SetProperty(DiagnosticRequest request)
    {
        var lookup = ResolveRequiredControl(request);
        if (!lookup.Success)
            return lookup.Response!;

        var propertyName = InspectionRequestHelpers.GetString(request.Parameters, "propertyName");
        var rawValue = InspectionRequestHelpers.GetString(request.Parameters, "value");
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.InvalidRequest,
                "Parameter 'propertyName' is required.");
        }

        var property = FindAvaloniaProperty(lookup.Control!, propertyName);
        if (property is null)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                $"Avalonia property '{propertyName}' was not found.");
        }

        if (!_valueConverter.TryConvert(rawValue, property.PropertyType, out var converted, out var error))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.ConversionFailed,
                error ?? $"Could not convert value to {property.PropertyType.Name}.");
        }

        lookup.Control!.SetValue(property, converted);
        return Ok(request, "propertySet");
    }

    private async Task<DiagnosticResponse> InputTextAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken)
    {
        var lookup = ResolveRequiredControl(request);
        if (!lookup.Success)
            return lookup.Response!;

        var text = InspectionRequestHelpers.GetString(request.Parameters, "text") ?? "";
        var pressEnter = InspectionRequestHelpers.GetBool(request.Parameters, "pressEnter");
        var textBox = lookup.Control as TextBox;
        if (textBox is not null && !IsEditableTextBox(textBox))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                "Target TextBox is not editable.");
        }

        textBox ??= AvaloniaControlResolver.EnumerateControls(lookup.Control!)
            .OfType<TextBox>()
            .FirstOrDefault(IsEditableTextBox);

        if (textBox is null)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                "Target control is not a TextBox and does not contain an editable TextBox.");
        }

        textBox.Focus();
        textBox.Text = text;

        var enterPressed = false;
        if (pressEnter)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Dispatcher.UIThread.InvokeAsync(() => RaiseReturnKey(textBox), DispatcherPriority.Background);
            cancellationToken.ThrowIfCancellationRequested();
            enterPressed = true;
        }

        return DiagnosticResponse.Ok(
            request.Id,
            new JsonObject
            {
                ["status"] = "textSet",
                ["pressEnterRequested"] = pressEnter,
                ["enterPressed"] = enterPressed
            });
    }

    private DiagnosticResponse InvokeCommand(DiagnosticRequest request)
    {
        var lookup = ResolveRequiredControl(request);
        if (!lookup.Success)
            return lookup.Response!;

        var commandName = InspectionRequestHelpers.GetString(request.Parameters, "commandName");
        var parameter = InspectionRequestHelpers.GetString(request.Parameters, "parameter");
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.InvalidRequest,
                "Parameter 'commandName' is required.");
        }

        var dataContext = lookup.Control!.DataContext;
        var command = dataContext?.GetType()
            .GetProperty(commandName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(dataContext) as ICommand;

        if (command is null)
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                $"Command '{commandName}' was not found.");
        }

        if (!command.CanExecute(parameter))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                $"Command '{commandName}' cannot execute.");
        }

        command.Execute(parameter);
        return Ok(request, "commandExecuted");
    }

    private async Task<DiagnosticResponse> WaitForPropertyAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken)
    {
        var lookup = ResolveOptionalControl(request);
        if (!lookup.Success)
            return lookup.Response!;

        var propertyName = InspectionRequestHelpers.GetString(request.Parameters, "propertyName");
        var expectedValue = InspectionRequestHelpers.GetString(request.Parameters, "expectedValue");
        if (string.IsNullOrWhiteSpace(propertyName))
            return DiagnosticResponse.Fail(request.Id, DiagnosticErrorCode.InvalidRequest, "Parameter 'propertyName' is required.");

        var timeoutMs = Math.Clamp(
            InspectionRequestHelpers.GetInt(request.Parameters, "timeoutMs", request.TimeoutMs),
            1,
            600_000);
        var pollIntervalMs = Math.Clamp(
            InspectionRequestHelpers.GetInt(request.Parameters, "pollIntervalMs", 500),
            10,
            10_000);
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTimeOffset.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = ReadPropertyValue(lookup.Control!, propertyName);
            var text = Convert.ToString(current, CultureInfo.InvariantCulture);

            if (string.Equals(text, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return DiagnosticResponse.Ok(
                    request.Id,
                    new JsonObject
                    {
                        ["status"] = "matched",
                        ["propertyName"] = propertyName,
                        ["value"] = text
                    });
            }

            await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return DiagnosticResponse.Fail(
            request.Id,
            DiagnosticErrorCode.UiThreadTimeout,
            $"Property '{propertyName}' did not reach expected value within {timeoutMs} ms.");
    }

    private ControlLookup ResolveRequiredControl(DiagnosticRequest request) =>
        InspectionRequestHelpers.ResolveControl(request, _getRoots(), _controlResolver);

    private ControlLookup ResolveOptionalControl(DiagnosticRequest request)
    {
        if (!string.IsNullOrWhiteSpace(InspectionRequestHelpers.GetString(request.Parameters, "controlId")))
            return ResolveRequiredControl(request);

        var roots = _getRoots();
        return roots.Count == 0
            ? ControlLookup.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.TargetNotFound,
                    "No root controls are available."))
            : ControlLookup.Found(roots[0], rootIndex: 0, controlId: "");
    }

    private static AvaloniaProperty? FindAvaloniaProperty(
        Control control,
        string propertyName) =>
        AvaloniaPropertyRegistry.Instance
            .GetRegistered(control)
            .FirstOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal));

    private static object? ReadPropertyValue(
        Control control,
        string propertyName)
    {
        var avaloniaProperty = FindAvaloniaProperty(control, propertyName);
        if (avaloniaProperty is not null)
            return control.GetValue(avaloniaProperty);

        var clrProperty = control.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (clrProperty is { } && clrProperty.GetIndexParameters().Length == 0)
            return clrProperty.GetValue(control);

        var dataContext = control.DataContext;
        var dataContextProperty = dataContext?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return dataContextProperty?.GetIndexParameters().Length == 0
            ? dataContextProperty.GetValue(dataContext)
            : null;
    }

    private static DiagnosticResponse? TryExecuteCommand(
        DiagnosticRequest request,
        Control control)
    {
        var command = control.GetType()
            .GetProperty("Command", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(control) as ICommand;
        if (command is null)
        {
            return null;
        }

        var parameter = control.GetType()
            .GetProperty("CommandParameter", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(control);
        if (!command.CanExecute(parameter))
        {
            return DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.UnsupportedOperation,
                "Control command cannot execute.");
        }

        command.Execute(parameter);
        return Ok(request, "commandExecuted");
    }

    private static bool TryToggleExpandableControl(
        Control control,
        out bool expanded)
    {
        expanded = false;

        var isExpandedProperty = control.GetType()
            .GetProperty("IsExpanded", BindingFlags.Instance | BindingFlags.Public);
        if (isExpandedProperty is not { PropertyType: { } propertyType } || propertyType != typeof(bool) || !isExpandedProperty.CanWrite)
        {
            return false;
        }

        var hasNavigationChildren = control.GetType()
            .GetMethod("HasNavigationChildren", BindingFlags.Instance | BindingFlags.Public)
            ?.Invoke(control, null) as bool?;
        if (hasNavigationChildren != true)
        {
            return false;
        }

        expanded = !((bool?)isExpandedProperty.GetValue(control) ?? false);
        isExpandedProperty.SetValue(control, expanded);
        return true;
    }

    private static bool TryRaiseStaticRoutedEvent(
        Control control,
        string fieldName)
    {
        var routedEvent = control.GetType()
            .GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            ?.GetValue(null) as RoutedEvent;
        if (routedEvent is null)
        {
            return false;
        }

        control.RaiseEvent(new RoutedEventArgs(routedEvent, control));
        return true;
    }

    private static bool IsEditableTextBox(TextBox textBox) =>
        textBox.IsVisible && textBox.IsEnabled && !textBox.IsReadOnly;

    private static void RaiseReturnKey(TextBox textBox)
    {
        textBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = textBox,
            Key = Key.Return,
            KeyModifiers = KeyModifiers.None
        });
        textBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Source = textBox,
            Key = Key.Return,
            KeyModifiers = KeyModifiers.None
        });
    }

    private static DiagnosticResponse Ok(
        DiagnosticRequest request,
        string status) =>
        DiagnosticResponse.Ok(
            request.Id,
            new JsonObject { ["status"] = status });

    private enum InteractionKind
    {
        Click,
        SetProperty,
        InputText,
        InvokeCommand,
        WaitForProperty
    }
}
