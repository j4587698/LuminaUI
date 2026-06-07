using LuminaUI.Diagnostics.Binding;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Data;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Inspection;
using LuminaUI.Diagnostics.Interaction;
using LuminaUI.Diagnostics.Scroll;
using LuminaUI.Diagnostics.Serialization;
using LuminaUI.Diagnostics.Threading;
using LuminaUI.Diagnostics.Visual;

namespace LuminaUI.Diagnostics;

public static class DiagnosticsServiceFactory
{
    public static DiagnosticsServices CreateDefault()
    {
        var invoker = new AvaloniaUiThreadInvoker();
        var resolver = new AvaloniaControlResolver();
        var formatter = new ValueFormatter();
        var treeSerializer = new VisualNodeSerializer(formatter);
        var propertyValueConverter = new PropertyValueConverter();
        var scrollStateSerializer = new ScrollStateSerializer();
        var bindingErrorStore = new BindingErrorStore();
        var bindingErrorSink = BindingErrorLogSink.Install(bindingErrorStore);

        var registry = new DiagnosticHandlerRegistry()
            .Add(new DiagnosticsPingHandler())
            .Add(new WindowInspectionHandler(invoker))
            .Add(TreeInspectionHandler.VisualTree(invoker, resolver, treeSerializer))
            .Add(TreeInspectionHandler.LogicalTree(invoker, resolver, treeSerializer))
            .Add(new ControlSearchHandler(invoker))
            .Add(new FocusedElementInspectionHandler(invoker))
            .Add(new PropertyInspectionHandler(invoker, resolver, formatter))
            .Add(new DataContextInspectionHandler(invoker, resolver, formatter))
            .Add(new BindingErrorInspectionHandler(bindingErrorStore))
            .Add(new StyleInspectionHandler(invoker, resolver))
            .Add(new ResourceInspectionHandler(invoker, resolver, formatter))
            .Add(InteractionHandler.ClickControl(invoker, resolver))
            .Add(InteractionHandler.SetProperty(invoker, resolver, propertyValueConverter))
            .Add(InteractionHandler.InputText(invoker, resolver))
            .Add(InteractionHandler.InvokeCommand(invoker, resolver))
            .Add(InteractionHandler.WaitForProperty(invoker, resolver))
            .Add(new ScreenshotHandler(invoker, resolver))
            .Add(ScrollDiagnosticsHandler.Scroll(invoker, resolver, scrollStateSerializer))
            .Add(ScrollDiagnosticsHandler.ScrollableItems(invoker, resolver, scrollStateSerializer));

        return new DiagnosticsServices(
            registry.Handlers,
            registry.CreateDispatcher(),
            [bindingErrorSink]);
    }
}

public sealed class DiagnosticsServices : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _ownedDisposables;
    private bool _disposed;

    public DiagnosticsServices(
        IReadOnlyList<IDiagnosticToolHandler> handlers,
        DiagnosticDispatcher dispatcher,
        IReadOnlyList<IDisposable>? ownedDisposables = null)
    {
        Handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _ownedDisposables = ownedDisposables ?? [];
    }

    public IReadOnlyList<IDiagnosticToolHandler> Handlers { get; }

    public DiagnosticDispatcher Dispatcher { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        for (var index = _ownedDisposables.Count - 1; index >= 0; index--)
            _ownedDisposables[index].Dispose();
    }
}
