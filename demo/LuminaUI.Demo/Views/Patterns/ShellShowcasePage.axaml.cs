using Avalonia.Controls;
using LuminaUI.Controls;
using LuminaUI.Localization;

namespace LuminaUI.Demo.Views;

public partial class ShellShowcasePage : LuminaPage
{
    private const string RouteHomeTitleKey = "Sandbox.ShellShowcase.NavHome";
    private const string RouteHomeSubtitleKey = "Sandbox.ShellShowcase.RouteHomeSubtitle";
    private const string RouteReportsTitleKey = "Sandbox.ShellShowcase.NavReports";
    private const string RouteReportsSubtitleKey = "Sandbox.ShellShowcase.RouteReportsSubtitle";
    private const string RouteSettingsTitleKey = "Sandbox.ShellShowcase.NavSettings";
    private const string RouteSettingsSubtitleKey = "Sandbox.ShellShowcase.RouteSettingsSubtitle";
    private const string RouteAuditTitleKey = "Sandbox.ShellShowcase.NavAudit";
    private const string RouteAuditSubtitleKey = "Sandbox.ShellShowcase.RouteAuditSubtitle";
    private const string RouteHintKey = "Sandbox.ShellShowcase.RouteHint";

    public ShellShowcasePage()
    {
        InitializeComponent();
        RegisterPreviewRoutes();
        PreviewShell.NavigateTo("PreviewHome", closeMenuOnNavigate: false);
    }

    private void RegisterPreviewRoutes()
    {
        PreviewShell.RegisterRoute("PreviewHome", () => CreatePreviewPage("PreviewHome", RouteHomeTitleKey, RouteHomeSubtitleKey));
        PreviewShell.RegisterRoute("PreviewReports", () => CreatePreviewPage("PreviewReports", RouteReportsTitleKey, RouteReportsSubtitleKey));
        PreviewShell.RegisterRoute("PreviewSettings", () => CreatePreviewPage("PreviewSettings", RouteSettingsTitleKey, RouteSettingsSubtitleKey));
        PreviewShell.RegisterRoute("PreviewAudit", () => CreatePreviewPage("PreviewAudit", RouteAuditTitleKey, RouteAuditSubtitleKey));
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
}
