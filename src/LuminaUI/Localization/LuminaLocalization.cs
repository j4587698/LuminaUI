using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace LuminaUI.Localization;

public static class LuminaLocalization
{
	private sealed record ResourceSource(ResourceManager ResourceManager, int Priority, int Order);

	private const string CoreResourceBaseName = "LuminaUI.Localization.Resources.LuminaUIStrings";

	private static readonly CultureInfo DefaultFallbackCulture = CultureInfo.GetCultureInfo("en-US");

	private static readonly List<CultureInfo> RegisteredCultures;

	private static readonly List<ResourceSource> ResourceSources;

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
		CurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		ApplyToApplicationResources();
		LuminaLocalization.LanguageChanged?.Invoke(null, EventArgs.Empty);
	}

	public static void SetFallbackCulture(string cultureName)
	{
		SetFallbackCulture(CultureInfo.GetCultureInfo(cultureName));
	}

	public static void SetFallbackCulture(CultureInfo culture)
	{
		FallbackCulture = culture;
		RegisterSupportedCulture(culture);
		ApplyToApplicationResources();
		LuminaLocalization.LanguageChanged?.Invoke(null, EventArgs.Empty);
	}

	public static void RegisterSupportedCulture(string cultureName)
	{
		RegisterSupportedCulture(CultureInfo.GetCultureInfo(cultureName));
	}

	public static void RegisterSupportedCulture(CultureInfo culture)
	{
		if (!RegisteredCultures.Any((CultureInfo item) => item.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
		{
			RegisteredCultures.Add(culture);
		}
	}

	public static void RegisterResourceManager(ResourceManager resourceManager, int priority = 100)
	{
		ArgumentNullException.ThrowIfNull(resourceManager, "resourceManager");
		ResourceSources.Add(new ResourceSource(resourceManager, priority, _nextResourceOrder++));
		ResourceSources.Sort(delegate(ResourceSource left, ResourceSource right)
		{
			int num = right.Priority.CompareTo(left.Priority);
			return (num != 0) ? num : right.Order.CompareTo(left.Order);
		});
		ApplyToApplicationResources();
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

	public static void ApplyToApplicationResources()
	{
		IResourceDictionary? resources = Application.Current?.Resources;
		if (resources == null)
		{
			return;
		}
		foreach (string key in EnumerateResourceKeys())
		{
			resources[key] = Get(key);
		}
	}

	private static CultureInfo? FindSupportedCulture(CultureInfo culture)
	{
		CultureInfo current = culture;
		while (!string.IsNullOrEmpty(current.Name))
		{
			CultureInfo? exact = RegisteredCultures.FirstOrDefault((CultureInfo item) => item.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase));
			if (exact != null)
			{
				return exact;
			}
			current = current.Parent;
		}
		return RegisteredCultures.FirstOrDefault((CultureInfo item) => item.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
	}

	private static string? GetResourceString(string key, CultureInfo culture)
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

	private static IEnumerable<string> EnumerateResourceKeys()
	{
		HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
		foreach (ResourceSource source in ResourceSources)
		{
			foreach (CultureInfo culture in EnumerateResourceCultures())
			{
				ResourceSet? resourceSet;
				try
				{
					resourceSet = source.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: true);
				}
				catch (MissingManifestResourceException)
				{
					continue;
				}
				if (resourceSet == null)
				{
					continue;
				}
				foreach (DictionaryEntry item in resourceSet)
				{
					if (item.Key is string key)
					{
						keys.Add(key);
					}
				}
			}
		}
		return keys;
	}

	private static IEnumerable<CultureInfo> EnumerateResourceCultures()
	{
		yield return CurrentCulture;
		yield return FallbackCulture;
		yield return CultureInfo.InvariantCulture;
	}

	static LuminaLocalization()
	{
		int num = 2;
		List<CultureInfo> list = new List<CultureInfo>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<CultureInfo> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = DefaultFallbackCulture;
		num2++;
		span[num2] = CultureInfo.GetCultureInfo("zh-CN");
		RegisteredCultures = list;
		num2 = 1;
		List<ResourceSource> list2 = new List<ResourceSource>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		Span<ResourceSource> span2 = CollectionsMarshal.AsSpan(list2);
		num = 0;
		span2[num] = new ResourceSource(new ResourceManager("LuminaUI.Localization.Resources.LuminaUIStrings", typeof(LuminaLocalization).Assembly), 0, 0);
		ResourceSources = list2;
		_nextResourceOrder = 1;
		FallbackCulture = DefaultFallbackCulture;
		CurrentCulture = DetectSystemCulture();
	}
}
