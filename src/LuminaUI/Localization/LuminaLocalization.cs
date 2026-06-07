using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace LuminaUI.Localization;

public static class LuminaLocalization
{
    private sealed record ResourceSource(ResourceManager ResourceManager, int Priority, int Order);

    private readonly record struct ResourceCacheKey(string Key, string CultureName);

    private readonly record struct ObservableCacheKey(string Key, string? Fallback);

    private const string CoreResourceBaseName = "LuminaUI.Localization.Resources.LuminaUIStrings";

    private static readonly CultureInfo DefaultFallbackCulture = CultureInfo.GetCultureInfo("en-US");

    private static readonly List<CultureInfo> RegisteredCultures;

    private static readonly List<ResourceSource> ResourceSources;

    private static readonly Dictionary<ResourceCacheKey, string> ResourceCache = new();

    private static readonly Dictionary<ObservableCacheKey, LocalizedStringObservable> Observables = new();

    private static readonly object SyncRoot = new();

    private static int _nextResourceOrder;

    public static CultureInfo FallbackCulture { get; private set; }

    public static CultureInfo CurrentCulture { get; private set; }

    public static IReadOnlyList<CultureInfo> SupportedCultures => RegisteredCultures;

    public static event EventHandler? LanguageChanged;

    public static CultureInfo DetectSystemCulture()
    {
        return FindSupportedCulture(CultureInfo.CurrentUICulture) ?? FallbackCulture;
    }

    public static void UseSystemCulture()
    {
        SetCulture(DetectSystemCulture());
    }

    public static void SetCulture(string cultureName)
    {
        SetCulture(CultureInfo.GetCultureInfo(cultureName));
    }

    public static void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (CurrentCulture.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
        {
            ApplyThreadCulture(culture);
            return;
        }

        CurrentCulture = culture;
        ApplyThreadCulture(culture);
        NotifyLanguageChanged();
    }

    public static void SetFallbackCulture(string cultureName)
    {
        SetFallbackCulture(CultureInfo.GetCultureInfo(cultureName));
    }

