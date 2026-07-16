using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

/// <summary>
/// Dispatches system back requests for a single top level.
/// </summary>
public sealed class LuminaBackDispatcher
{
    internal const int ShellNavigationPriority = 0;

    internal const int ContentPriority = 100;

    internal const int ModalPriority = 200;

    private static readonly ConditionalWeakTable<TopLevel, LuminaBackDispatcher> Dispatchers = new ConditionalWeakTable<TopLevel, LuminaBackDispatcher>();

    private readonly TopLevel _topLevel;

    private readonly List<Registration> _registrations = new List<Registration>();

    private long _nextSequence;

    private LuminaBackDispatcher(TopLevel topLevel)
    {
        _topLevel = topLevel;
        _topLevel.BackRequested += OnBackRequested;
    }

    /// <summary>
    /// Raised when no registered handler consumes a system back request.
    /// Leave <see cref="LuminaBackRequestedEventArgs.Handled"/> false to pass the request to the platform.
    /// </summary>
    public event EventHandler<LuminaBackRequestedEventArgs>? UnhandledBackRequested;

    public static LuminaBackDispatcher GetFor(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        return Dispatchers.GetValue(topLevel, static value => new LuminaBackDispatcher(value));
    }

    public static LuminaBackDispatcher? FindFor(Control owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        TopLevel? topLevel = TopLevel.GetTopLevel(owner);
        return topLevel == null ? null : GetFor(topLevel);
    }

    /// <summary>
    /// Registers a back handler. Handlers at the same priority run from the deepest visual owner outward,
    /// then in last-in-first-out order at the same visual depth.
    /// Dispose the returned registration when the handler is no longer active.
    /// </summary>
    public IDisposable Register(Control owner, Func<bool> handler)
    {
        return Register(owner, handler, ContentPriority);
    }

    internal IDisposable Register(Control owner, Func<bool> handler, int priority)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(handler);

        var registration = new Registration(this, owner, handler, priority, ++_nextSequence);
        _registrations.Add(registration);
        return registration;
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        Registration[] registrations = _registrations
            .Where(static registration => !registration.IsDisposed)
            .ToArray();
        var candidates = new List<(Registration Registration, Control Owner, int VisualDepth)>(registrations.Length);

        foreach (Registration registration in registrations)
        {
            if (!registration.TryGetOwner(out Control? owner) || owner == null || !ReferenceEquals(TopLevel.GetTopLevel(owner), _topLevel))
            {
                registration.Dispose();
                continue;
            }

            candidates.Add((registration, owner, owner.GetVisualAncestors().Count()));
        }

        foreach (var candidate in candidates
            .OrderByDescending(static candidate => candidate.Registration.Priority)
            .ThenByDescending(static candidate => candidate.VisualDepth)
            .ThenByDescending(static candidate => candidate.Registration.Sequence))
        {
            if (!candidate.Owner.IsEffectivelyVisible || !candidate.Registration.Handler())
            {
                continue;
            }

            e.Handled = true;
            return;
        }

        var args = new LuminaBackRequestedEventArgs();
        UnhandledBackRequested?.Invoke(this, args);
        e.Handled = args.Handled;
    }

    private void Remove(Registration registration)
    {
        _registrations.Remove(registration);
    }

    private sealed class Registration : IDisposable
    {
        private readonly LuminaBackDispatcher _dispatcher;

        private readonly WeakReference<Control> _owner;

        public Registration(LuminaBackDispatcher dispatcher, Control owner, Func<bool> handler, int priority, long sequence)
        {
            _dispatcher = dispatcher;
            _owner = new WeakReference<Control>(owner);
            Handler = handler;
            Priority = priority;
            Sequence = sequence;
        }

        public Func<bool> Handler { get; }

        public int Priority { get; }

        public long Sequence { get; }

        public bool IsDisposed { get; private set; }

        public bool TryGetOwner(out Control? owner)
        {
            return _owner.TryGetTarget(out owner);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            _dispatcher.Remove(this);
        }
    }
}
