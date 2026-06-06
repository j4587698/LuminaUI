using System.Text.Json.Nodes;
using LuminaUI.Diagnostics.Dispatch;
using LuminaUI.Diagnostics.Inspection;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Binding;

public sealed class BindingErrorStore
{
    private readonly object _gate = new();
    private readonly Queue<BindingErrorEntry> _entries = new();
    private readonly int _capacity;

    public BindingErrorStore(int capacity = 200)
    {
        _capacity = capacity > 0
            ? capacity
            : throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
    }

    public void Add(BindingErrorEntry entry)
    {
        lock (_gate)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _capacity)
                _entries.Dequeue();
        }
    }

    public IReadOnlyList<BindingErrorEntry> Snapshot(int maxItems = 100)
    {
        lock (_gate)
        {
            return _entries
                .Reverse()
                .Take(Math.Clamp(maxItems, 1, _capacity))
                .ToArray();
        }
    }

    public int Count
    {
        get
        {
            lock (_gate)
                return _entries.Count;
        }
    }
}

public sealed class BindingErrorInspectionHandler : IDiagnosticToolHandler
{
    private readonly BindingErrorStore _store;

    public BindingErrorInspectionHandler(BindingErrorStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string Method => LuminaDiagnosticsToolNames.GetBindingErrors;

    public Task<DiagnosticResponse> HandleAsync(
        DiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        var maxItems = InspectionRequestHelpers.GetInt(request.Parameters, "maxItems", defaultValue: 100);
        var entries = new JsonArray();

        foreach (var entry in _store.Snapshot(maxItems))
        {
            entries.Add(new JsonObject
            {
                ["timestamp"] = entry.Timestamp.ToString("O"),
                ["level"] = entry.Level,
                ["area"] = entry.Area,
                ["sourceType"] = entry.SourceType,
                ["message"] = entry.Message
            });
        }

        return Task.FromResult(
            DiagnosticResponse.Ok(
                request.Id,
                new JsonObject
                {
                    ["count"] = entries.Count,
                    ["entries"] = entries
                }));
    }
}