    public static void SetFallbackCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        FallbackCulture = culture;
        RegisterSupportedCulture(culture);
        ClearResourceCache();
        NotifyLanguageChanged();
    }

    public static void RegisterSupportedCulture(string cultureName)
    {
        RegisterSupportedCulture(CultureInfo.GetCultureInfo(cultureName));
    }

    public static void RegisterSupportedCulture(CultureInfo culture)
    {
        if (!RegisteredCultures.Any(item => item.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
        {
            RegisteredCultures.Add(culture);
        }
    }

    public static void RegisterResourceManager(ResourceManager resourceManager, int priority = 100)
    {
        ArgumentNullException.ThrowIfNull(resourceManager, "resourceManager");
        ResourceSources.Add(new ResourceSource(resourceManager, priority, _nextResourceOrder++));
        ResourceSources.Sort((ResourceSource left, ResourceSource right) =>
        {
            int num = right.Priority.CompareTo(left.Priority);
            return (num != 0) ? num : right.Order.CompareTo(left.Order);
        });
        ClearResourceCache();
        NotifyLanguageChanged();
    }

    public static string Get(string key)
    {
        string value;
        return TryGet(key, out value) ? value : string.Empty;
    }

    public static string Get(string key, CultureInfo culture)
    {
        string value;
        return TryGet(key, culture, out value) ? value : string.Empty;
    }

    public static bool TryGet(string key, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }
        if (TryGet(key, CurrentCulture, out value))
        {
            return true;
        }
        return TryGet(key, FallbackCulture, out value);
    }

    public static bool TryGet(string key, CultureInfo culture, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }
        value = GetResourceString(key, culture) ?? string.Empty;
        return value.Length > 0;
    }

    public static string Format(string key, params object?[] args)
    {
        return string.Format(CurrentCulture, Get(key), args);
    }

    public static IObservable<string> Observe(string key, string? fallback = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var cacheKey = new ObservableCacheKey(key, fallback);
        lock (SyncRoot)
        {
            if (!Observables.TryGetValue(cacheKey, out var observable))
            {
                observable = new LocalizedStringObservable(key, fallback);
                Observables.Add(cacheKey, observable);
            }

            return observable;
        }
    }

    private static CultureInfo? FindSupportedCulture(CultureInfo culture)
    {
        CultureInfo current = culture;
        while (!string.IsNullOrEmpty(current.Name))
        {
            CultureInfo? exact = RegisteredCultures.FirstOrDefault(item => item.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }
            current = current.Parent;
        }
        return RegisteredCultures.FirstOrDefault(item => item.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetResourceString(string key, CultureInfo culture)
    {
        var cacheKey = new ResourceCacheKey(key, culture.Name);
        lock (SyncRoot)
        {
            if (ResourceCache.TryGetValue(cacheKey, out var cachedValue))
            {
                return cachedValue.Length == 0 ? null : cachedValue;
            }
        }

        string value = GetResourceStringCore(key, culture) ?? string.Empty;
        lock (SyncRoot)
        {
            ResourceCache[cacheKey] = value;
        }

        return value.Length == 0 ? null : value;
    }

    private static string? GetResourceStringCore(string key, CultureInfo culture)
    {
        foreach (ResourceSource source in ResourceSources)
        {
            try
            {
                string? value = source.ResourceManager.GetString(key, culture);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch (MissingManifestResourceException)
            {
            }
        }
        return null;
    }

    private static void ApplyThreadCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static void ClearResourceCache()
    {
        lock (SyncRoot)
        {
            ResourceCache.Clear();
        }
    }

    private static void NotifyLanguageChanged()
    {
        LocalizedStringObservable[] observables;
        lock (SyncRoot)
        {
            observables = Observables.Values.ToArray();
        }

        foreach (var observable in observables)
        {
            observable.Notify();
        }

        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    private sealed class LocalizedStringObservable : IObservable<string>
    {
        private readonly List<IObserver<string>> _observers = [];
        private readonly string _key;
        private readonly string? _fallback;

        public LocalizedStringObservable(string key, string? fallback)
        {
            _key = key;
            _fallback = fallback;
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            lock (_observers)
            {
                _observers.Add(observer);
            }

            observer.OnNext(GetValue());
            return new Subscription(this, observer);
        }

        public void Notify()
        {
            IObserver<string>[] observers;
            lock (_observers)
            {
                if (_observers.Count == 0)
                {
                    return;
                }

                observers = _observers.ToArray();
            }

            var value = GetValue();
            foreach (var observer in observers)
            {
                observer.OnNext(value);
            }
        }

        private string GetValue()
        {
            return TryGet(_key, out var value) ? value : _fallback ?? _key;
        }

        private void Unsubscribe(IObserver<string> observer)
        {
            lock (_observers)
            {
                _observers.Remove(observer);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private LocalizedStringObservable? _owner;
            private IObserver<string>? _observer;

            public Subscription(LocalizedStringObservable owner, IObserver<string> observer)
            {
                _owner = owner;
                _observer = observer;
            }

            public void Dispose()
            {
                var owner = _owner;
                var observer = _observer;
                if (owner != null && observer != null)
                {
                    owner.Unsubscribe(observer);
                    _owner = null;
                    _observer = null;
                }
            }
        }
    }

    static LuminaLocalization()
    {
        RegisteredCultures = [DefaultFallbackCulture, CultureInfo.GetCultureInfo("zh-CN")];
        ResourceSources =
        [
            new ResourceSource(new ResourceManager(CoreResourceBaseName, typeof(LuminaLocalization).Assembly), 0, 0)
        ];
        _nextResourceOrder = 1;
        FallbackCulture = DefaultFallbackCulture;
        CurrentCulture = DetectSystemCulture();
    }
}
