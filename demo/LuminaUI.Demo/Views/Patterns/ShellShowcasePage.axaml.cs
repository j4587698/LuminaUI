using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using LuminaUI.Controls;
using LuminaUI.Localization;

namespace LuminaUI.Demo.Views;

public partial class ShellShowcasePage : LuminaPage
{
    private const double StackedLayoutBreakpoint = 760.0;

    private const string RouteHomeTitleKey = "Sandbox.ShellShowcase.NavHome";
    private const string RouteHomeSubtitleKey = "Sandbox.ShellShowcase.RouteHomeSubtitle";
    private const string RouteReportsTitleKey = "Sandbox.ShellShowcase.NavReports";
    private const string RouteReportsSubtitleKey = "Sandbox.ShellShowcase.RouteReportsSubtitle";
    private const string RouteSettingsTitleKey = "Sandbox.ShellShowcase.NavSettings";
    private const string RouteSettingsSubtitleKey = "Sandbox.ShellShowcase.RouteSettingsSubtitle";
    private const string RouteAuditTitleKey = "Sandbox.ShellShowcase.NavAudit";
    private const string RouteAuditSubtitleKey = "Sandbox.ShellShowcase.RouteAuditSubtitle";
    private const string RouteHintKey = "Sandbox.ShellShowcase.RouteHint";

    private int _nextStackPageNumber = 2;

    public ShellShowcasePage()
    {
        InitializeComponent();
        RegisterPreviewRoutes();
        PreviewShell.NavigateTo("PreviewHome", closeMenuOnNavigate: false);
        ShellShowcaseLayout.SizeChanged += OnShellShowcaseLayoutSizeChanged;
    }

    private void OnShellShowcaseLayoutSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateShellShowcaseLayout(e.NewSize.Width);
    }

    private void UpdateShellShowcaseLayout(double width)
    {
        bool isStacked = width < StackedLayoutBreakpoint;
        ShellShowcaseLayout.ColumnDefinitions = isStacked ? new ColumnDefinitions("*") : new ColumnDefinitions("320,*");
        ShellShowcaseLayout.RowDefinitions = isStacked ? new RowDefinitions("Auto,Auto") : new RowDefinitions("Auto");
        ShellShowcaseLayout.ColumnSpacing = isStacked ? 0 : 16;
        ShellShowcaseLayout.RowSpacing = isStacked ? 16 : 0;

        Grid.SetColumn(ShellOptionsCard, 0);
        Grid.SetRow(ShellOptionsCard, 0);
        Grid.SetColumn(ShellPreviewCard, isStacked ? 0 : 1);
        Grid.SetRow(ShellPreviewCard, isStacked ? 1 : 0);
    }

    private void RegisterPreviewRoutes()
    {
        PreviewShell.RegisterRoute("PreviewHome", () => CreatePreviewPage("PreviewHome", RouteHomeTitleKey, RouteHomeSubtitleKey));
        PreviewShell.RegisterRoute("PreviewReports", () => CreatePreviewPage("PreviewReports", RouteReportsTitleKey, RouteReportsSubtitleKey));
        PreviewShell.RegisterRoute("PreviewSettings", () => CreatePreviewPage("PreviewSettings", RouteSettingsTitleKey, RouteSettingsSubtitleKey));
        PreviewShell.RegisterRoute("PreviewAudit", () => CreatePreviewPage("PreviewAudit", RouteAuditTitleKey, RouteAuditSubtitleKey));
    }

    private async void OnPushPreviewShellPageClicked(object? sender, RoutedEventArgs e)
    {
        await PushPreviewShellPageAsync();
    }

    private async void OnPopPreviewShellPageClicked(object? sender, RoutedEventArgs e)
    {
        await PreviewShell.PopAsync();
    }

    private async void OnPopPreviewShellRootClicked(object? sender, RoutedEventArgs e)
    {
        await PreviewShell.PopToRootAsync();
    }

    private void OnHeaderButtonsStandardClicked(object? sender, RoutedEventArgs e)
    {
        ApplyHeaderButtonPreset(LuminaShellHeaderButtonVisibility.Auto, LuminaShellHeaderButtonVisibility.Auto, collapseMenuWhenBack: false);
    }

    private void OnHeaderButtonsBackMenuClicked(object? sender, RoutedEventArgs e)
    {
        ApplyHeaderButtonPreset(LuminaShellHeaderButtonVisibility.Visible, LuminaShellHeaderButtonVisibility.Visible, collapseMenuWhenBack: false);
    }

    private void OnHeaderButtonsBackOnlyClicked(object? sender, RoutedEventArgs e)
    {
        ApplyHeaderButtonPreset(LuminaShellHeaderButtonVisibility.Visible, LuminaShellHeaderButtonVisibility.Collapsed, collapseMenuWhenBack: false);
    }

    private void OnHeaderButtonsMenuOnlyClicked(object? sender, RoutedEventArgs e)
    {
        ApplyHeaderButtonPreset(LuminaShellHeaderButtonVisibility.Collapsed, LuminaShellHeaderButtonVisibility.Visible, collapseMenuWhenBack: false);
    }

    private void OnHeaderButtonsHideOnBackClicked(object? sender, RoutedEventArgs e)
    {
        ApplyHeaderButtonPreset(LuminaShellHeaderButtonVisibility.Auto, LuminaShellHeaderButtonVisibility.Auto, collapseMenuWhenBack: true);
    }

    private void OnHeaderButtonsNoneClicked(object? sender, RoutedEventArgs e)
    {
        ApplyHeaderButtonPreset(LuminaShellHeaderButtonVisibility.Collapsed, LuminaShellHeaderButtonVisibility.Collapsed, collapseMenuWhenBack: false);
    }

    private void ApplyHeaderButtonPreset(LuminaShellHeaderButtonVisibility backVisibility, LuminaShellHeaderButtonVisibility menuVisibility, bool collapseMenuWhenBack)
    {
        PreviewShell.HeaderBackButtonVisibility = backVisibility;
        PreviewShell.HeaderPaneToggleButtonVisibility = menuVisibility;
        PreviewShell.CollapseHeaderPaneToggleWhenCanGoBack = collapseMenuWhenBack;
    }

    private async Task PushPreviewShellPageAsync()
    {
        int pageNumber = _nextStackPageNumber++;
        await PreviewShell.PushAsync(CreatePreviewStackPage(pageNumber));
    }

    private static LuminaPage CreatePreviewPage(string navigationKey, string titleKey, string subtitleKey)
    {
        var title = LuminaLocalization.Get(titleKey);
        var subtitle = LuminaLocalization.Get(subtitleKey);

        return new LuminaPage
        {
            NavigationKey = navigationKey,
            ShellTitle = title,
            ShellSubtitle = subtitle,
            Content = new Border
            {
                Padding = new Avalonia.Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 28,
                            FontWeight = Avalonia.Media.FontWeight.Bold
                        },
                        new TextBlock
                        {
                            Text = subtitle,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = LuminaLocalization.Get(RouteHintKey),
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        }
                    }
                }
            }
        };
    }

    private LuminaPage CreatePreviewStackPage(int pageNumber)
    {
        string title = string.Format(LuminaLocalization.Get("Sandbox.ShellShowcase.StackPageTitle"), pageNumber);
        string subtitle = LuminaLocalization.Get("Sandbox.ShellShowcase.StackPageSubtitle");

        var pushButton = new Button
        {
            Classes = { "Primary", "Small" },
            Content = LuminaLocalization.Get("Sandbox.ShellShowcase.PushShellPage"),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        pushButton.Click += async (_, _) => await PushPreviewShellPageAsync();

        var popButton = new Button
        {
            Classes = { "Outline", "Small" },
            Content = LuminaLocalization.Get("Sandbox.ShellShowcase.PopShellPage"),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        popButton.Click += async (_, _) => await PreviewShell.PopAsync();

        var popRootButton = new Button
        {
            Classes = { "Outline", "Small" },
            Content = LuminaLocalization.Get("Sandbox.ShellShowcase.PopShellRoot"),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        popRootButton.Click += async (_, _) => await PreviewShell.PopToRootAsync();

        return new LuminaPage
        {
            ShellTitle = title,
            ShellSubtitle = subtitle,
            Content = new Border
            {
                Padding = new Avalonia.Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 28,
                            FontWeight = FontWeight.Bold
                        },
                        new TextBlock
                        {
                            Text = subtitle,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = LuminaLocalization.Get("Sandbox.ShellShowcase.StackPageHint"),
                            Foreground = Brushes.Gray,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Children =
                            {
                                pushButton,
                                popButton,
                                popRootButton
                            }
                        }
                    }
                }
            }
        };
    }
}
