using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class ColorsShowcaseViewModel : ObservableObject
{
    private readonly Func<string, Task> _copyToClipboardAsync;
    private readonly IAsyncRelayCommand<string?> _copyResourceCommand;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private string _shadowTitle = string.Empty;

    [ObservableProperty]
    private string _shadowDescription = string.Empty;

    [ObservableProperty]
    private string _copyStatus = string.Empty;

    [ObservableProperty]
    private bool _hasCopyStatus;

    [ObservableProperty]
    private IReadOnlyList<ColorTokenGroup> _colorGroups = [];

    [ObservableProperty]
    private IReadOnlyList<ShadowTokenRow> _shadowRows = [];

    public ColorsShowcaseViewModel()
        : this(_ => Task.CompletedTask)
    {
    }

    public ColorsShowcaseViewModel(Func<string, Task> copyToClipboardAsync)
    {
        _copyToClipboardAsync = copyToClipboardAsync;
        _copyResourceCommand = new AsyncRelayCommand<string?>(CopyResourceAsync);
        Refresh();
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    partial void OnCopyStatusChanged(string value)
    {
        HasCopyStatus = !string.IsNullOrWhiteSpace(value);
    }

    private async Task CopyResourceAsync(string? resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return;
        }

        await _copyToClipboardAsync(resourceKey);
        CopyStatus = LuminaLocalization.Format(SandboxLocalization.CommonCopiedFormat, resourceKey);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        Title = Text("Color Tokens", "色彩令牌");
        Subtitle = Text(
            "Semi-style token tables for surfaces, text, borders, semantic colors, interaction states, and shadows. Each row compares light and dark values so design decisions are easier to audit.",
            "参考 Semi 的色彩页结构，将表面、文本、边框、语义色、交互状态和阴影整理成表格。每一行都对比浅色和深色值，方便审查设计决策。");
        ShadowTitle = Text("Elevation Shadows", "层级阴影");
        ShadowDescription = Text(
            "BoxShadow tokens are listed separately because they describe depth rather than color.",
            "BoxShadow 令牌单独成表，因为它们表达的是层级深度，而不是颜色本身。");

        ColorGroups =
        [
            new ColorTokenGroup(
                Text("App Surfaces", "应用表面"),
                Text("Base layers used by the shell, pages, cards, popups, and disabled surfaces.", "用于 Shell、页面、卡片、弹层和禁用表面的基础层级。"),
                [
                    Row("Background", "LuminaBackgroundBrush", "#F4F4F5", "#070708", "Application base."),
                    Row("NavigationPane", "LuminaNavigationPaneBrush", "#E7ECF3", "#0D0E11", "Sidebar and navigation shell."),
                    Row("NavigationPaneItem", "LuminaNavigationPaneItemBrush", "#F8FAFC", "#15171C", "Items placed inside navigation panes."),
                    Row("NavigationPaneItemSelected", "LuminaNavigationPaneItemSelectedBrush", "#DBEAFE", "#172554", "Selected navigation item background."),
                    Row("Surface", "LuminaSurfaceBrush", "#FFFFFF", "#0D0E11", "Default cards and content panels."),
                    Row("SurfaceElevated", "LuminaSurfaceElevatedBrush", "#FFFFFF", "#171A20", "Raised cards and hover surfaces."),
                    Row("Popover", "LuminaPopoverBrush", "#FFFFFF", "#16171C", "Menus, flyouts, dialogs, and popups."),
                    Row("ActionDisabledBg", "LuminaActionDisabledBgBrush", "#E4E4E7", "#1D1D22", "Disabled controls and inactive surfaces.")
                ]),

            new ColorTokenGroup(
                Text("Text Hierarchy", "文本层级"),
                Text("Readable text tokens ordered by emphasis.", "按信息强调程度排序的可读文本令牌。"),
                [
                    Row("TextPrimary", "LuminaTextPrimaryBrush", "#09090B", "#FAFAFA", "Headings and primary labels."),
                    Row("TextSecondary", "LuminaTextSecondaryBrush", "#3F3F46", "#D4D4D8", "Body text and regular content."),
                    Row("TextTertiary", "LuminaTextTertiaryBrush", "#71717A", "#A1A1AA", "Metadata, hints, and captions."),
                    Row("TextDisabled", "LuminaTextDisabledBrush", "#A1A1AA", "#52525B", "Disabled or unavailable text."),
                    Row("TextForeground", "LuminaTextForegroundBrush", "#09090B", "#FAFAFA", "Compatibility foreground alias."),
                    Row("TextMuted", "LuminaTextMutedBrush", "#71717A", "#A1A1AA", "Compatibility muted alias.")
                ]),

            new ColorTokenGroup(
                Text("Borders And Dividers", "边框与分割线"),
                Text("Border strength tokens for quiet dividers, default outlines, and hover/focus emphasis.", "用于弱分割线、默认描边和悬停/焦点强调的边框强度。"),
                [
                    Row("BorderSubtle", "LuminaBorderSubtleBrush", "#F4F4F5", "#17181D", "Quiet separators and low-emphasis dividers."),
                    Row("BorderDefault", "LuminaBorderDefaultBrush", "#E4E4E7", "#252731", "Default component outlines."),
                    Row("BorderStrong", "LuminaBorderStrongBrush", "#A1A1AA", "#393C48", "Hover, focus, and elevated edges."),
                    Row("BorderDisabled", "LuminaBorderDisabledBrush", "#E4E4E7", "#252731", "Disabled component outlines."),
                    Row("Border", "LuminaBorderBrush", "#E4E4E7", "#252731", "Compatibility border alias."),
                    Row("Divider", "LuminaDividerBrush", "#F4F4F5", "#17181D", "Compatibility divider alias.")
                ]),

            new ColorTokenGroup(
                Text("Semantic Colors", "语义颜色"),
                Text("Action and feedback colors with base, hover, pressed, foreground, and background variants.", "动作和反馈颜色，包含基础、悬停、按下、前景和背景变体。"),
                [
                    Row("Primary", "LuminaPrimaryBrush", "#2563EB", "#3B82F6", "Primary actions and brand accents."),
                    Row("PrimaryHover", "LuminaPrimaryHoverBrush", "#1D4ED8", "#2563EB", "Hover state for primary actions."),
                    Row("PrimaryPressed", "LuminaPrimaryPressedBrush", "#1E3A8A", "#1D4ED8", "Pressed state for primary actions."),
                    Row("PrimaryForeground", "LuminaPrimaryForegroundBrush", "#FAFAFA", "#FAFAFA", "Text and icon color on primary surfaces."),
                    Row("PrimaryBg", "LuminaPrimaryBgBrush", "#EFF6FF", "#172554", "Subtle selected or highlighted background."),
                    Row("Success", "LuminaSuccessBrush", "#16A34A", "#22C55E", "Success actions and healthy states."),
                    Row("SuccessHover", "LuminaSuccessHoverBrush", "#15803D", "#16A34A", "Hover state for success actions."),
                    Row("SuccessBg", "LuminaSuccessBgBrush", "#F0FDF4", "#052E16", "Subtle success background."),
                    Row("Warning", "LuminaWarningBrush", "#F59E0B", "#FBBF24", "Warnings and review-needed states."),
                    Row("WarningHover", "LuminaWarningHoverBrush", "#D97706", "#F59E0B", "Hover state for warning actions."),
                    Row("WarningBg", "LuminaWarningBgBrush", "#FFFBEB", "#451A03", "Subtle warning background."),
                    Row("Danger", "LuminaDangerBrush", "#EF4444", "#F87171", "Errors and destructive actions."),
                    Row("DangerHover", "LuminaDangerHoverBrush", "#DC2626", "#EF4444", "Hover state for destructive actions."),
                    Row("DangerBg", "LuminaDangerBgBrush", "#FEF2F2", "#450A0A", "Subtle danger background.")
                ]),

            new ColorTokenGroup(
                Text("Extended Palette", "扩展色板"),
                Text("Supporting colors for charts, tags, illustrations, and optional theme accents.", "用于图表、标签、插图和可选主题强调的辅助色。"),
                [
                    Row("Teal", "LuminaTealBrush", "#0D9488", "#14B8A6", "Data visualization and status accents."),
                    Row("Cyan", "LuminaCyanBrush", "#0891B2", "#06B6D4", "Secondary data and cool highlights."),
                    Row("Indigo", "LuminaIndigoBrush", "#4F46E5", "#6366F1", "Navigation, product, and integration accents."),
                    Row("Purple", "LuminaPurpleBrush", "#9333EA", "#A855F7", "Creative or AI-related accents."),
                    Row("Pink", "LuminaPinkBrush", "#DB2777", "#EC4899", "Decorative and expressive accents."),
                    Row("Orange", "LuminaOrangeBrush", "#EA580C", "#F97316", "Warm data points and attention accents.")
                ]),

            new ColorTokenGroup(
                Text("Interaction And Glass", "交互与玻璃"),
                Text("State overlays, window glass, and card glass tokens used by composited surfaces.", "用于状态覆盖、窗口玻璃和卡片玻璃的合成表面令牌。"),
                [
                    Row("HoverBackground", "LuminaHoverBackgroundBrush", "#F4F4F5", "#15171C", "General hover background."),
                    Row("GhostHover", "LuminaGhostHoverBrush", "#E4E4E7", "#191B21", "Hover background for ghost controls."),
                    Row("StateHover", "LuminaStateHoverBrush", "#08000000", "#10FFFFFF", "Transparent hover overlay.", Text("hex alpha", "HEX 透明度")),
                    Row("StatePressed", "LuminaStatePressedBrush", "#0D000000", "#18FFFFFF", "Transparent pressed overlay.", Text("hex alpha", "HEX 透明度")),
                    Row("WindowGlassBackground", "LuminaWindowGlassBackgroundBrush", "#CCF4F4F5", "#66070708", "Window-level glass background.", Text("hex alpha", "HEX 透明度")),
                    Row("WindowGlassPane", "LuminaWindowGlassPaneBrush", "#E7ECF3", "#A00D0E11", "Window glass pane tint.", Text("mixed", "混合")),
                    Row("CardPseudoGlass", "LuminaCardPseudoGlassBrush", "#FFFFFF", "#0D0E11", "Readable glass tint.", Text("85% / 78%", "85% / 78%"), 0.85, 0.78),
                    Row("CardGlassBorder", "LuminaCardGlassBorderBrush", "#E4E4E7", "#252731", "Glass edge border.", Text("50% / 58%", "50% / 58%"), 0.50, 0.58),
                    Row("CardGlassBackground", "LuminaCardGlassBackgroundBrush", "#FFFFFF", "#0A0A0A", "Glass background layer.", "40%", 0.40, 0.40),
                    Row("CardGlassTint", "LuminaCardGlassTintBrush", "#82FFFFFF", "#5C0C0E12", "Gaussian glass readability tint.", Text("hex alpha", "HEX 透明度")),
                    Row("CardGlassEdge", "LuminaCardGlassEdgeBrush", "#CCFFFFFF", "#36FFFFFF", "Bright glass edge.", Text("hex alpha", "HEX 透明度"))
                ])
        ];

        ShadowRows =
        [
            new ShadowTokenRow(
                "ShadowSurface",
                "LuminaShadowSurface",
                "0 1 2 #10000000",
                "0 1 2 #30000000",
                Text("Base surface depth.", "基础表面深度。"),
                _copyResourceCommand),
            new ShadowTokenRow(
                "ShadowElevated",
                "LuminaShadowElevated",
                "0 0 1 #3D000000, 0 8 24 #22000000",
                "inset 0 0 0 1 #12FFFFFF, 0 10 28 #70000000",
                Text("Raised cards and elevated borders.", "浮起卡片和层级边缘。"),
                _copyResourceCommand),
            new ShadowTokenRow(
                "ShadowFloating",
                "LuminaShadowFloating",
                "0 0 1 #4A000000, 0 14 36 #2E000000",
                "inset 0 0 0 1 #18FFFFFF, 0 18 42 #84000000",
                Text("Pointer hover, floating surfaces, and stronger popups.", "鼠标悬停、浮层表面和更强弹层。"),
                _copyResourceCommand)
        ];
    }

    private ColorTokenRow Row(
        string name,
        string resourceKey,
        string lightHex,
        string darkHex,
        string usage,
        string opacityText = "100%",
        double lightOpacity = 1,
        double darkOpacity = 1)
    {
        return new ColorTokenRow(
            name,
            resourceKey,
            lightHex,
            darkHex,
            opacityText,
            Text(usage, TranslateUsage(usage)),
            CreateBrush(lightHex, lightOpacity),
            CreateBrush(darkHex, darkOpacity),
            _copyResourceCommand);
    }

    private static IBrush CreateBrush(string hex, double opacity)
    {
        return new SolidColorBrush(Color.Parse(hex))
        {
            Opacity = opacity
        };
    }

    private static string Text(string english, string chineseSimplified)
    {
        return SandboxTextLocalizer.Localize(english);
    }

    private static string TranslateUsage(string usage)
    {
        return usage switch
        {
            "Application base." => "应用底层背景。",
            "Sidebar and navigation shell." => "侧边栏和导航壳层。",
            "Items placed inside navigation panes." => "导航面板中的项目背景。",
            "Selected navigation item background." => "选中导航项背景。",
            "Default cards and content panels." => "默认卡片和内容面板。",
            "Raised cards and hover surfaces." => "浮起卡片和悬停表面。",
            "Menus, flyouts, dialogs, and popups." => "菜单、浮层、对话框和弹窗。",
            "Disabled controls and inactive surfaces." => "禁用控件和非活动表面。",
            "Headings and primary labels." => "标题和主要标签。",
            "Body text and regular content." => "正文和常规内容。",
            "Metadata, hints, and captions." => "元信息、提示和说明文字。",
            "Disabled or unavailable text." => "禁用或不可用文本。",
            "Compatibility foreground alias." => "兼容用前景色别名。",
            "Compatibility muted alias." => "兼容用弱化文本别名。",
            "Quiet separators and low-emphasis dividers." => "安静分割线和低强调分隔。",
            "Default component outlines." => "默认组件描边。",
            "Hover, focus, and elevated edges." => "悬停、焦点和浮起边缘。",
            "Disabled component outlines." => "禁用组件描边。",
            "Compatibility border alias." => "兼容用边框别名。",
            "Compatibility divider alias." => "兼容用分割线别名。",
            "Primary actions and brand accents." => "主要操作和品牌强调。",
            "Hover state for primary actions." => "主要操作悬停态。",
            "Pressed state for primary actions." => "主要操作按下态。",
            "Text and icon color on primary surfaces." => "主色表面上的文字和图标。",
            "Subtle selected or highlighted background." => "轻量选中或高亮背景。",
            "Success actions and healthy states." => "成功操作和健康状态。",
            "Hover state for success actions." => "成功操作悬停态。",
            "Subtle success background." => "轻量成功背景。",
            "Warnings and review-needed states." => "警告和需要检查的状态。",
            "Hover state for warning actions." => "警告操作悬停态。",
            "Subtle warning background." => "轻量警告背景。",
            "Errors and destructive actions." => "错误和破坏性操作。",
            "Hover state for destructive actions." => "破坏性操作悬停态。",
            "Subtle danger background." => "轻量危险背景。",
            "Data visualization and status accents." => "数据可视化和状态强调。",
            "Secondary data and cool highlights." => "次要数据和冷色高亮。",
            "Navigation, product, and integration accents." => "导航、产品和集成强调。",
            "Creative or AI-related accents." => "创意或 AI 相关强调。",
            "Decorative and expressive accents." => "装饰性和表达性强调。",
            "Warm data points and attention accents." => "暖色数据点和注意力强调。",
            "General hover background." => "通用悬停背景。",
            "Hover background for ghost controls." => "Ghost 控件悬停背景。",
            "Transparent hover overlay." => "透明悬停覆盖层。",
            "Transparent pressed overlay." => "透明按下覆盖层。",
            "Window-level glass background." => "窗口级玻璃背景。",
            "Window glass pane tint." => "窗口玻璃面板色调。",
            "Readable fallback glass tint." => "可读的玻璃回退色调。",
            "Glass edge border." => "玻璃边缘描边。",
            "Glass background layer." => "玻璃背景层。",
            "Acrylic overlay layer." => "亚克力覆盖层。",
            "Glass readability tint." => "玻璃可读性色调。",
            "Bright glass edge." => "明亮玻璃边缘。",
            _ => usage
        };
    }
}

public sealed record ColorTokenGroup(
    string Title,
    string Description,
    IReadOnlyList<ColorTokenRow> Rows);

public sealed record ColorTokenRow(
    string Name,
    string ResourceKey,
    string LightHex,
    string DarkHex,
    string OpacityText,
    string Usage,
    IBrush LightBrush,
    IBrush DarkBrush,
    ICommand CopyResourceCommand);

public sealed record ShadowTokenRow(
    string Name,
    string ResourceKey,
    string LightValue,
    string DarkValue,
    string Usage,
    ICommand CopyResourceCommand);
