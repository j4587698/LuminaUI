using System;
using System.Collections.Generic;
using LuminaUI.Charts;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

/// <summary>
/// Localizes the sample data shown by the chart showcase pages (series titles, slice labels,
/// category names) and re-applies the texts when the language changes. Keys are
/// <c>Sandbox.Charts.Data.&lt;name&gt;</c> in <c>SandboxTexts.resx</c>.
/// </summary>
internal sealed class ChartDemoTexts
{
    private readonly List<Action> _appliers = [];

    public ChartDemoTexts()
    {
        // The showcase view models live as long as their cached pages, i.e. for the app's
        // lifetime, like the other demo view models that listen to LanguageChanged.
        LuminaLocalization.LanguageChanged += (_, _) =>
        {
            foreach (Action apply in _appliers)
            {
                apply();
            }
        };
    }

    public static string Get(string name) => LuminaLocalization.Get("Sandbox.Charts.Data." + name);

    /// <summary>Titles <paramref name="series"/> in order with the given resource names.</summary>
    public void Titles(IList<LuminaChartSeries> series, params string[] names)
    {
        for (int index = 0; index < names.Length; index++)
        {
            LuminaChartSeries item = series[index];
            string name = names[index];
            Track(() => item.Title = Get(name));
        }
    }

    public void Titles(IList<LuminaRadarSeries> series, params string[] names)
    {
        for (int index = 0; index < names.Length; index++)
        {
            LuminaRadarSeries item = series[index];
            string name = names[index];
            Track(() => item.Title = Get(name));
        }
    }

    public void Labels(IList<LuminaPieSlice> slices, params string[] names)
    {
        for (int index = 0; index < names.Length; index++)
        {
            LuminaPieSlice item = slices[index];
            string name = names[index];
            Track(() => item.Label = Get(name));
        }
    }

    /// <summary>Resolves <paramref name="names"/> into a fresh list (for category axes).</summary>
    public static string[] GetAll(params string[] names) => Array.ConvertAll(names, Get);

    /// <summary>Runs <paramref name="apply"/> now and after every language change.</summary>
    public void Track(Action apply)
    {
        apply();
        _appliers.Add(apply);
    }
}
