using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

/// <summary>
/// Compatibility wrapper for the old top-level overlay container. Prefer <see cref="LuminaOverlayHost"/> or <see cref="LuminaShell"/>.
/// </summary>
public class LuminaTopView : LuminaOverlayHost
{
    private static readonly object TopViewRegistryLock = new object();

    private static readonly List<WeakReference<LuminaTopView>> AttachedTopViews = new List<WeakReference<LuminaTopView>>();

    private static readonly Dictionary<string, WeakReference<LuminaTopView>> TopViewRegistry = new Dictionary<string, WeakReference<LuminaTopView>>(StringComparer.Ordinal);

    public static readonly StyledProperty<string?> TopViewKeyProperty = AvaloniaProperty.Register<LuminaTopView, string?>(nameof(TopViewKey));

    public static new LuminaTopView? Current { get; private set; }

    public string? TopViewKey
    {
        get => GetValue(TopViewKeyProperty);
        set => SetValue(TopViewKeyProperty, value);
    }

    public static new LuminaTopView? FindFor(Control? owner)
    {
        if (owner == null)
        {
            return Current;
        }

        List<LuminaTopView> candidates = GetOwnerChain(owner);
        if (candidates.Count > 0)
        {
            return candidates.OrderByDescending(GetVisualDepth).First();
        }

        return TopLevel.GetTopLevel(owner)?.GetVisualDescendants().OfType<LuminaTopView>().OrderByDescending(GetVisualDepth)
            .FirstOrDefault() ?? Current;
    }

    public static new LuminaTopView? FindOuterFor(Control? owner)
    {
        if (owner == null)
        {
            return Current;
        }

        List<LuminaTopView> candidates = GetOwnerChain(owner);
        if (candidates.Count > 0)
        {
            return candidates.OrderBy(GetVisualDepth).First();
        }

        return TopLevel.GetTopLevel(owner)?.GetVisualDescendants().OfType<LuminaTopView>().OrderBy(GetVisualDepth)
            .FirstOrDefault() ?? Current;
    }

    public static LuminaTopView? GetTopView(string topViewKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topViewKey, nameof(topViewKey));
        lock (TopViewRegistryLock)
        {
            CleanupTopViewRegistry();
            return TopViewRegistry.TryGetValue(topViewKey, out WeakReference<LuminaTopView>? reference) && reference.TryGetTarget(out LuminaTopView? topView)
                ? topView
                : null;
        }
    }

    public static IReadOnlyList<LuminaTopView> GetOpenTopViews()
    {
        lock (TopViewRegistryLock)
        {
            CleanupTopViewRegistry();
            return AttachedTopViews.Select(reference => reference.TryGetTarget(out LuminaTopView? target) ? target : null).OfType<LuminaTopView>().ToArray();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RegisterAttachedTopView();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnregisterAttachedTopView();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopViewKeyProperty)
        {
            UpdateTopViewKey(change.GetOldValue<string>(), change.GetNewValue<string>());
        }
    }

    private static List<LuminaTopView> GetOwnerChain(Control owner)
    {
        List<LuminaTopView> candidates = new List<LuminaTopView>();
        if (owner is LuminaTopView topView)
        {
            candidates.Add(topView);
        }

        candidates.AddRange(owner.GetVisualAncestors().OfType<LuminaTopView>());
        return candidates;
    }

    private static int GetVisualDepth(Control control)
    {
        return control.GetVisualAncestors().Count();
    }

    private void RegisterAttachedTopView()
    {
        lock (TopViewRegistryLock)
        {
            CleanupTopViewRegistry();
            AttachedTopViews.Add(new WeakReference<LuminaTopView>(this));
            RegisterTopViewKey(TopViewKey, this);
            Current = this;
        }
    }

    private void UnregisterAttachedTopView()
    {
        lock (TopViewRegistryLock)
        {
            AttachedTopViews.RemoveAll(IsThisTopViewReference);
            UnregisterTopViewKey(TopViewKey, this);
            if (Current == this)
            {
                Current = AttachedTopViews.Select(reference => reference.TryGetTarget(out LuminaTopView? target) ? target : null).OfType<LuminaTopView>().LastOrDefault();
            }
        }
    }

    private void UpdateTopViewKey(string? oldKey, string? newKey)
    {
        lock (TopViewRegistryLock)
        {
            UnregisterTopViewKey(oldKey, this);
            RegisterTopViewKey(newKey, this);
        }
    }

    private static void RegisterTopViewKey(string? topViewKey, LuminaTopView topView)
    {
        if (!string.IsNullOrWhiteSpace(topViewKey))
        {
            TopViewRegistry[topViewKey] = new WeakReference<LuminaTopView>(topView);
        }
    }

    private static void UnregisterTopViewKey(string? topViewKey, LuminaTopView topView)
    {
        if (!string.IsNullOrWhiteSpace(topViewKey) &&
            TopViewRegistry.TryGetValue(topViewKey, out WeakReference<LuminaTopView>? reference) &&
            reference.TryGetTarget(out LuminaTopView? registeredTopView) &&
            registeredTopView == topView)
        {
            TopViewRegistry.Remove(topViewKey);
        }
    }

    private static void CleanupTopViewRegistry()
    {
        AttachedTopViews.RemoveAll(reference => !reference.TryGetTarget(out LuminaTopView? _));
        foreach (string key in TopViewRegistry.Keys.ToArray())
        {
            if (!TopViewRegistry[key].TryGetTarget(out LuminaTopView? _))
            {
                TopViewRegistry.Remove(key);
            }
        }
    }

    private bool IsThisTopViewReference(WeakReference<LuminaTopView> reference)
    {
        return reference.TryGetTarget(out LuminaTopView? topView) && topView == this;
    }
}
