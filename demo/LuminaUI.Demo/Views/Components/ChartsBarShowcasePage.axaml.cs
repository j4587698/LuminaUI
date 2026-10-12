using System.Collections.Generic;
using LuminaUI.Charts;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;
using LuminaUI.Localization;

namespace LuminaUI.Demo.Views;

public partial class ChartsBarShowcasePage : LuminaPage
{
    public ChartsBarShowcasePage()
    {
        DataContext = new ChartsBarShowcaseViewModel();
        InitializeComponent();
        Chart.XAxis.Kind = LuminaChartAxisKind.Category;
        Chart.XAxis.Categories = BuildMonthLabels();
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
        Unloaded += OnUnloaded;
    }

    private void OnLanguageChanged(object? sender, System.EventArgs e)
    {
        Chart.XAxis.Categories = BuildMonthLabels();
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        LuminaLocalization.LanguageChanged -= OnLanguageChanged;
        Unloaded -= OnUnloaded;
    }

    private static IReadOnlyList<string> BuildMonthLabels()
    {
        var months = new string[10];
        for (int i = 0; i < months.Length; i++)
        {
            months[i] = LuminaLocalization.Get(string.Format(System.Globalization.CultureInfo.InvariantCulture, SandboxLocalization.ChartsMonthFormat, i + 1));
        }

        return months;
    }
}