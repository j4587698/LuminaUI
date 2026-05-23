using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using LuminaUI.Localization;
using LuminaUI.Theming;
using LuminaUI.Demo.Views;

namespace LuminaUI.Demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SandboxLocalization.Register();
        LuminaLocalization.UseSystemCulture();
        LuminaThemeManager.Initialize(this);
        
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new SandboxRootView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
